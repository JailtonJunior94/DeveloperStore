using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.ValueObjects;
using MediatR;

namespace DeveloperStore.Application.Sales.UpdateSale;

public sealed record UpdateSaleCommand(
    SaleId Id,
    SaleNumber SaleNumber,
    SoldAt SoldAt,
    CustomerReference Customer,
    BranchReference Branch,
    IReadOnlyCollection<SaleItemInput> Items) : IRequest<SaleDto>;
