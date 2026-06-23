using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.ValueObjects;
using MediatR;

namespace DeveloperStore.Application.Sales.CancelSaleItem;

public sealed record CancelSaleItemCommand(SaleId SaleId, SaleItemId ItemId) : IRequest<SaleDto>;
