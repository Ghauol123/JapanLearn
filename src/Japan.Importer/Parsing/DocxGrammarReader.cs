using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using CoreTable = Japan.Core.Table;
using WordTable = DocumentFormat.OpenXml.Wordprocessing.Table;

namespace Japan.Importer.Parsing;

public static partial class DocxGrammarReader
{
    [GeneratedRegex(@"heading\s*([1-9])", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingRegex();

    public static SourceDocument ReadFile(string path)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        catch (IOException ex)
        {
            throw new SourceFileLockedException(path, ex);
        }

        using (stream)
        {
            return Read(Path.GetFileName(path), stream);
        }
    }

    public static SourceDocument Read(string fileName, Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var main = doc.MainDocumentPart ?? throw new InvalidDataException($"{fileName}: không có nội dung.");
        var body = main.Document?.Body ?? throw new InvalidDataException($"{fileName}: không có nội dung.");
        var styleNames = StyleNames(main);
        var builder = new DocumentBuilder(fileName);

        foreach (var element in body.ChildElements)
        {
            switch (element)
            {
                case Paragraph p:
                    var text = p.InnerText;
                    switch (HeadingLevel(p, styleNames))
                    {
                        case 1: builder.LessonHeading(text); break;
                        case 2: builder.PointHeading(text); break;
                        default: builder.Paragraph(text); break;
                    }

                    break;
                case WordTable t:
                    builder.Table(ToTable(t));
                    break;
            }
        }

        return builder.Build();
    }

    private static Dictionary<string, string> StyleNames(MainDocumentPart main)
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var style in main.StyleDefinitionsPart?.Styles?.Elements<Style>() ?? [])
        {
            if (style.StyleId?.Value is { } id) names.TryAdd(id, style.StyleName?.Val?.Value ?? id);
        }

        return names;
    }

    private static int HeadingLevel(Paragraph p, Dictionary<string, string> styleNames)
    {
        var id = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (id is null) return 0;

        var m = HeadingRegex().Match(styleNames.GetValueOrDefault(id, id));
        if (!m.Success) m = HeadingRegex().Match(id);
        return m.Success ? m.Groups[1].Value[0] - '0' : 0;
    }

    private static CoreTable ToTable(WordTable table)
    {
        var rows = table.Elements<TableRow>()
            .Select(r => (IReadOnlyList<string>)r.Elements<TableCell>()
                .Select(c => string.Join("\n", c.Elements<Paragraph>().Select(p => p.InnerText.Trim()).Where(s => s.Length > 0)))
                .ToList())
            .ToList();
        return rows.Count == 0 ? new CoreTable([], []) : new CoreTable(rows[0], rows.Skip(1).ToList());
    }
}
