using Japan.Core;
using Japan.Importer.Parsing;

namespace Japan.Importer.Assembly;

public sealed record AssemblyResult(GrammarBook? Book, IReadOnlyList<string> Errors);

public static class BookAssembler
{
    public const int ExpectedPointCount = 106;

    private sealed record LessonParts(string Title, List<GrammarPoint> Points, List<Table> QuickComparisons);

    public static AssemblyResult Assemble(
        IReadOnlyList<SourceDocument> documents,
        IReadOnlyDictionary<string, GrammarEnrichment> enrichment,
        DateTimeOffset generatedAt,
        int expectedPointCount = ExpectedPointCount)
    {
        var errors = new List<string>();
        var lessons = new SortedDictionary<int, LessonParts>();

        foreach (var doc in documents)
        {
            var count = doc.Lessons.Sum(l => l.Points.Count);
            if (doc.Range is { } range && range.Count != count)
                errors.Add($"{doc.FileName}: đầu file ghi điểm {range.First}–{range.Last} ({range.Count} điểm) nhưng đọc được {count} điểm");

            foreach (var raw in doc.Lessons)
            {
                if (!lessons.TryGetValue(raw.Number, out var lesson))
                {
                    lesson = new LessonParts(raw.Title, [], []);
                    lessons[raw.Number] = lesson;
                }

                lesson.QuickComparisons.AddRange(raw.QuickComparisons);
                foreach (var p in raw.Points)
                {
                    var id = $"b{raw.Number}-{p.Index}";
                    if (lesson.Points.Any(x => x.Index == p.Index))
                    {
                        errors.Add($"{doc.FileName}: trùng điểm {id} ({p.Pattern})");
                        continue;
                    }

                    errors.AddRange(Validate(doc.FileName, id, p));
                    lesson.Points.Add(new GrammarPoint(
                        id, raw.Number, p.Index, p.Pattern, p.Meaning, p.Structure, p.Note, p.Examples,
                        enrichment.GetValueOrDefault(id), doc.FileName));
                }
            }
        }

        var total = lessons.Values.Sum(l => l.Points.Count);
        if (total != expectedPointCount)
            errors.Add($"Tổng cộng đọc được {total} điểm, mong đợi {expectedPointCount}");

        if (lessons.Count > 0)
        {
            var excluded = documents.SelectMany(d => d.ExcludedLessons).ToHashSet();
            var missing = Enumerable.Range(1, lessons.Keys.Max()).Where(n => !lessons.ContainsKey(n) && !excluded.Contains(n)).ToList();
            if (missing.Count > 0) errors.Add($"Thiếu bài: {string.Join(", ", missing)}");
        }

        var ids = lessons.Values.SelectMany(l => l.Points).Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var (id, e) in enrichment.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (!ids.Contains(id)) errors.Add($"content/enrichment/{id}.json: không có điểm ngữ pháp {id}");
            foreach (var c in e.Comparisons)
            {
                if (c.TargetId is { } target && !ids.Contains(target))
                    errors.Add($"content/enrichment/{id}.json: so sánh \"{c.Pattern}\" trỏ tới {target} không tồn tại");
            }
        }

        if (errors.Count > 0) return new AssemblyResult(null, errors);

        var book = new GrammarBook(
            lessons.Select(kv => new Lesson(kv.Key, kv.Value.Title, kv.Value.Points.OrderBy(p => p.Index).ToList(), kv.Value.QuickComparisons)).ToList(),
            generatedAt);
        return new AssemblyResult(book, errors);
    }

    private static IEnumerable<string> Validate(string file, string id, RawPoint p)
    {
        var where = $"{file}: {id} ({p.Pattern})";
        if (p.Meaning.IsEmpty) yield return $"{where}: thiếu \"Ý nghĩa\"";
        for (var i = 0; i < p.Examples.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(p.Examples[i].Japanese) || string.IsNullOrWhiteSpace(p.Examples[i].Vietnamese))
                yield return $"{where}: ví dụ {i + 1} thiếu câu tiếng Nhật hoặc câu dịch";
        }
    }
}
