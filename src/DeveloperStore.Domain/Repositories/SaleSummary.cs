using DeveloperStore.Domain.Enums;

namespace DeveloperStore.Domain.Repositories;

public sealed record SaleSummary(
    Guid Id,
    string SaleNumber,
    DateTimeOffset SoldAt,
    string CustomerName,
    string BranchName,
    decimal TotalAmount,
    SaleStatus Status,
    int ItemCount);
