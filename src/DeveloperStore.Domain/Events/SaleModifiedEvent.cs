using DeveloperStore.Domain.ValueObjects;

namespace DeveloperStore.Domain.Events;

public sealed record SaleModifiedEvent(SaleId SaleId, SaleNumber SaleNumber, DateTimeOffset OccurredOn) : IDomainEvent;
