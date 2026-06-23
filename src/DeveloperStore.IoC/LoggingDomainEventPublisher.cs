using DeveloperStore.Domain.Events;
using Microsoft.Extensions.Logging;

namespace DeveloperStore.IoC;

public sealed class LoggingDomainEventPublisher : IDomainEventPublisher
{
    private readonly ILogger<LoggingDomainEventPublisher> _logger;

    public LoggingDomainEventPublisher(ILogger<LoggingDomainEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogInformation("Domain event published: {EventName} {@DomainEvent}", domainEvent.GetType().Name, domainEvent);
        }

        return Task.CompletedTask;
    }
}
