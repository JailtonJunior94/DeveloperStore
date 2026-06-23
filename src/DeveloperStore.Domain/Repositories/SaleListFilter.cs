using DeveloperStore.Domain.Enums;
using DeveloperStore.Domain.ValueObjects;

namespace DeveloperStore.Domain.Repositories;

public sealed record SaleListFilter(
    SaleNumberFilter? SaleNumber,
    TextFilter? CustomerName,
    TextFilter? BranchName,
    SaleStatus? Status,
    SoldAtRange? SoldAtRange,
    PageRequest Pagination);
