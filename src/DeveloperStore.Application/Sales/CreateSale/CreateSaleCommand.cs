using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.ValueObjects;
using MediatR;

namespace DeveloperStore.Application.Sales.CreateSale;

public sealed record CreateSaleCommand(
    SaleNumber SaleNumber,
    SoldAt SoldAt,
    CustomerReference Customer,
    BranchReference Branch,
    IReadOnlyCollection<SaleItemInput> Items) : IRequest<SaleDto>;
