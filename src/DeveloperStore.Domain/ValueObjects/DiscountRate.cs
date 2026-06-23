namespace DeveloperStore.Domain.ValueObjects;

public readonly record struct DiscountRate
{
    private DiscountRate(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static DiscountRate Zero => new(0m);

    public static DiscountRate TenPercent => new(0.10m);

    public static DiscountRate TwentyPercent => new(0.20m);

    public override string ToString() => Value.ToString("0.##");
}
