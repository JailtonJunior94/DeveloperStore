namespace DeveloperStore.Domain.ValueObjects;

public sealed record CustomerReference
{
    private CustomerReference()
    {
    }

    private CustomerReference(string id, string description)
    {
        Id = id;
        Description = description;
    }

    public string Id { get; private init; } = string.Empty;

    public string Description { get; private init; } = string.Empty;

    public static CustomerReference Create(string id, string description)
    {
        var normalized = ExternalIdentityGuard.Normalize(id, description, "customer");
        return new CustomerReference(normalized.Id, normalized.Description);
    }
}
