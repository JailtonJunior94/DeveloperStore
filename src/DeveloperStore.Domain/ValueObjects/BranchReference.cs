namespace DeveloperStore.Domain.ValueObjects;

public sealed record BranchReference
{
    private BranchReference()
    {
    }

    private BranchReference(string id, string description)
    {
        Id = id;
        Description = description;
    }

    public string Id { get; private init; } = string.Empty;

    public string Description { get; private init; } = string.Empty;

    public static BranchReference Create(string id, string description)
    {
        var normalized = ExternalIdentityGuard.Normalize(id, description, "branch");
        return new BranchReference(normalized.Id, normalized.Description);
    }
}
