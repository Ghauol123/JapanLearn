namespace Japan.Core;

public sealed record SearchHit(GrammarPoint Point, string Field, string Snippet);

public sealed class GrammarSearch
{
    private const int SnippetLength = 160;

    private readonly record struct Field(int Weight, string Name, string Raw, string Normalized);

    private readonly IReadOnlyList<(GrammarPoint Point, IReadOnlyList<Field> Fields)> _entries;

    public GrammarSearch(GrammarBook book)
    {
        _entries = book.Lessons
            .SelectMany(l => l.Points)
            .Select(p => (p, (IReadOnlyList<Field>)Fields(p).ToList()))
            .ToList();
    }

    public IReadOnlyList<SearchHit> Search(string? query, int limit = 50)
    {
        var q = TextNormalizer.Normalize(query);
        if (q.Length == 0) return [];

        return _entries
            .Select((entry, order) => (entry.Point, Order: order,
                Best: entry.Fields.Where(f => f.Normalized.Contains(q, StringComparison.Ordinal))
                                  .OrderByDescending(f => f.Weight)
                                  .FirstOrDefault()))
            .Where(x => x.Best.Weight > 0)
            .OrderByDescending(x => x.Best.Weight)
            .ThenBy(x => x.Order)
            .Take(limit)
            .Select(x => new SearchHit(x.Point, x.Best.Name, Snippet(x.Best.Raw)))
            .ToList();
    }

    private static IEnumerable<Field> Fields(GrammarPoint p)
    {
        Field F(int weight, string name, string raw) => new(weight, name, raw, TextNormalizer.Normalize(raw));

        yield return F(100, "Mẫu", p.Pattern);
        foreach (var s in Texts(p.Meaning)) yield return F(50, "Ý nghĩa", s);
        foreach (var s in Texts(p.Structure)) yield return F(20, "Cấu trúc", s);
        if (p.Note is { } note)
        {
            foreach (var s in Texts(note)) yield return F(20, "Lưu ý", s);
        }

        if (p.Enrichment is { } e)
        {
            yield return F(20, "Bổ sung", e.Explanation);
            foreach (var s in e.Usage.Concat(e.Pitfalls)) yield return F(20, "Bổ sung", s);
            foreach (var c in e.Comparisons) yield return F(20, "So sánh", $"{c.Pattern}: {c.Difference}");
        }

        foreach (var ex in p.Examples.Concat(p.Enrichment?.Examples ?? []))
            yield return F(10, "Ví dụ", $"{ex.Japanese} — {ex.Vietnamese}");
    }

    private static IEnumerable<string> Texts(Section s) =>
        s.Paragraphs.Concat(s.Tables.SelectMany(t => t.Header.Concat(t.Rows.SelectMany(r => r))));

    private static string Snippet(string raw) => raw.Length <= SnippetLength ? raw : raw[..(SnippetLength - 1)] + "…";
}
