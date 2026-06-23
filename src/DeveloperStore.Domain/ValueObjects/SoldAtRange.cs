namespace DeveloperStore.Domain.ValueObjects;

public readonly record struct SoldAtRange
{
    private SoldAtRange(SoldAt min, SoldAt max)
    {
        Min = min;
        Max = max;
    }

    public SoldAt Min { get; }

    public SoldAt Max { get; }

    public static SoldAtRange? Create(DateTimeOffset? minValue, DateTimeOffset? maxValue)
    {
        if (!minValue.HasValue && !maxValue.HasValue)
        {
            return null;
        }

        var min = minValue.HasValue ? SoldAt.Create(minValue.Value) : SoldAt.Create(DateTimeOffset.MinValue);
        var max = maxValue.HasValue ? SoldAt.Create(maxValue.Value) : SoldAt.Create(DateTimeOffset.MaxValue);

        return new SoldAtRange(min, max);
    }
}
