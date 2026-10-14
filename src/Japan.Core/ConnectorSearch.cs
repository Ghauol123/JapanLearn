namespace Japan.Core;

public sealed record ConnectorHit(Connector Connector, string Field, string Snippet);

public sealed class ConnectorSearch
{
    private readonly record struct Field(int Weight, string Name, string Raw, string Normalized);

    private readonly IReadOnlyList<(Connector Connector, IReadOnlyList<Field> Fields)> _entries;

    public ConnectorSearch(ConnectorBook book)
    {
        _entries = book.Groups
            .SelectMany(g => g.Connectors)
            .Select(c => (c, (IReadOnlyList<Field>)Fields(c).ToList()))
            .ToList();
    }

    public IReadOnlyList<ConnectorHit> Search(string? query, int limit = 50)
    {
        var q = TextNormalizer.Normalize(query);
        if (q.Length == 0) return [];

        return _entries
            .Select((entry, order) => (entry.Connector, Order: order,
                Best: entry.Fields.Where(f => f.Normalized.Contains(q, StringComparison.Ordinal))
                                  .OrderByDescending(f => f.Weight)
                                  .FirstOrDefault()))
            .Where(x => x.Best.Weight > 0)
            .OrderByDescending(x => x.Best.Weight)
            .ThenBy(x => x.Order)
            .Take(limit)
            .Select(x => new ConnectorHit(x.Connector, x.Best.Name, SearchSnippet.Make(x.Best.Raw, q)))
            .ToList();
    }

    private static IEnumerable<Field> Fields(Connector c)
    {
        Field F(int weight, string name, string raw) => new(weight, name, raw, TextNormalizer.Normalize(raw));

        yield return F(100, "Từ nối", c.Word);
        if (c.Reading != c.Word) yield return F(100, "Từ nối", c.Reading);
        yield return F(50, "Nghĩa", c.Meaning);
        yield return F(20, "Giải thích", c.Explanation);
        foreach (var s in c.Usage.Concat(c.Pitfalls)) yield return F(20, "Giải thích", s);
        foreach (var cmp in c.Comparisons) yield return F(20, "So sánh", $"{cmp.Pattern}: {cmp.Difference}");
        foreach (var ex in c.Examples) yield return F(10, "Ví dụ", $"{ex.Japanese} — {ex.Vietnamese}");
    }
}
