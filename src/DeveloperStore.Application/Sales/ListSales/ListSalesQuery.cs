using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.Enums;
using MediatR;

namespace DeveloperStore.Application.Sales.ListSales;

public sealed record ListSalesQuery(
    string? SaleNumber,
    string? Customer,
    string? Branch,
    SaleStatus? Status,
    DateTimeOffset? MinSoldAt,
    DateTimeOffset? MaxSoldAt,
    string? Order,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResponse<SaleSummaryDto>>;
