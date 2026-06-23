namespace DeveloperStore.WebApi.Features.Sales;

public sealed record UpdateSaleRequest(
    string SaleNumber,
    DateTimeOffset SoldAt,
    string CustomerExternalId,
    string CustomerName,
    string BranchExternalId,
    string BranchName,
    IReadOnlyCollection<SaleItemRequest> Items);
