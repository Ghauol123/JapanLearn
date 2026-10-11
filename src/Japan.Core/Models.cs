using System.Text.Json.Serialization;

namespace Japan.Core;

public sealed record GrammarBook(IReadOnlyList<Lesson> Lessons, DateTimeOffset GeneratedAt);

public sealed record Lesson(
    int Number,
    string Title,
    IReadOnlyList<GrammarPoint> Points,
    IReadOnlyList<Table> QuickComparisons);

public sealed record GrammarPoint(
    string Id,
    int Lesson,
    int Index,
    string Pattern,
    Section Meaning,
    Section Structure,
    Section? Note,
    IReadOnlyList<Example> Examples,
    GrammarEnrichment? Enrichment,
    string SourceFile);

public sealed record Section(IReadOnlyList<string> Paragraphs, IReadOnlyList<Table> Tables)
{
    public static Section Empty { get; } = new([], []);

    [JsonIgnore]
    public bool IsEmpty => Paragraphs.Count == 0 && Tables.Count == 0;
}

public sealed record Table(IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record Example(string Japanese, string Vietnamese, string? Ruby = null);
