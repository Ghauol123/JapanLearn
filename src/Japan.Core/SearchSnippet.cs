namespace Japan.Core;

internal static class SearchSnippet
{
    private const int Length = 160;
    private const int Lead = 40;

    public static string Make(string raw, string normalizedQuery)
    {
        if (raw.Length <= Length) return raw;

        var match = TextNormalizer.FindIn(raw, normalizedQuery)?.Start ?? 0;
        var start = match < Length - Lead
            ? 0
            : Math.Min(match - Lead, raw.Length - (Length - 1));
        var end = Math.Min(raw.Length, start + Length - (start > 0 ? 2 : 1));
        return (start > 0 ? "…" : "") + raw[start..end] + (end < raw.Length ? "…" : "");
    }
}
