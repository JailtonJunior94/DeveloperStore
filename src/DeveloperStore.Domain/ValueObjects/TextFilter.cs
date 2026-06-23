namespace DeveloperStore.Domain.ValueObjects;

public readonly record struct TextFilter
{
    private TextFilter(string text, StringMatchMode mode)
    {
        Text = text;
        Mode = mode;
    }

    public string Text { get; }

    public StringMatchMode Mode { get; }

    public static TextFilter? Create(string? rawValue)
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
        return new TextFilter(normalized, mode);
    }

    public bool Matches(string text)
    {
        return Mode switch
        {
            StringMatchMode.Contains => text.Contains(Text, StringComparison.OrdinalIgnoreCase),
            StringMatchMode.EndsWith => text.EndsWith(Text, StringComparison.OrdinalIgnoreCase),
            StringMatchMode.StartsWith => text.StartsWith(Text, StringComparison.OrdinalIgnoreCase),
            _ => text.Equals(Text, StringComparison.OrdinalIgnoreCase)
        };
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
