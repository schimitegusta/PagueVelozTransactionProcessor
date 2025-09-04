using PagueVeloz.TransactionProcessor.Domain.Events;

namespace PagueVeloz.TransactionProcessor.Application.Services
{
    public interface IEventPublisher
    {
        Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    }
}