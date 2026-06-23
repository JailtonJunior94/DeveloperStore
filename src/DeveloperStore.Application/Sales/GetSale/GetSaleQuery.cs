using DeveloperStore.Application.Sales.Common;
using DeveloperStore.Domain.ValueObjects;
using MediatR;

namespace DeveloperStore.Application.Sales.GetSale;

public sealed record GetSaleQuery(SaleId Id) : IRequest<SaleDto>;
