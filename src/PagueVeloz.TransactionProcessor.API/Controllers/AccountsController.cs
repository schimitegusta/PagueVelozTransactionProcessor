using MediatR;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.TransactionProcessor.Application.Commands;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;

namespace PagueVeloz.TransactionProcessor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AccountsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(
            IMediator mediator,
            IUnitOfWork unitOfWork,
            ILogger<AccountsController> logger)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Cria uma nova conta
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AccountResponse>> CreateAccount(
            [FromBody] CreateAccountRequest request)
        {
            try
            {
                var clientId = Guid.Parse(request.ClientId);
                var clientExists = await _unitOfWork.Clients.ExistsAsync(clientId);

                if (!clientExists)
                {
                    var client = new Client(clientId ,$"Client-{clientId}", $"DOC-{clientId}".Substring(0, 8), $"client{clientId}@test.com");
                    request.ClientId = client.Id.ToString();
                    await _unitOfWork.Clients.AddAsync(client);
                    await _unitOfWork.SaveChangesAsync();
                }

                var command = CreateAccountCommand.FromRequest(request);
                var result = await _mediator.Send(command);

                return CreatedAtAction(
                    nameof(GetAccount),
                    new { id = result.AccountId },
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar conta");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Busca uma conta pelo ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AccountResponse>> GetAccount(Guid id)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(id);

            if (account == null)
            {
                return NotFound(new { error = $"Conta {id} não encontrada" });
            }

            return Ok(new AccountResponse
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
            });
        }

        /// <summary>
        /// Lista contas de um cliente
        /// </summary>
        [HttpGet("client/{clientId}")]
        [ProducesResponseType(typeof(List<AccountResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AccountResponse>>> GetClientAccounts(Guid clientId)
        {
            var accounts = await _unitOfWork.Accounts.GetByClientIdAsync(clientId);

            var result = accounts.Select(a => new AccountResponse
            {
                AccountId = a.Id.ToString(),
                ClientId = a.ClientId.ToString(),
                Balance = (long)(a.Balance * 100),
                ReservedBalance = (long)(a.ReservedBalance * 100),
                AvailableBalance = (long)(a.AvailableBalance * 100),
                CreditLimit = (long)(a.CreditLimit * 100),
                Currency = a.Currency,
                Status = a.Status.ToString().ToLower(),
                CreatedAt = a.CreatedAt
            }).ToList();

            return Ok(result);
        }
    }
}
