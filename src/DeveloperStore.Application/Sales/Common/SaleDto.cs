using DeveloperStore.Domain.Enums;

namespace DeveloperStore.Application.Sales.Common;

public sealed record SaleDto(
    Guid Id,
    string SaleNumber,
    DateTimeOffset SoldAt,
    string CustomerExternalId,
    string CustomerName,
    string BranchExternalId,
    string BranchName,
    decimal TotalAmount,
    SaleStatus Status,
    IReadOnlyCollection<SaleItemDto> Items);
