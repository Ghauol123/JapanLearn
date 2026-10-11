using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Japan.Tests;

internal static class DocxFixture
{
    public static MemoryStream Create(params OpenXmlElement[] body)
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            var styles = main.AddNewPart<StyleDefinitionsPart>();
            styles.Styles = new Styles(
                new Style(new StyleName { Val = "heading 1" }) { Type = StyleValues.Paragraph, StyleId = "Heading1" },
                new Style(new StyleName { Val = "heading 2" }) { Type = StyleValues.Paragraph, StyleId = "Heading2" },
                new Style(new StyleName { Val = "heading 2" }) { Type = StyleValues.Paragraph, StyleId = "a2" });
            main.Document = new Document(new Body(body));
        }

        ms.Position = 0;
        return ms;
    }

    public static void Save(string path, params OpenXmlElement[] body)
    {
        using var ms = Create(body);
        File.WriteAllBytes(path, ms.ToArray());
    }

    public static Paragraph P(string text, string? style = null)
    {
        var p = new Paragraph(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        if (style is not null) p.PrependChild(new ParagraphProperties(new ParagraphStyleId { Val = style }));
        return p;
    }

    public static Table T(params string[][] rows) =>
        new(rows.Select(r => new TableRow(r.Select(c => new TableCell(P(c))))));
}
