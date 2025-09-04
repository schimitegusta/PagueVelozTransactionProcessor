namespace PagueVeloz.TransactionProcessor.Domain.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
