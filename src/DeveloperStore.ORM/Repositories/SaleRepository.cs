using DeveloperStore.Domain.Entities;
using DeveloperStore.Domain.Repositories;
using DeveloperStore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

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

    public async Task<PagedResult<SaleSummary>> ListAsync(SaleListFilter filter, CancellationToken cancellationToken)
    {
        var query = _context.Sales.AsNoTracking();

        query = ApplySaleNumberExactFilter(query, filter.SaleNumber);

        if (filter.Status.HasValue)
            query = query.Where(sale => sale.Status == filter.Status.Value);

        var projectedQuery = query.Select(sale => new SaleSummary(
            sale.Id.Value,
            sale.SaleNumber.Value,
            sale.SoldAt.Value,
            sale.Customer.Description,
            sale.Branch.Description,
            sale.TotalAmount.Value,
            sale.Status,
            sale.Items.Count));

        var rows = await projectedQuery.ToListAsync(cancellationToken);

        var filtered = ApplySaleNumberWildcardFilterInMemory(rows, filter.SaleNumber);
        filtered = ApplySoldAtRangeFilterInMemory(filtered, filter.SoldAtRange);
        filtered = ApplyTextFilterInMemory(filtered, filter.CustomerName, summary => summary.CustomerName);
        filtered = ApplyTextFilterInMemory(filtered, filter.BranchName, summary => summary.BranchName);

        var totalCount = filtered.LongCount();
        var ordered = ApplyInMemoryOrdering(filtered, filter.Pagination.Order);
        var paged = ordered
            .Skip((filter.Pagination.PageNumber - 1) * filter.Pagination.PageSize)
            .Take(filter.Pagination.PageSize)
            .ToList();

        return new PagedResult<SaleSummary>(paged, filter.Pagination.PageNumber, filter.Pagination.PageSize, totalCount);
    }

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Sale> ApplySaleNumberExactFilter(IQueryable<Sale> query, SaleNumberFilter? filter)
    {
        if (filter is null || filter.Value.Mode != StringMatchMode.Equals)
        {
            return query;
        }

        var saleNumber = SaleNumber.Create(filter.Value.Text);
        return query.Where(sale => sale.SaleNumber == saleNumber);
    }

    private static IEnumerable<SaleSummary> ApplySaleNumberWildcardFilterInMemory(IEnumerable<SaleSummary> rows, SaleNumberFilter? filter)
    {
        if (filter is null || filter.Value.Mode == StringMatchMode.Equals)
            return rows;

        var text = filter.Value.Text;
        return filter.Value.Mode switch
        {
            StringMatchMode.Contains => rows.Where(r => r.SaleNumber.Contains(text, StringComparison.OrdinalIgnoreCase)),
            StringMatchMode.EndsWith => rows.Where(r => r.SaleNumber.EndsWith(text, StringComparison.OrdinalIgnoreCase)),
            StringMatchMode.StartsWith => rows.Where(r => r.SaleNumber.StartsWith(text, StringComparison.OrdinalIgnoreCase)),
            _ => rows.Where(r => r.SaleNumber.Equals(text, StringComparison.OrdinalIgnoreCase))
        };
    }

    private static IEnumerable<SaleSummary> ApplySoldAtRangeFilterInMemory(IEnumerable<SaleSummary> rows, SoldAtRange? range)
    {
        if (range is null)
            return rows;

        var soldAtRange = range.Value;
        var min = soldAtRange.Min.Value;
        var max = soldAtRange.Max.Value;

        if (min > DateTimeOffset.MinValue)
            rows = rows.Where(r => r.SoldAt >= min);

        if (max < DateTimeOffset.MaxValue)
            rows = rows.Where(r => r.SoldAt <= max);

        return rows;
    }

    private static IEnumerable<SaleSummary> ApplyTextFilterInMemory(
        IEnumerable<SaleSummary> rows,
        TextFilter? filter,
        Func<SaleSummary, string> selector)
    {
        if (filter is null)
            return rows;

        var textFilter = filter.Value;
        return rows.Where(row => textFilter.Matches(selector(row)));
    }

    private static IOrderedEnumerable<SaleSummary> ApplyInMemoryOrdering(IEnumerable<SaleSummary> rows, string? order)
    {
        var segments = string.IsNullOrWhiteSpace(order)
            ? ["soldAt asc", "saleNumber asc"]
            : order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedEnumerable<SaleSummary>? ordered = null;
        foreach (var segment in segments)
        {
            var parts = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var field = parts[0];
            var descending = parts.Length == 2 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            ordered = field.ToLowerInvariant() switch
            {
                "salenumber" => ApplyInMemoryOrder(ordered, rows, r => r.SaleNumber, descending),
                "soldat" => ApplyInMemoryOrder(ordered, rows, r => r.SoldAt, descending),
                "customername" => ApplyInMemoryOrder(ordered, rows, r => r.CustomerName, descending),
                "branchname" => ApplyInMemoryOrder(ordered, rows, r => r.BranchName, descending),
                "totalamount" => ApplyInMemoryOrder(ordered, rows, r => r.TotalAmount, descending),
                "status" => ApplyInMemoryOrder(ordered, rows, r => r.Status, descending),
                "itemcount" => ApplyInMemoryOrder(ordered, rows, r => r.ItemCount, descending),
                _ => ordered ?? rows.OrderBy(r => r.SoldAt).ThenBy(r => r.SaleNumber)
            };
        }

        return ordered ?? rows.OrderBy(r => r.SoldAt).ThenBy(r => r.SaleNumber);
    }

    private static IOrderedEnumerable<SaleSummary> ApplyInMemoryOrder<TKey>(
        IOrderedEnumerable<SaleSummary>? ordered,
        IEnumerable<SaleSummary> rows,
        Func<SaleSummary, TKey> keySelector,
        bool descending)
    {
        if (ordered is null)
            return descending ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);
        return descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);
    }
}
