using MediatR;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Domain.Enums;

namespace PagueVeloz.TransactionProcessor.Application.Commands
{
    public class ProcessTransactionCommand : IRequest<TransactionResponse>
    {
        public OperationType Operation { get; set; }
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "BRL";
        public string ReferenceId { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
        public Guid? TargetAccountId { get; set; }

        public static ProcessTransactionCommand FromRequest(TransactionRequest request)
        {
            if (!Enum.TryParse<OperationType>(request.Operation, true, out var operation))
                throw new ArgumentException($"Tipo de operação inválido: {request.Operation}");

            return new ProcessTransactionCommand
            {
                Operation = operation,
                AccountId = Guid.Parse(request.AccountId),
                Amount = request.Amount / 100m,
                Currency = request.Currency,
                ReferenceId = request.ReferenceId,
                Metadata = request.Metadata,
                TargetAccountId = !string.IsNullOrEmpty(request.TargetAccountId)
                    ? Guid.Parse(request.TargetAccountId)
                    : null
            };
        }
    }
}
