using Japan.Core;
using Japan.Importer;
using Japan.Importer.Furigana;
using static Japan.Tests.DocxFixture;

namespace Japan.Tests.Importer;

public sealed class ImportPipelineTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("import-").FullName;
    private string Source => Path.Combine(_root, "source");
    private string Content => Path.Combine(_root, "content");
    private string Output => Path.Combine(_root, "data", "grammar.json");

    public ImportPipelineTests()
    {
        Directory.CreateDirectory(Source);
        Directory.CreateDirectory(Path.Combine(Content, "enrichment"));
        Save(Path.Combine(Source, "NguPhap_N3_File01_Bai1.docx"),
            P("FILE 1 — điểm 1–1/106"),
            P("BÀI 1: Mở đầu", "Heading1"),
            P("1. ～ば", "Heading2"),
            P("Ý nghĩa: Nếu."), P("Cấu trúc: Vば"), P("Ví dụ:"),
            P("1. 雨が降れば、行かない。"), P("→ Nếu mưa thì không đi."));
        File.WriteAllText(Path.Combine(Source, "NguPhap_N3_File02_Bai2.md"), """
            # FILE 2 (điểm 2–2/106)
            ## BÀI 2: Tiếp theo
            ### 1. ～たら
            **Ý nghĩa:** Sau khi.

            **Cấu trúc:** Vたら

            **Ví dụ:**
            1. 家に帰ったら、電話します。
               → Về nhà rồi tôi sẽ gọi điện.
            """);
        Save(Path.Combine(Source, "NguPhap_N3_File02_Bai2.docx"), P("bản Word lỗi, phải bị bỏ qua"));
        Save(Path.Combine(Source, "Ngữ pháp N3_1.docx"), P("không phải nguồn"));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private ImportSettings Settings => new(Source, Content, Output, ExpectedPointCount: 2);

    [Fact]
    public void Find_PrefersMarkdownAndIgnoresOtherFiles()
    {
        var files = SourceDiscovery.Find(Source).Select(Path.GetFileName);

        Assert.Equal(["NguPhap_N3_File01_Bai1.docx", "NguPhap_N3_File02_Bai2.md"], files);
    }

    [Fact]
    public void Run_WritesGrammarJsonWithFurigana()
    {
        var output = new StringWriter();

        var exit = ImportPipeline.Run(Settings, output, new FakeGenerator());

        Assert.Equal(0, exit);
        var book = GrammarJson.Deserialize(File.ReadAllText(Output));
        Assert.Equal(["b1-1", "b2-1"], book.Lessons.SelectMany(l => l.Points).Select(p => p.Id));
        Assert.Equal("R:家に帰ったら、電話します。", book.Lessons[1].Points[0].Examples[0].Ruby);
        Assert.Contains("2 điểm", output.ToString());
        Assert.Contains("Chưa có phần bổ sung: b1-1, b2-1", output.ToString());
    }

    [Fact]
    public void Run_BadEnrichment_FailsWithoutOverwritingOutput()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Output)!);
        File.WriteAllText(Output, "cũ");
        File.WriteAllText(Path.Combine(Content, "enrichment", "b1-1.json"), "{ hỏng");
        var output = new StringWriter();

        var exit = ImportPipeline.Run(Settings, output, new FakeGenerator());

        Assert.Equal(1, exit);
        Assert.Equal("cũ", File.ReadAllText(Output));
        Assert.Contains("b1-1.json", output.ToString());
    }

    [Fact]
    public void Run_LockedSourceFile_FailsWithHint()
    {
        using var _ = new FileStream(Path.Combine(Source, "NguPhap_N3_File01_Bai1.docx"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var output = new StringWriter();

        var exit = ImportPipeline.Run(Settings, output, new FakeGenerator());

        Assert.Equal(1, exit);
        Assert.Contains("đóng Word", output.ToString());
        Assert.False(File.Exists(Output));
    }

    [Fact]
    public void Run_MissingSourceDirectory_Returns2()
    {
        var exit = ImportPipeline.Run(Settings with { SourceDirectory = Path.Combine(_root, "khong-co") }, new StringWriter(), new FakeGenerator());

        Assert.Equal(2, exit);
    }

    private sealed class FakeGenerator : IFuriganaGenerator
    {
        public string ToRuby(string japanese) => "R:" + japanese;
    }
}
