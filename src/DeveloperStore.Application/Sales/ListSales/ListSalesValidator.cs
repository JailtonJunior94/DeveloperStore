using DeveloperStore.Common.Validation;
using FluentValidation;

namespace DeveloperStore.Application.Sales.ListSales;

public sealed class ListSalesValidator : ApiValidator<ListSalesQuery>
{
    public ListSalesValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThan(0)
            .WithErrorCode("page_number_invalid")
            .WithMessage("pageNumber must be greater than zero");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode("page_size_invalid")
            .WithMessage("pageSize must be between 1 and 100");

        RuleFor(query => query.Order)
            .Must(BeAValidOrderExpression)
            .WithErrorCode("order_invalid")
            .WithMessage("_order must use supported fields: saleNumber, soldAt, customerName, branchName, totalAmount, status or itemCount");

        RuleFor(query => query)
            .Must(q => !q.MinSoldAt.HasValue || !q.MaxSoldAt.HasValue || q.MinSoldAt <= q.MaxSoldAt)
            .WithErrorCode("sold_at_range_invalid")
            .WithMessage("_minSoldAt must not be greater than _maxSoldAt");
    }

    private static bool BeAValidOrderExpression(string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
        {
            return true;
        }

        var supportedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "saleNumber",
            "soldAt",
            "customerName",
            "branchName",
            "totalAmount",
            "status",
            "itemCount"
        };

        var segments = order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        foreach (var segment in segments)
        {
            var parts = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length is < 1 or > 2 || !supportedFields.Contains(parts[0]))
            {
                return false;
            }

            if (parts.Length == 2 && !parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase) && !parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
