using Japan.Core;
using Japan.Importer.Assembly;
using Japan.Importer.Parsing;

namespace Japan.Tests.Importer;

public class BookAssemblerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Dictionary<string, GrammarEnrichment> NoEnrichment = new();

    private static RawPoint Raw(int index, string pattern = "～x", string vietnamese = "Dịch.") =>
        new(index, pattern, new Section(["Nghĩa."], []), new Section(["Vる + x"], []), null, [new Example("例文。", vietnamese)]);

    private static SourceDocument Doc(string file, PointRange? range, params RawLesson[] lessons) => new(file, range, lessons, []);

    private static RawLesson L(int number, params RawPoint[] points) => new(number, $"Bài {number}", points, []);

    [Fact]
    public void Assemble_MergesLessonSplitAcrossFiles_InIndexOrder()
    {
        var result = BookAssembler.Assemble(
            [Doc("File01.docx", new PointRange(1, 2), L(1, Raw(1), Raw(2))),
             Doc("File02.md", new PointRange(3, 4), L(1, Raw(3)), L(2, Raw(1)))],
            NoEnrichment, Now, expectedPointCount: 4);

        Assert.Empty(result.Errors);
        var book = result.Book!;
        Assert.Equal(["b1-1", "b1-2", "b1-3"], book.Lessons[0].Points.Select(p => p.Id));
        Assert.Equal("File02.md", book.Lessons[0].Points[2].SourceFile);
        Assert.Equal("b2-1", book.Lessons[1].Points.Single().Id);
        Assert.Equal(Now, book.GeneratedAt);
    }

    [Fact]
    public void Assemble_RangeMismatch_IsError()
    {
        var result = BookAssembler.Assemble([Doc("File03.docx", new PointRange(19, 27), L(1, Raw(1)))], NoEnrichment, Now, 1);

        Assert.Null(result.Book);
        Assert.Contains(result.Errors, e => e.Contains("File03.docx") && e.Contains("19–27") && e.Contains("đọc được 1"));
    }

    [Fact]
    public void Assemble_TotalMissingLessonDuplicateAndIncompletePoint_AreErrors()
    {
        var result = BookAssembler.Assemble(
            [Doc("a.docx", null, L(1, Raw(1), Raw(1)), L(3, Raw(1, vietnamese: "")))],
            NoEnrichment, Now, expectedPointCount: 106);

        Assert.Null(result.Book);
        Assert.Contains(result.Errors, e => e.Contains("trùng điểm b1-1"));
        Assert.Contains(result.Errors, e => e.Contains("Thiếu bài: 2"));
        Assert.Contains(result.Errors, e => e.Contains("mong đợi 106"));
        Assert.Contains(result.Errors, e => e.Contains("b3-1") && e.Contains("ví dụ 1"));
    }

    [Fact]
    public void Assemble_AttachesEnrichment_AndRejectsUnknownIds()
    {
        var enrichment = new Dictionary<string, GrammarEnrichment>
        {
            ["b1-1"] = new("Giải thích.", [], [], [], [new Comparison("～y", "b9-9", "Khác.")], []),
            ["b5-5"] = new("Không có điểm này.", [], [], [], [], []),
        };

        var result = BookAssembler.Assemble([Doc("a.docx", null, L(1, Raw(1)))], enrichment, Now, 1);

        Assert.Contains(result.Errors, e => e.Contains("b5-5.json") && e.Contains("không có điểm"));
        Assert.Contains(result.Errors, e => e.Contains("b1-1.json") && e.Contains("b9-9"));
    }

    [Fact]
    public void Assemble_ValidEnrichment_IsAttached()
    {
        var enrichment = new Dictionary<string, GrammarEnrichment> { ["b1-1"] = new("Giải thích.", [], [], [], [], []) };

        var result = BookAssembler.Assemble([Doc("a.docx", null, L(1, Raw(1)))], enrichment, Now, 1);

        Assert.Equal("Giải thích.", result.Book!.Lessons[0].Points[0].Enrichment!.Explanation);
    }
}
