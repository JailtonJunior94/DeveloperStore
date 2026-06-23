namespace DeveloperStore.WebApi.Common;

public sealed record ApiPagedResponse<T>(
    IReadOnlyCollection<T> Data,
    int PageNumber,
    int PageSize,
    int TotalPages,
    long TotalCount);
