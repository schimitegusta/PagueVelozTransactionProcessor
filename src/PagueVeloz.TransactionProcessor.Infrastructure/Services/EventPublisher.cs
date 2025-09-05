using MassTransit;
using Microsoft.Extensions.Logging;
using PagueVeloz.TransactionProcessor.Application.Services;
using PagueVeloz.TransactionProcessor.Domain.Events;
using Polly;
using Polly.Retry;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Services
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IBus _bus;
        private readonly ILogger<EventPublisher> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public EventPublisher(IBus bus, ILogger<EventPublisher> logger)
        {
            _bus = bus;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Repetir {RetryCount} evento de publicação após {TimeSpan}s: {Error}",
                            retryCount, timeSpan.TotalSeconds, exception.Message);
                    });
        }

        public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _bus.Publish(domainEvent, cancellationToken);
                    _logger.LogInformation(
                        "Evento {EventType} publicado com sucesso",
                        domainEvent.GetType().Name);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha ao publicar o evento {EventType} após novas tentativas",
                    domainEvent.GetType().Name);
            }
        }

        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _bus.Publish(message, cancellationToken);
                    _logger.LogInformation(
                        "Mensagem {MessageType} publicada com sucesso",
                        typeof(T).Name);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha ao publicar a mensagem {MessageType} após novas tentativas",
                    typeof(T).Name);
            }
        }
    }
}
