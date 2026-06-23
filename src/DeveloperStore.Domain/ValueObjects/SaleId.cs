using DeveloperStore.Domain.Exceptions;

namespace DeveloperStore.Domain.ValueObjects;

public readonly record struct SaleId
{
    private SaleId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static SaleId New() => new(Guid.CreateVersion7());

    public static SaleId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BusinessRuleValidationException("sale id is required");
        }

        return new SaleId(value);
    }

    public override string ToString() => Value.ToString();
}
