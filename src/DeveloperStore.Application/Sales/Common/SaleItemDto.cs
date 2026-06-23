namespace DeveloperStore.Application.Sales.Common;

public sealed record SaleItemDto(
    Guid Id,
    string ProductExternalId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TotalAmount,
    bool IsCancelled);
