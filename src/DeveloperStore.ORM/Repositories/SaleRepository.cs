using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DeveloperStore.ORM.Repositories;

public sealed class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByNumberAsync(SaleNumber saleNumber, CancellationToken cancellationToken)
    {
        return await _context.Sales.AnyAsync(sale => sale.SaleNumber == saleNumber, cancellationToken);
    }

    public async Task<Sale?> GetByIdAsync(SaleId id, CancellationToken cancellationToken)
    {
        return await _context.Sales
            .Include(sale => sale.Items)
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);
    }

    public async Task<Sale?> GetByNumberAsync(SaleNumber saleNumber, CancellationToken cancellationToken)
    {
        return await _context.Sales
            .Include(sale => sale.Items)
            .FirstOrDefaultAsync(sale => sale.SaleNumber == saleNumber, cancellationToken);
    }

    public async Task<PagedResult<Sale>> ListAsync(SaleListFilter filter, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(filter.Customer) || !string.IsNullOrWhiteSpace(filter.Branch))
        {
            return await ListWithOwnedFiltersAsync(filter, cancellationToken);
        }

        var query = _context.Sales
            .AsNoTracking()
            .Select(sale => new SaleListRow
            {
                Id = sale.Id.Value,
                SaleNumber = EF.Property<string>(sale, nameof(Sale.SaleNumber)),
                SoldAt = sale.SoldAt.Value,
                CustomerName = EF.Property<string>(sale.Customer, nameof(CustomerReference.Description)),
                BranchName = EF.Property<string>(sale.Branch, nameof(BranchReference.Description)),
                TotalAmount = sale.TotalAmount.Value,
                Status = sale.Status,
                ItemCount = sale.Items.Count
            });

        query = ApplySaleNumberFilter(query, filter.SaleNumber);

        if (filter.Status.HasValue)
        {
            query = query.Where(sale => sale.Status == filter.Status.Value);
        }

        if (filter.MinSoldAt.HasValue)
        {
            query = query.Where(sale => sale.SoldAt >= filter.MinSoldAt.Value);
        }

        if (filter.MaxSoldAt.HasValue)
        {
            query = query.Where(sale => sale.SoldAt <= filter.MaxSoldAt.Value);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var pageIds = await ApplyOrdering(query, filter.Order)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(row => row.Id)
            .ToListAsync(cancellationToken);

        var sortOrder = pageIds
            .Select((id, index) => new { id, index })
            .ToDictionary(entry => entry.id, entry => entry.index);

        var items = await _context.Sales
            .AsNoTracking()
            .Where(BuildSaleIdPredicate(pageIds))
            .Include(sale => sale.Items)
            .ToArrayAsync(cancellationToken);

        Array.Sort(items, (left, right) => sortOrder[left.Id.Value].CompareTo(sortOrder[right.Id.Value]));

        return new PagedResult<Sale>(items, filter.PageNumber, filter.PageSize, totalCount);
    }

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<PagedResult<Sale>> ListWithOwnedFiltersAsync(SaleListFilter filter, CancellationToken cancellationToken)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Include(sale => sale.Items)
            .AsQueryable();

        query = ApplySaleNumberFilter(query, filter.SaleNumber);
        query = ApplyCustomerFilter(query, filter.Customer);
        query = ApplyBranchFilter(query, filter.Branch);

        if (filter.Status.HasValue)
        {
            query = query.Where(sale => sale.Status == filter.Status.Value);
        }

        if (filter.MinSoldAt.HasValue)
        {
            query = query.Where(sale => sale.SoldAt.Value >= filter.MinSoldAt.Value);
        }

        if (filter.MaxSoldAt.HasValue)
        {
            query = query.Where(sale => sale.SoldAt.Value <= filter.MaxSoldAt.Value);
        }

        var materialized = await query.ToArrayAsync(cancellationToken);
        var totalCount = materialized.LongLength;
        var pagedItems = ApplyInMemoryOrdering(materialized, filter.Order)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToArray();

        return new PagedResult<Sale>(pagedItems, filter.PageNumber, filter.PageSize, totalCount);
    }

    private static IOrderedQueryable<SaleListRow> ApplyOrdering(IQueryable<SaleListRow> query, string? order)
    {
        var segments = string.IsNullOrWhiteSpace(order)
            ? ["soldAt asc", "saleNumber asc"]
            : order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedQueryable<SaleListRow>? orderedQuery = null;
        foreach (var segment in segments)
        {
            var parts = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var field = parts[0];
            var descending = parts.Length == 2 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
            orderedQuery = ApplyOrderSegment(orderedQuery ?? query, field, descending, orderedQuery is not null);
        }

        return orderedQuery ?? query.OrderBy(sale => sale.SoldAt);
    }

    private static IOrderedQueryable<SaleListRow> ApplyOrderSegment(
        IQueryable<SaleListRow> query,
        string field,
        bool descending,
        bool append)
    {
        return field.ToLowerInvariant() switch
        {
            "salenumber" => OrderBy(query, sale => sale.SaleNumber, descending, append),
            "soldat" => OrderBy(query, sale => sale.SoldAt, descending, append),
            "customername" => OrderBy(query, sale => sale.CustomerName, descending, append),
            "branchname" => OrderBy(query, sale => sale.BranchName, descending, append),
            "totalamount" => OrderBy(query, sale => sale.TotalAmount, descending, append),
            "status" => OrderBy(query, sale => sale.Status, descending, append),
            "itemcount" => OrderBy(query, sale => sale.ItemCount, descending, append),
            _ => append
                ? ((IOrderedQueryable<SaleListRow>)query).ThenBy(sale => sale.SoldAt)
                : query.OrderBy(sale => sale.SoldAt)
        };
    }

    private static IQueryable<SaleListRow> ApplySaleNumberFilter(
        IQueryable<SaleListRow> query,
        string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return query;
        }

        var value = rawValue.Trim();
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        var normalized = value.Trim('*');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return query;
        }

        if (startsWithWildcard && endsWithWildcard)
        {
            return query.Where(sale => EF.Functions.Like(sale.SaleNumber, BuildLikePattern(normalized, StringMatchMode.Contains)));
        }

        if (startsWithWildcard)
        {
            return query.Where(sale => EF.Functions.Like(sale.SaleNumber, BuildLikePattern(normalized, StringMatchMode.EndsWith)));
        }

        if (endsWithWildcard)
        {
            return query.Where(sale => EF.Functions.Like(sale.SaleNumber, BuildLikePattern(normalized, StringMatchMode.StartsWith)));
        }

        return query.Where(sale => sale.SaleNumber == normalized);
    }

    private static IQueryable<Sale> ApplySaleNumberFilter(
        IQueryable<Sale> query,
        string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return query;
        }

        var value = rawValue.Trim();
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        var normalized = value.Trim('*');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return query;
        }

        return matchMode(startsWithWildcard, endsWithWildcard) switch
        {
            StringMatchMode.Contains => query.Where(sale => EF.Functions.Like(EF.Property<string>(sale, nameof(Sale.SaleNumber)), BuildLikePattern(normalized, StringMatchMode.Contains))),
            StringMatchMode.EndsWith => query.Where(sale => EF.Functions.Like(EF.Property<string>(sale, nameof(Sale.SaleNumber)), BuildLikePattern(normalized, StringMatchMode.EndsWith))),
            StringMatchMode.StartsWith => query.Where(sale => EF.Functions.Like(EF.Property<string>(sale, nameof(Sale.SaleNumber)), BuildLikePattern(normalized, StringMatchMode.StartsWith))),
            _ => query.Where(sale => EF.Property<string>(sale, nameof(Sale.SaleNumber)) == normalized)
        };
    }

    private static IQueryable<SaleListRow> ApplyCustomerFilter(IQueryable<SaleListRow> query, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return query;
        }

        var value = rawValue.Trim();
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        var normalized = value.Trim('*');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return query;
        }

        return matchMode(startsWithWildcard, endsWithWildcard) switch
        {
            StringMatchMode.Contains => query.Where(sale => EF.Functions.Like(sale.CustomerName, BuildLikePattern(normalized, StringMatchMode.Contains))),
            StringMatchMode.EndsWith => query.Where(sale => EF.Functions.Like(sale.CustomerName, BuildLikePattern(normalized, StringMatchMode.EndsWith))),
            StringMatchMode.StartsWith => query.Where(sale => EF.Functions.Like(sale.CustomerName, BuildLikePattern(normalized, StringMatchMode.StartsWith))),
            _ => query.Where(sale => sale.CustomerName == normalized)
        };
    }

    private static IQueryable<Sale> ApplyCustomerFilter(IQueryable<Sale> query, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return query;
        }

        var value = rawValue.Trim();
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        var normalized = value.Trim('*');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return query;
        }

        return matchMode(startsWithWildcard, endsWithWildcard) switch
        {
            StringMatchMode.Contains => query.Where(sale => EF.Functions.Like(EF.Property<string>(sale.Customer, nameof(CustomerReference.Description)), BuildLikePattern(normalized, StringMatchMode.Contains))),
            StringMatchMode.EndsWith => query.Where(sale => EF.Functions.Like(EF.Property<string>(sale.Customer, nameof(CustomerReference.Description)), BuildLikePattern(normalized, StringMatchMode.EndsWith))),
            StringMatchMode.StartsWith => query.Where(sale => EF.Functions.Like(EF.Property<string>(sale.Customer, nameof(CustomerReference.Description)), BuildLikePattern(normalized, StringMatchMode.StartsWith))),
            _ => query.Where(sale => EF.Property<string>(sale.Customer, nameof(CustomerReference.Description)) == normalized)
        };
    }

    private static IQueryable<SaleListRow> ApplyBranchFilter(IQueryable<SaleListRow> query, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return query;
        }

        var value = rawValue.Trim();
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        var normalized = value.Trim('*');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return query;
        }

        return matchMode(startsWithWildcard, endsWithWildcard) switch
        {
            StringMatchMode.Contains => query.Where(sale => EF.Functions.Like(sale.BranchName, BuildLikePattern(normalized, StringMatchMode.Contains))),
            StringMatchMode.EndsWith => query.Where(sale => EF.Functions.Like(sale.BranchName, BuildLikePattern(normalized, StringMatchMode.EndsWith))),
            StringMatchMode.StartsWith => query.Where(sale => EF.Functions.Like(sale.BranchName, BuildLikePattern(normalized, StringMatchMode.StartsWith))),
            _ => query.Where(sale => sale.BranchName == normalized)
        };
    }

    private static IQueryable<Sale> ApplyBranchFilter(IQueryable<Sale> query, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return query;
        }

        var value = rawValue.Trim();
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        var normalized = value.Trim('*');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return query;
        }

        return matchMode(startsWithWildcard, endsWithWildcard) switch
        {
            StringMatchMode.Contains => query.Where(sale => EF.Functions.Like(EF.Property<string>(sale.Branch, nameof(BranchReference.Description)), BuildLikePattern(normalized, StringMatchMode.Contains))),
            StringMatchMode.EndsWith => query.Where(sale => EF.Functions.Like(EF.Property<string>(sale.Branch, nameof(BranchReference.Description)), BuildLikePattern(normalized, StringMatchMode.EndsWith))),
            StringMatchMode.StartsWith => query.Where(sale => EF.Functions.Like(EF.Property<string>(sale.Branch, nameof(BranchReference.Description)), BuildLikePattern(normalized, StringMatchMode.StartsWith))),
            _ => query.Where(sale => EF.Property<string>(sale.Branch, nameof(BranchReference.Description)) == normalized)
        };
    }

    private static string BuildLikePattern(string value, StringMatchMode matchMode)
    {
        var escaped = value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        return matchMode switch
        {
            StringMatchMode.StartsWith => $"{escaped}%",
            StringMatchMode.EndsWith => $"%{escaped}",
            StringMatchMode.Contains => $"%{escaped}%",
            _ => escaped
        };
    }

    private static StringMatchMode matchMode(bool startsWithWildcard, bool endsWithWildcard)
    {
        if (startsWithWildcard && endsWithWildcard)
        {
            return StringMatchMode.Contains;
        }

        if (startsWithWildcard)
        {
            return StringMatchMode.EndsWith;
        }

        return endsWithWildcard ? StringMatchMode.StartsWith : StringMatchMode.Equals;
    }

    private enum StringMatchMode
    {
        Equals,
        StartsWith,
        EndsWith,
        Contains
    }

    private static IOrderedQueryable<SaleListRow> OrderBy<TKey>(
        IQueryable<SaleListRow> query,
        Expression<Func<SaleListRow, TKey>> keySelector,
        bool descending,
        bool append)
    {
        if (append)
        {
            var ordered = (IOrderedQueryable<SaleListRow>)query;
            return descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);
        }

        return descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }

    private static Expression<Func<SaleListRow, bool>> BuildRowIdPredicate(IReadOnlyList<Guid> ids)
    {
        var parameter = Expression.Parameter(typeof(SaleListRow), "sale");
        Expression body = Expression.Constant(false);

        foreach (var id in ids)
        {
            body = Expression.OrElse(
                body,
                Expression.Equal(
                    Expression.Property(parameter, nameof(SaleListRow.Id)),
                    Expression.Constant(id)));
        }

        return Expression.Lambda<Func<SaleListRow, bool>>(body, parameter);
    }

    private static Expression<Func<Sale, bool>> BuildSaleIdPredicate(IReadOnlyList<Guid> ids)
    {
        var parameter = Expression.Parameter(typeof(Sale), "sale");
        Expression body = Expression.Constant(false);
        var idValue = Expression.Property(Expression.Property(parameter, nameof(Sale.Id)), nameof(SaleId.Value));

        foreach (var id in ids)
        {
            body = Expression.OrElse(body, Expression.Equal(idValue, Expression.Constant(id)));
        }

        return Expression.Lambda<Func<Sale, bool>>(body, parameter);
    }

    private static IOrderedEnumerable<Sale> ApplyInMemoryOrdering(IEnumerable<Sale> sales, string? order)
    {
        var segments = string.IsNullOrWhiteSpace(order)
            ? ["soldAt asc", "saleNumber asc"]
            : order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedEnumerable<Sale>? ordered = null;

        foreach (var segment in segments)
        {
            var parts = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var field = parts[0];
            var descending = parts.Length == 2 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            ordered = field.ToLowerInvariant() switch
            {
                "salenumber" => ApplyInMemoryOrder(ordered, sales, sale => sale.SaleNumber.Value, descending),
                "soldat" => ApplyInMemoryOrder(ordered, sales, sale => sale.SoldAt.Value, descending),
                "customername" => ApplyInMemoryOrder(ordered, sales, sale => sale.Customer.Description, descending),
                "branchname" => ApplyInMemoryOrder(ordered, sales, sale => sale.Branch.Description, descending),
                "totalamount" => ApplyInMemoryOrder(ordered, sales, sale => sale.TotalAmount.Value, descending),
                "status" => ApplyInMemoryOrder(ordered, sales, sale => sale.Status, descending),
                "itemcount" => ApplyInMemoryOrder(ordered, sales, sale => sale.Items.Count, descending),
                _ => ordered ?? sales.OrderBy(sale => sale.SoldAt.Value).ThenBy(sale => sale.SaleNumber.Value)
            };
        }

        return ordered ?? sales.OrderBy(sale => sale.SoldAt.Value).ThenBy(sale => sale.SaleNumber.Value);
    }

    private static IOrderedEnumerable<Sale> ApplyInMemoryOrder<TKey>(
        IOrderedEnumerable<Sale>? ordered,
        IEnumerable<Sale> sales,
        Func<Sale, TKey> keySelector,
        bool descending)
    {
        if (ordered is null)
        {
            return descending ? sales.OrderByDescending(keySelector) : sales.OrderBy(keySelector);
        }

        return descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);
    }

    private sealed class SaleListRow
    {
        public required Guid Id { get; init; }

        public required string SaleNumber { get; init; }

        public required DateTimeOffset SoldAt { get; init; }

        public required string CustomerName { get; init; }

        public required string BranchName { get; init; }

        public required decimal TotalAmount { get; init; }

        public required Domain.Enums.SaleStatus Status { get; init; }

        public required int ItemCount { get; init; }
    }
}
