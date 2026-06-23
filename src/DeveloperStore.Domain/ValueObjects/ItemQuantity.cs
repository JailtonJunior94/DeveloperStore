using DeveloperStore.Domain.Exceptions;

namespace DeveloperStore.Domain.ValueObjects;

public readonly record struct ItemQuantity
{
    private ItemQuantity(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static ItemQuantity Create(int value)
    {
        if (value <= 0)
        {
            throw new BusinessRuleValidationException("item quantity must be greater than zero");
        }

        if (value > 20)
        {
            throw new BusinessRuleValidationException("cannot sell more than 20 units of the same product");
        }

        return new ItemQuantity(value);
    }

    public override string ToString() => Value.ToString();
}
