using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.TransactionProcessor.Application.Commands;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Application.Services;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;

namespace PagueVeloz.TransactionProcessor.Application.Handlers
{
    public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, AccountResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<CreateAccountHandler> _logger;

        public CreateAccountHandler(
            IUnitOfWork unitOfWork,
            IEventPublisher eventPublisher,
            ILogger<CreateAccountHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task<AccountResponse> Handle(
            CreateAccountCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var client = await _unitOfWork.Clients
                    .GetByIdWithAccountsAsync(request.ClientId, cancellationToken);

                if (client == null)
                {
                    client = new Client(
                        $"Client-{request.ClientId}",
                        Guid.NewGuid().ToString("N").Substring(0, 11),
                        $"client{request.ClientId}@test.com");

                    await _unitOfWork.Clients.AddAsync(client, cancellationToken);
                }

                var account = client.CreateAccount(
                    request.InitialBalance,
                    request.CreditLimit,
                    request.Currency);

                await _unitOfWork.Clients.UpdateAsync(client, cancellationToken);
                await _unitOfWork.SaveEntitiesAsync(cancellationToken);

                foreach (var domainEvent in client.DomainEvents)
                {
                    await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
                }
                client.ClearDomainEvents();

                _logger.LogInformation(
                    "Conta {AccountId} criada para o cliente {ClientId}",
                    account.Id, client.Id);

                return new AccountResponse
                {
                    AccountId = account.Id.ToString(),
                    ClientId = account.ClientId.ToString(),
                    Balance = (long)(account.Balance * 100),
                    ReservedBalance = (long)(account.ReservedBalance * 100),
                    AvailableBalance = (long)(account.AvailableBalance * 100),
                    CreditLimit = (long)(account.CreditLimit * 100),
                    Currency = account.Currency,
                    Status = account.Status.ToString().ToLower(),
                    CreatedAt = account.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao criar conta para o cliente {ClientId}",
                    request.ClientId);
                throw;
            }
        }
    }
}
