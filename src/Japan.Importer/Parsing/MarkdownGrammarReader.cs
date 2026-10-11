using System.Text;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using CoreTable = Japan.Core.Table;
using MdTable = Markdig.Extensions.Tables.Table;

namespace Japan.Importer.Parsing;

public static class MarkdownGrammarReader
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();

    public static SourceDocument ReadFile(string path) => Read(Path.GetFileName(path), File.ReadAllText(path));

    public static SourceDocument Read(string fileName, string markdown)
    {
        var builder = new DocumentBuilder(fileName);
        foreach (var block in Markdown.Parse(EnsureBlankLineBeforeTables(markdown), Pipeline))
        {
            Visit(block, builder);
        }

        return builder.Build();
    }

    private static void Visit(Block block, DocumentBuilder builder)
    {
        switch (block)
        {
            case HeadingBlock { Level: 2 } h:
                builder.LessonHeading(Text(h.Inline));
                break;
            case HeadingBlock { Level: 3 } h:
                builder.PointHeading(Text(h.Inline));
                break;
            case HeadingBlock h:
                builder.Paragraph(Text(h.Inline));
                break;
            case ParagraphBlock p:
                foreach (var line in Lines(p.Inline)) builder.Paragraph(line);
                break;
            case ListBlock list:
                var number = 0;
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    number++;
                    var lines = item.OfType<ParagraphBlock>().SelectMany(p => Lines(p.Inline)).ToList();
                    if (lines.Count == 0) continue;
                    builder.Paragraph(list.IsOrdered ? $"{number}. {lines[0]}" : lines[0]);
                    foreach (var line in lines.Skip(1)) builder.Paragraph(line);
                }

                break;
            case MdTable table:
                builder.Table(ToTable(table));
                break;
        }
    }

    private static CoreTable ToTable(MdTable table)
    {
        var rows = table.OfType<TableRow>()
            .Select(r => (r.IsHeader, Cells: (IReadOnlyList<string>)r.OfType<TableCell>()
                .Select(c => string.Join(" ", c.OfType<ParagraphBlock>().Select(p => Text(p.Inline).Trim())))
                .ToList()))
            .ToList();
        var header = rows.Where(r => r.IsHeader).Select(r => r.Cells).FirstOrDefault() ?? [];
        return new CoreTable(header, rows.Where(r => !r.IsHeader).Select(r => r.Cells).ToList());
    }

    private static IEnumerable<string> Lines(ContainerInline? inline) =>
        Text(inline).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string Text(ContainerInline? inline)
    {
        var sb = new StringBuilder();
        if (inline is not null) Append(inline, sb);
        return sb.ToString();
    }

    private static void Append(Inline inline, StringBuilder sb)
    {
        switch (inline)
        {
            case LiteralInline l: sb.Append(l.Content.ToString()); break;
            case CodeInline c: sb.Append(c.Content); break;
            case LineBreakInline: sb.Append('\n'); break;
            case HtmlEntityInline e: sb.Append(e.Transcoded.ToString()); break;
            case ContainerInline c:
                foreach (var child in c) Append(child, sb);
                break;
        }
    }

    private static string EnsureBlankLineBeforeTables(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(markdown.Length + 16);
        for (var i = 0; i < lines.Length; i++)
        {
            var isTableLine = lines[i].TrimStart().StartsWith('|');
            if (isTableLine && i > 0 && lines[i - 1].Trim().Length > 0 && !lines[i - 1].TrimStart().StartsWith('|'))
                sb.Append('\n');
            sb.Append(lines[i]).Append('\n');
        }

        return sb.ToString();
    }
}
