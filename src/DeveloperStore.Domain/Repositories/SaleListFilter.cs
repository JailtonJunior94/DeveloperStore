using DeveloperStore.Domain.Enums;

namespace DeveloperStore.Domain.Repositories;

public sealed record SaleListFilter(
    string? SaleNumber,
    string? CustomerName,
    string? BranchName,
    SaleStatus? Status,
    DateTimeOffset? MinSoldAt,
    DateTimeOffset? MaxSoldAt,
    string? Order,
    int PageNumber,
    int PageSize);
