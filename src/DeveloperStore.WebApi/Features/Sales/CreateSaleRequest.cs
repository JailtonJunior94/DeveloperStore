namespace DeveloperStore.WebApi.Features.Sales;

public sealed record SaleItemRequest(
    string ProductExternalId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record CreateSaleRequest(
    string SaleNumber,
    DateTimeOffset SoldAt,
    string CustomerExternalId,
    string CustomerName,
    string BranchExternalId,
    string BranchName,
    IReadOnlyCollection<SaleItemRequest> Items);
