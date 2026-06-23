using DeveloperStore.Domain.Exceptions;

namespace DeveloperStore.Domain.ValueObjects;

internal static class ExternalIdentityGuard
{
    public static (string Id, string Description) Normalize(string id, string description, string label)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new BusinessRuleValidationException($"{label} external id is required");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new BusinessRuleValidationException($"{label} description is required");
        }

        return (id.Trim(), description.Trim());
    }
}
