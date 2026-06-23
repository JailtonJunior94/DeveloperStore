namespace DeveloperStore.Application.Sales.Common;

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    int TotalPages,
    long TotalCount);
