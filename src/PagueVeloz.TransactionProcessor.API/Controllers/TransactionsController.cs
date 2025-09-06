using MediatR;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.TransactionProcessor.Application.Commands;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;
using PagueVeloz.TransactionProcessor.Infrastructure.Services;
using System.Diagnostics;

namespace PagueVeloz.TransactionProcessor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TransactionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionsController> _logger;
        private readonly TransactionMetrics _metrics;

        public TransactionsController(
            IMediator mediator,
            IUnitOfWork unitOfWork,
            ILogger<TransactionsController> logger,
            TransactionMetrics metrics)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _metrics = metrics;
        }

        /// <summary>
        /// Processa uma transação financeira
        /// </summary>
        /// <param name="request">Dados da transação</param>
        /// <returns>Resultado do processamento</returns>
        [HttpPost]
        [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TransactionResponse>> ProcessTransaction(
            [FromBody] TransactionRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation(
                    "Processando transação {ReferenceId} para a conta {AccountId}",
                    request.ReferenceId, request.AccountId);

                var command = ProcessTransactionCommand.FromRequest(request);
                var result = await _mediator.Send(command);

                stopwatch.Stop();
                _metrics.RecordTransaction(request.Operation, result.Status);
                _metrics.RecordDuration(request.Operation, stopwatch.ElapsedMilliseconds);

                if (result.Status == "failed")
                {
                    _logger.LogWarning(
                        "A transação {ReferenceId} falhou: {Error}",
                        request.ReferenceId, result.ErrorMessage);

                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Dados de solicitação inválidos");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar a transação {ReferenceId}", request.ReferenceId);

                stopwatch.Stop();
                _metrics.RecordTransaction(request.Operation, "error");

                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Processa múltiplas transações em lote
        /// </summary>
        [HttpPost("batch")]
        [ProducesResponseType(typeof(List<TransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TransactionResponse>>> ProcessBatch(
            [FromBody] List<TransactionRequest> requests)
        {
            if (requests == null || requests.Count == 0)
            {
                return BadRequest(new { error = "Nenhuma transação fornecida" });
            }

            if (requests.Count > 100)
            {
                return BadRequest(new { error = "Máximo de 100 transações por lote" });
            }

            var responses = new List<TransactionResponse>();

            foreach (var request in requests)
            {
                try
                {
                    var command = ProcessTransactionCommand.FromRequest(request);
                    var result = await _mediator.Send(command);
                    responses.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Erro ao processar a transação {ReferenceId} em lote",
                        request.ReferenceId);

                    responses.Add(new TransactionResponse
                    {
                        TransactionId = Guid.NewGuid().ToString(),
                        Status = "failed",
                        ErrorMessage = "Erro de processamento",
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            return Ok(responses);
        }

        /// <summary>
        /// Busca uma transação pelo ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetTransaction(Guid id)
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);

            if (transaction == null)
            {
                return NotFound(new { error = $"Transação {id} não encontrada" });
            }

            return Ok(new
            {
                transaction_id = transaction.Id,
                account_id = transaction.AccountId,
                operation = transaction.Operation.ToString().ToLower(),
                amount = (long)(transaction.Amount * 100),
                currency = transaction.Currency,
                status = transaction.Status.ToString().ToLower(),
                reference_id = transaction.ReferenceId,
                processed_at = transaction.ProcessedAt,
                balance_after = (long)(transaction.BalanceAfter * 100),
                error_message = transaction.ErrorMessage
            });
        }

        /// <summary>
        /// Busca transações de uma conta
        /// </summary>
        [HttpGet("account/{accountId}")]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAccountTransactions(
            Guid accountId,
            [FromQuery] int limit = 50)
        {
            var transactions = await _unitOfWork.Transactions
                .GetByAccountIdAsync(accountId, limit);

            var result = transactions.Select(t => new
            {
                transaction_id = t.Id,
                operation = t.Operation.ToString().ToLower(),
                amount = (long)(t.Amount * 100),
                currency = t.Currency,
                status = t.Status.ToString().ToLower(),
                reference_id = t.ReferenceId,
                processed_at = t.ProcessedAt,
                balance_after = (long)(t.BalanceAfter * 100)
            });

            return Ok(result);
        }
    }
}
