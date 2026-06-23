using DeveloperStore.Domain.ValueObjects;

namespace DeveloperStore.Domain.Events;

public sealed record SaleCancelledEvent(SaleId SaleId, SaleNumber SaleNumber, DateTimeOffset OccurredOn) : IDomainEvent;
