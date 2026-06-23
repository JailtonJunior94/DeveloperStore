using DeveloperStore.Domain.ValueObjects;

namespace DeveloperStore.Domain.Events;

public sealed record ItemCancelledEvent(SaleId SaleId, SaleItemId ItemId, SaleNumber SaleNumber, DateTimeOffset OccurredOn) : IDomainEvent;
