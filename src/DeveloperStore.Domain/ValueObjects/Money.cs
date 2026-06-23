using DeveloperStore.Domain.Exceptions;

namespace DeveloperStore.Domain.ValueObjects;

public readonly record struct Money
{
    private Money(decimal value)
    {
        Value = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public decimal Value { get; }

    public static Money Zero => new(0m);

    public static Money Create(decimal value, string label, bool allowZero = true)
    {
        if (allowZero)
        {
            if (value < 0)
            {
                throw new BusinessRuleValidationException($"{label} cannot be negative");
            }
        }
        else if (value <= 0)
        {
            throw new BusinessRuleValidationException($"{label} must be greater than zero");
        }

        return new Money(value);
    }

    public static Money FromDecimal(decimal value) => new(value);

    public static Money operator +(Money left, Money right) => new(left.Value + right.Value);

    public static Money operator -(Money left, Money right) => new(left.Value - right.Value);

    public static Money operator *(Money left, ItemQuantity right) => new(left.Value * right.Value);

    public static Money operator *(Money left, DiscountRate right) => new(left.Value * right.Value);

    public override string ToString() => Value.ToString("0.00");
}
