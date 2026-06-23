namespace DeveloperStore.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
