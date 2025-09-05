using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.TransactionProcessor.Application.Commands;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Application.Services;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Enums;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;
using PagueVeloz.TransactionProcessor.Domain.ValueObjects;
using Polly;
using Polly.Retry;

namespace PagueVeloz.TransactionProcessor.Application.Handlers
{
    public class ProcessTransactionHandler : IRequestHandler<ProcessTransactionCommand, TransactionResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<ProcessTransactionHandler> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public ProcessTransactionHandler(
            IUnitOfWork unitOfWork,
            IEventPublisher eventPublisher,
            ILogger<ProcessTransactionHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventPublisher = eventPublisher;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Tentar novamente {RetryCount} após {TimeSpan}s devido a {Exception}",
                            retryCount, timeSpan.TotalSeconds, exception.Message);
                    });
        }

        public async Task<TransactionResponse> Handle(
            ProcessTransactionCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var existingTransaction = await _unitOfWork.Transactions
                    .GetByReferenceIdAsync(request.ReferenceId, cancellationToken);

                if (existingTransaction != null)
                {
                    _logger.LogInformation(
                        "A transação com referência {ReferenceId} já existe",
                        request.ReferenceId);

                    return CreateResponse(existingTransaction);
                }

                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    return await ProcessTransactionWithLock(request, cancellationToken);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao processar transação {ReferenceId}",
                    request.ReferenceId);

                return new TransactionResponse
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Status = "failed",
                    ErrorMessage = "Erro interno ao processar transação",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        private async Task<TransactionResponse> ProcessTransactionWithLock(
            ProcessTransactionCommand request,
            CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var account = await _unitOfWork.Accounts
                    .GetByIdForUpdateAsync(request.AccountId, cancellationToken);

                if (account == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return new TransactionResponse
                    {
                        TransactionId = Guid.NewGuid().ToString(),
                        Status = "failed",
                        ErrorMessage = $"Conta {request.AccountId} não encontrada",
                        Timestamp = DateTime.UtcNow
                    };
                }

                var money = new Money(request.Amount, request.Currency);
                Transaction transaction;

                switch (request.Operation)
                {
                    case OperationType.Credit:
                        transaction = account.Credit(money, request.ReferenceId, request.Metadata);
                        break;

                    case OperationType.Debit:
                        transaction = account.Debit(money, request.ReferenceId, request.Metadata);
                        break;

                    case OperationType.Reserve:
                        transaction = account.Reserve(money, request.ReferenceId, request.Metadata);
                        break;

                    case OperationType.Capture:
                        transaction = account.Capture(money, request.ReferenceId, request.Metadata);
                        break;

                    case OperationType.Transfer:
                        transaction = await ProcessTransfer(
                            account, request, money, cancellationToken);
                        break;

                    case OperationType.Reversal:
                        transaction = await ProcessReversal(
                            account, request, cancellationToken);
                        break;

                    default:
                        throw new NotSupportedException($"Operação {request.Operation} não suportada");
                }

                await _unitOfWork.Accounts.UpdateAsync(account, cancellationToken);
                await _unitOfWork.SaveEntitiesAsync(cancellationToken);

                await PublishEventsAsync(account, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "A transação {TransactionId} foi processada com sucesso para a conta {AccountId}",
                    transaction.Id, account.Id);

                return CreateResponse(transaction);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        private async Task<Transaction> ProcessTransfer(
            Account sourceAccount,
            ProcessTransactionCommand request,
            Money money,
            CancellationToken cancellationToken)
        {
            if (!request.TargetAccountId.HasValue)
                throw new ArgumentException("É necessária uma conta de destino para transferências");

            var targetAccount = await _unitOfWork.Accounts
                .GetByIdForUpdateAsync(request.TargetAccountId.Value, cancellationToken);

            if (targetAccount == null)
                throw new InvalidOperationException($"Conta de destino {request.TargetAccountId} não encontrada");

            var debitTransaction = sourceAccount.Debit(money, request.ReferenceId + "-DEBIT", request.Metadata);

            if (debitTransaction.Status == TransactionStatus.Failed)
                return debitTransaction;

            var creditTransaction = targetAccount.Credit(
                money,
                request.ReferenceId + "-CREDIT",
                request.Metadata);

            await _unitOfWork.Accounts.UpdateAsync(targetAccount, cancellationToken);

            return debitTransaction;
        }

        private async Task<Transaction> ProcessReversal(
            Account account,
            ProcessTransactionCommand request,
            CancellationToken cancellationToken)
        {
            var originalReferenceId = request.Metadata?["original_reference_id"]?.ToString();

            if (string.IsNullOrEmpty(originalReferenceId))
                throw new ArgumentException("É necessário o documento de identificação de referência original para reversões");

            var originalTransaction = await _unitOfWork.Transactions
                .GetByReferenceIdAsync(originalReferenceId, cancellationToken);

            if (originalTransaction == null)
                throw new InvalidOperationException($"Transação original {originalReferenceId} não encontrada");

            if (originalTransaction.Status != TransactionStatus.Success)
                throw new InvalidOperationException("Só pode reverter transações bem-sucedidas");

            var money = new Money(originalTransaction.Amount, originalTransaction.Currency);

            Transaction reversalTransaction = originalTransaction.Operation switch
            {
                OperationType.Credit => account.Debit(money, request.ReferenceId, request.Metadata),
                OperationType.Debit => account.Credit(money, request.ReferenceId, request.Metadata),
                _ => throw new NotSupportedException($"Não é possível reverter a operação {originalTransaction.Operation}")
            };

            return reversalTransaction;
        }

        private async Task PublishEventsAsync(Account account, CancellationToken cancellationToken)
        {
            foreach (var domainEvent in account.DomainEvents)
            {
                await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
            account.ClearDomainEvents();
        }

        private TransactionResponse CreateResponse(Transaction transaction)
        {
            return new TransactionResponse
            {
                TransactionId = transaction.Id.ToString(),
                Status = transaction.Status.ToString().ToLower(),
                Balance = (long)(transaction.BalanceAfter * 100),
                ReservedBalance = (long)(transaction.ReservedBalanceAfter * 100),
                AvailableBalance = (long)(transaction.AvailableBalanceAfter * 100),
                ErrorMessage = transaction.ErrorMessage,
                Timestamp = transaction.ProcessedAt
            };
        }
    }
}
