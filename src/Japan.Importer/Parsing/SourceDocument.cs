using Japan.Core;

namespace Japan.Importer.Parsing;

public sealed record SourceDocument(
    string FileName,
    PointRange? Range,
    IReadOnlyList<RawLesson> Lessons,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<int> ExcludedLessons);

public readonly record struct PointRange(int First, int Last)
{
    public int Count => Last - First + 1;
}

public sealed record RawLesson(int Number, string Title, IReadOnlyList<RawPoint> Points, IReadOnlyList<Table> QuickComparisons);

public sealed record RawPoint(
    int Index,
    string Pattern,
    Section Meaning,
    Section Structure,
    Section? Note,
    IReadOnlyList<Example> Examples);
