namespace PagueVeloz.TransactionProcessor.Domain.Events
{
    public record AccountCreatedEvent(
    Guid AccountId,
    Guid ClientId,
    decimal InitialBalance,
    decimal CreditLimit) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public record AccountBlockedEvent(Guid AccountId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    public record AccountUnblockedEvent(Guid AccountId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
