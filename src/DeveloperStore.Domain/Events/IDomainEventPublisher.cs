namespace DeveloperStore.Domain.Events;

public interface IDomainEventPublisher
{
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
