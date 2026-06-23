namespace DeveloperStore.Domain.Repositories;

public sealed record PageRequest(int PageNumber, int PageSize, string? Order = null);
