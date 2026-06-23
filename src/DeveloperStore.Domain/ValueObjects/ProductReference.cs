namespace DeveloperStore.Domain.ValueObjects;

public sealed record ProductReference
{
    private ProductReference()
    {
    }

    private ProductReference(string id, string description)
    {
        Id = id;
        Description = description;
    }

    public string Id { get; private init; } = string.Empty;

    public string Description { get; private init; } = string.Empty;

    public static ProductReference Create(string id, string description)
    {
        var normalized = ExternalIdentityGuard.Normalize(id, description, "product");
        return new ProductReference(normalized.Id, normalized.Description);
    }
}
