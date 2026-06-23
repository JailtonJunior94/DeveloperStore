using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.Repositories;

namespace DeveloperStore.Application.Sales.Common;

public static class SaleMappings
{
    public static SaleDto ToDto(this Sale sale)
    {
        return new SaleDto(
            sale.Id.Value,
            sale.SaleNumber.Value,
            sale.SoldAt.Value,
            sale.Customer.Id,
            sale.Customer.Description,
            sale.Branch.Id,
            sale.Branch.Description,
            sale.TotalAmount.Value,
            sale.Status,
            sale.Items.Select(item => new SaleItemDto(
                item.Id.Value,
                item.Product.Id,
                item.Product.Description,
                item.Quantity.Value,
                item.UnitPrice.Value,
                item.DiscountPercentage.Value,
                item.DiscountAmount.Value,
                item.TotalAmount.Value,
                item.IsCancelled)).ToArray());
    }

    public static SaleSummaryDto ToSummaryDto(this Sale sale)
    {
        return new SaleSummaryDto(
            sale.Id.Value,
            sale.SaleNumber.Value,
            sale.SoldAt.Value,
            sale.Customer.Description,
            sale.Branch.Description,
            sale.TotalAmount.Value,
            sale.Status,
            sale.Items.Count);
    }

    public static PagedResponse<SaleSummaryDto> ToResponse(this PagedResult<SaleSummary> pagedResult)
    {
        return new PagedResponse<SaleSummaryDto>(
            pagedResult.Items.Select(ToSummaryDto).ToArray(),
            pagedResult.PageNumber,
            pagedResult.PageSize,
            pagedResult.TotalPages,
            pagedResult.TotalCount);
    }

    private static SaleSummaryDto ToSummaryDto(SaleSummary summary)
    {
        return new SaleSummaryDto(
            summary.Id,
            summary.SaleNumber,
            summary.SoldAt,
            summary.CustomerName,
            summary.BranchName,
            summary.TotalAmount,
            summary.Status,
            summary.ItemCount);
    }
}
