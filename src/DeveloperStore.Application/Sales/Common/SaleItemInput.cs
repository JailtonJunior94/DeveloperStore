using DeveloperStore.Domain.ValueObjects;

namespace DeveloperStore.Application.Sales.Common;

public sealed record SaleItemInput(
    ProductReference Product,
    ItemQuantity Quantity,
    Money UnitPrice);
