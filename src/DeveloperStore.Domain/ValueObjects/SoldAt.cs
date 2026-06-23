using DeveloperStore.Domain.Exceptions;

namespace DeveloperStore.Domain.ValueObjects;

public readonly record struct SoldAt
{
    private SoldAt(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset Value { get; }

    public static SoldAt Create(DateTimeOffset value)
    {
        if (value == default)
        {
            throw new BusinessRuleValidationException("sold at is required");
        }

        return new SoldAt(value);
    }

    public override string ToString() => Value.ToString("O");
}
