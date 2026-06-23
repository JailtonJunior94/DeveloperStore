using DeveloperStore.Domain.Enums;

namespace DeveloperStore.Domain.Repositories;

public sealed record SaleListFilter(
    string? SaleNumber,
    string? Customer,
    string? Branch,
    SaleStatus? Status,
    DateTimeOffset? MinSoldAt,
    DateTimeOffset? MaxSoldAt,
    string? Order,
    int PageNumber,
    int PageSize);
