using DeveloperStore.Domain.Enums;

namespace DeveloperStore.Application.Sales.Common;

public sealed record SaleSummaryDto(
    Guid Id,
    string SaleNumber,
    DateTimeOffset SoldAt,
    string CustomerName,
    string BranchName,
    decimal TotalAmount,
    SaleStatus Status,
    int ItemCount);
