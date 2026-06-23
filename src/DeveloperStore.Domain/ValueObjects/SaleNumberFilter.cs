namespace DeveloperStore.Domain.ValueObjects;

public enum StringMatchMode
{
    Equals,
    StartsWith,
    EndsWith,
    Contains
}

public readonly record struct SaleNumberFilter
{
    private SaleNumberFilter(string text, StringMatchMode mode)
    {
        Text = text;
        Mode = mode;
    }

    public string Text { get; }

    public StringMatchMode Mode { get; }

    public static SaleNumberFilter? Create(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var value = rawValue.Trim();
        var startsWithWildcard = value.StartsWith('*');
        var endsWithWildcard = value.EndsWith('*');
        var normalized = value.Trim('*');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var mode = MatchMode(startsWithWildcard, endsWithWildcard);
        return new SaleNumberFilter(normalized, mode);
    }

    private static StringMatchMode MatchMode(bool startsWithWildcard, bool endsWithWildcard)
    {
        if (startsWithWildcard && endsWithWildcard)
        {
            return StringMatchMode.Contains;
        }

        if (startsWithWildcard)
        {
            return StringMatchMode.EndsWith;
        }

        return endsWithWildcard ? StringMatchMode.StartsWith : StringMatchMode.Equals;
    }
}
