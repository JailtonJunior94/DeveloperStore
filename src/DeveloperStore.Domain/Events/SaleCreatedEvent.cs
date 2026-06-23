using DeveloperStore.Domain.ValueObjects;

namespace DeveloperStore.Domain.Events;

public sealed record SaleCreatedEvent(SaleId SaleId, SaleNumber SaleNumber, DateTimeOffset OccurredOn) : IDomainEvent;
