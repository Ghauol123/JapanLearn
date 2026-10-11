using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Japan.Core;

namespace Japan.Importer.Parsing;

public sealed partial class DocumentBuilder(string fileName)
{
    private enum Part { None, Meaning, Structure, Note, Examples, QuickComparison }

    private sealed class SectionBuilder
    {
        public List<string> Paragraphs { get; } = [];
        public List<Table> Tables { get; } = [];
        public bool IsEmpty => Paragraphs.Count == 0 && Tables.Count == 0;
        public Section Build() => new([.. Paragraphs], [.. Tables]);
    }

    private sealed class PointBuilder(int index, string pattern)
    {
        public string Pattern { get; } = pattern;
        public SectionBuilder Meaning { get; } = new();
        public SectionBuilder Structure { get; } = new();
        public SectionBuilder Note { get; } = new();
        public List<Example> Examples { get; } = [];
        public string? PendingJapanese { get; set; }

        public RawPoint Build() =>
            new(index, Pattern, Meaning.Build(), Structure.Build(), Note.IsEmpty ? null : Note.Build(), [.. Examples]);
    }

    private sealed class LessonBuilder(int number, string title)
    {
        public int Number { get; } = number;
        public List<RawPoint> Points { get; } = [];
        public List<Table> QuickComparisons { get; } = [];
        public RawLesson Build() => new(Number, title, [.. Points], [.. QuickComparisons]);
    }

    [GeneratedRegex(@"^B[ÀA]I\s*([0-9０-９]+)\s*[:：]\s*(.+?)\s*(?:[(（]\s*phần[^)）]*[)）])?$", RegexOptions.IgnoreCase)]
    private static partial Regex LessonRegex();

    [GeneratedRegex(@"^([0-9０-９]+)\s*[.．]\s*(.+)$")]
    private static partial Regex NumberedRegex();

    [GeneratedRegex(@"^(Ý nghĩa|Cấu trúc|Lưu ý|Ví dụ)\s*[:：]\s*(.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"điểm\s*([0-9]+)\s*[–—-]\s*([0-9]+)\s*/\s*106", RegexOptions.IgnoreCase)]
    private static partial Regex RangeRegex();

    [GeneratedRegex(@"^File\s*\d+\s*/\s*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex FooterRegex();

    private readonly List<LessonBuilder> _lessons = [];
    private readonly List<string> _warnings = [];
    private LessonBuilder? _lesson;
    private PointBuilder? _point;
    private Part _part;
    private PointRange? _range;

    public void LessonHeading(string text)
    {
        text = Clean(text);
        var m = LessonRegex().Match(text);
        if (!m.Success)
        {
            Paragraph(text);
            return;
        }

        ClosePoint();
        var number = ParseNumber(m.Groups[1].Value);
        _lesson = _lessons.Find(l => l.Number == number);
        if (_lesson is null)
        {
            _lesson = new LessonBuilder(number, m.Groups[2].Value.Trim());
            _lessons.Add(_lesson);
        }

        _part = Part.None;
    }

    public void PointHeading(string text)
    {
        text = Clean(text);
        ClosePoint();
        _part = Part.None;

        if (_lesson is null)
        {
            _warnings.Add($"{fileName}: tiêu đề nằm ngoài bài: \"{text}\"");
            return;
        }

        var m = NumberedRegex().Match(text);
        if (!m.Success)
        {
            _part = Part.QuickComparison;
            return;
        }

        _point = new PointBuilder(ParseNumber(m.Groups[1].Value), m.Groups[2].Value.Trim());
    }

    public void Paragraph(string text)
    {
        text = Clean(text);
        if (text.Length == 0 || FooterRegex().IsMatch(text)) return;

        if (_range is null && RangeRegex().Match(text) is { Success: true } r)
        {
            _range = new PointRange(ParseNumber(r.Groups[1].Value), ParseNumber(r.Groups[2].Value));
            return;
        }

        if (_lesson is null) return;

        if (_point is null)
        {
            _warnings.Add($"{fileName}: [Bài {_lesson.Number}] đoạn nằm ngoài điểm ngữ pháp: \"{text}\"");
            return;
        }

        var label = LabelRegex().Match(text);
        if (label.Success)
        {
            _part = label.Groups[1].Value.ToLowerInvariant() switch
            {
                "ý nghĩa" => Part.Meaning,
                "cấu trúc" => Part.Structure,
                "lưu ý" => Part.Note,
                _ => Part.Examples,
            };
            var rest = label.Groups[2].Value.Trim();
            if (rest.Length > 0) AddToPart(rest);
            return;
        }

        AddToPart(text);
    }

    public void Table(Table table)
    {
        if (_part == Part.QuickComparison && _lesson is not null)
        {
            _lesson.QuickComparisons.Add(table);
            return;
        }

        var target = _part switch
        {
            Part.Meaning => _point?.Meaning,
            Part.Structure => _point?.Structure,
            Part.Note => _point?.Note,
            _ => null,
        };

        if (target is null)
        {
            _warnings.Add($"{fileName}: bảng không thuộc mục nào (tiêu đề: {string.Join(" | ", table.Header)})");
            return;
        }

        target.Tables.Add(table);
    }

    public SourceDocument Build()
    {
        ClosePoint();
        return new SourceDocument(fileName, _range, [.. _lessons.Select(l => l.Build())], [.. _warnings]);
    }

    private void AddToPart(string text)
    {
        var point = _point!;
        switch (_part)
        {
            case Part.Meaning: point.Meaning.Paragraphs.Add(text); break;
            case Part.Structure: point.Structure.Paragraphs.Add(text); break;
            case Part.Note: point.Note.Paragraphs.Add(text); break;
            case Part.Examples: AddExampleLine(point, text); break;
            default: _warnings.Add($"{fileName}: [{point.Pattern}] đoạn không thuộc mục nào: \"{text}\""); break;
        }
    }

    private void AddExampleLine(PointBuilder point, string text)
    {
        if (text.StartsWith('→'))
        {
            var vietnamese = text[1..].Trim();
            if (point.PendingJapanese is null)
            {
                _warnings.Add($"{fileName}: [{point.Pattern}] câu dịch không có câu tiếng Nhật: \"{text}\"");
                return;
            }

            point.Examples.Add(new Example(point.PendingJapanese, vietnamese));
            point.PendingJapanese = null;
            return;
        }

        FlushPendingExample(point);
        var m = NumberedRegex().Match(text);
        var body = m.Success ? m.Groups[2].Value.Trim() : text;
        var arrow = body.IndexOf('→');
        if (arrow >= 0)
        {
            point.Examples.Add(new Example(body[..arrow].Trim(), body[(arrow + 1)..].Trim()));
            return;
        }

        point.PendingJapanese = body;
    }

    private static void FlushPendingExample(PointBuilder point)
    {
        if (point.PendingJapanese is null) return;
        point.Examples.Add(new Example(point.PendingJapanese, ""));
        point.PendingJapanese = null;
    }

    private void ClosePoint()
    {
        if (_point is null) return;
        FlushPendingExample(_point);
        _lesson!.Points.Add(_point.Build());
        _point = null;
    }

    private static string Clean(string text) => text.Normalize(NormalizationForm.FormC).Trim();

    private static int ParseNumber(string digits) =>
        int.Parse(digits.Normalize(NormalizationForm.FormKC), CultureInfo.InvariantCulture);
}
