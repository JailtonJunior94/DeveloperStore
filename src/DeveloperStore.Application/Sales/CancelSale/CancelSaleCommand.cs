using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.ValueObjects;
using MediatR;

namespace DeveloperStore.Application.Sales.CancelSale;

public sealed record CancelSaleCommand(SaleId Id) : IRequest<SaleDto>;
