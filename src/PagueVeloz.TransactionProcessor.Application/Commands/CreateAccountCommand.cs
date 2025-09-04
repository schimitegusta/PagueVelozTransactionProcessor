using MediatR;
using PagueVeloz.TransactionProcessor.Application.DTOs;

namespace PagueVeloz.TransactionProcessor.Application.Commands
{
    public class CreateAccountCommand : IRequest<AccountResponse>
    {
        public Guid ClientId { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public string Currency { get; set; } = "BRL";

        public static CreateAccountCommand FromRequest(CreateAccountRequest request)
        {
            return new CreateAccountCommand
            {
                ClientId = Guid.Parse(request.ClientId),
                InitialBalance = request.InitialBalance / 100m,
                CreditLimit = request.CreditLimit / 100m,
                Currency = request.Currency
            };
        }
    }
}
