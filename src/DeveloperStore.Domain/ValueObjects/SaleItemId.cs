using DeveloperStore.Domain.Exceptions;

namespace DeveloperStore.Domain.ValueObjects;

public readonly record struct SaleItemId
{
    private SaleItemId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static SaleItemId New() => new(Guid.CreateVersion7());

    public static SaleItemId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BusinessRuleValidationException("sale item id is required");
        }

        return new SaleItemId(value);
    }

    public override string ToString() => Value.ToString();
}
