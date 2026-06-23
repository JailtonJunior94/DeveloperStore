using DeveloperStore.Domain.Exceptions;

namespace DeveloperStore.Domain.ValueObjects;

public readonly record struct SaleNumber
{
    private SaleNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static SaleNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException("sale number is required");
        }

        return new SaleNumber(value.Trim());
    }

    public static implicit operator string(SaleNumber saleNumber) => saleNumber.Value;

    public override string ToString() => Value;
}
