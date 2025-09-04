using PagueVeloz.TransactionProcessor.Domain.Enums;
using System.Text.Json;

namespace PagueVeloz.TransactionProcessor.Domain.Entities
{
    public class Transaction : BaseEntity
    {
        public Guid AccountId { get; private set; }
        public OperationType Operation { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }
        public string ReferenceId { get; private set; }
        public TransactionStatus Status { get; private set; }
        public string? MetadataJson { get; private set; }
        public decimal BalanceAfter { get; private set; }
        public decimal ReservedBalanceAfter { get; private set; }
        public decimal AvailableBalanceAfter { get; private set; }
        public string? ErrorMessage { get; private set; }
        public DateTime ProcessedAt { get; private set; }

        protected Transaction() { }

        public Transaction(
            Guid accountId,
            OperationType operation,
            decimal amount,
            string currency,
            string referenceId,
            TransactionStatus status,
            Dictionary<string, object>? metadata,
            decimal balanceAfter,
            decimal reservedBalanceAfter,
            decimal availableBalanceAfter,
            string? errorMessage = null)
        {
            AccountId = accountId;
            Operation = operation;
            Amount = amount;
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
            ReferenceId = referenceId ?? throw new ArgumentNullException(nameof(referenceId));
            Status = status;
            MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null;
            BalanceAfter = balanceAfter;
            ReservedBalanceAfter = reservedBalanceAfter;
            AvailableBalanceAfter = availableBalanceAfter;
            ErrorMessage = errorMessage;
            ProcessedAt = DateTime.UtcNow;
        }

        public Dictionary<string, object>? GetMetadata()
        {
            return string.IsNullOrEmpty(MetadataJson)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson);
        }
    }
}
