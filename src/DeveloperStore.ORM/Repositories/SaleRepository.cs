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

    public async Task<PagedResult<Sale>> ListAsync(SaleListFilter filter, CancellationToken cancellationToken)
    {
        var sqlQuery = _context.Sales
            .AsNoTracking()
            .Select(sale => new SaleListRow
            {
                Id = sale.Id.Value,
                SaleNumber = EF.Property<string>(sale, nameof(Sale.SaleNumber)),
                SoldAt = sale.SoldAt.Value,
                CustomerName = sale.Customer.Description,
                BranchName = sale.Branch.Description,
                TotalAmount = sale.TotalAmount.Value,
                Status = sale.Status,
                ItemCount = sale.Items.Count
            });

        sqlQuery = ApplySaleNumberFilter(sqlQuery, filter.SaleNumber);

        if (filter.Status.HasValue)
            sqlQuery = sqlQuery.Where(sale => sale.Status == filter.Status.Value);
        if (filter.MinSoldAt.HasValue)
            sqlQuery = sqlQuery.Where(sale => sale.SoldAt >= filter.MinSoldAt.Value);
        if (filter.MaxSoldAt.HasValue)
            sqlQuery = sqlQuery.Where(sale => sale.SoldAt <= filter.MaxSoldAt.Value);

        var rows = await sqlQuery.ToListAsync(cancellationToken);

        var filtered = ApplyCustomerFilterInMemory(rows, filter.CustomerName);
        filtered = ApplyBranchFilterInMemory(filtered, filter.BranchName);

        var totalCount = filtered.LongCount();
        var pageIds = ApplyInMemoryOrdering(filtered, filter.Order)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(row => row.Id)
            .ToList();

        var sortOrder = pageIds
            .Select((id, index) => new { id, index })
            .ToDictionary(entry => entry.id, entry => entry.index);

        var saleIds = pageIds.Select(SaleId.Create).ToList();
        var items = await _context.Sales
            .AsNoTracking()
            .Where(sale => saleIds.Contains(sale.Id))
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

    private static IEnumerable<SaleListRow> ApplyCustomerFilterInMemory(IEnumerable<SaleListRow> rows, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return rows;

        var value = rawValue.Trim();
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        var normalized = value.Trim('*');

        if (string.IsNullOrWhiteSpace(normalized))
            return rows;

        return matchMode(startsWithWildcard, endsWithWildcard) switch
        {
            StringMatchMode.Contains => rows.Where(r => r.CustomerName.Contains(normalized, StringComparison.OrdinalIgnoreCase)),
            StringMatchMode.EndsWith => rows.Where(r => r.CustomerName.EndsWith(normalized, StringComparison.OrdinalIgnoreCase)),
            StringMatchMode.StartsWith => rows.Where(r => r.CustomerName.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)),
            _ => rows.Where(r => r.CustomerName.Equals(normalized, StringComparison.OrdinalIgnoreCase))
        };
    }

    private static IEnumerable<SaleListRow> ApplyBranchFilterInMemory(IEnumerable<SaleListRow> rows, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return rows;

        var value = rawValue.Trim();
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        var normalized = value.Trim('*');

        if (string.IsNullOrWhiteSpace(normalized))
            return rows;

        return matchMode(startsWithWildcard, endsWithWildcard) switch
        {
            StringMatchMode.Contains => rows.Where(r => r.BranchName.Contains(normalized, StringComparison.OrdinalIgnoreCase)),
            StringMatchMode.EndsWith => rows.Where(r => r.BranchName.EndsWith(normalized, StringComparison.OrdinalIgnoreCase)),
            StringMatchMode.StartsWith => rows.Where(r => r.BranchName.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)),
            _ => rows.Where(r => r.BranchName.Equals(normalized, StringComparison.OrdinalIgnoreCase))
        };
    }

    private static IOrderedEnumerable<SaleListRow> ApplyInMemoryOrdering(IEnumerable<SaleListRow> rows, string? order)
    {
        var segments = string.IsNullOrWhiteSpace(order)
            ? ["soldAt asc", "saleNumber asc"]
            : order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedEnumerable<SaleListRow>? ordered = null;
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

    private static IOrderedEnumerable<SaleListRow> ApplyInMemoryOrder<TKey>(
        IOrderedEnumerable<SaleListRow>? ordered,
        IEnumerable<SaleListRow> rows,
        Func<SaleListRow, TKey> keySelector,
        bool descending)
    {
        if (ordered is null)
            return descending ? rows.OrderByDescending(keySelector) : rows.OrderBy(keySelector);
        return descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);
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
