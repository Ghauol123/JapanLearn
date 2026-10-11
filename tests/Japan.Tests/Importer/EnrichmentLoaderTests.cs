using Japan.Importer.Assembly;

namespace Japan.Tests.Importer;

public sealed class EnrichmentLoaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("enrich-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Load_ReadsValidFile_AndDefaultsMissingLists()
    {
        File.WriteAllText(Path.Combine(_dir, "b6-3.json"), """
            {
              "explanation": "Dạng văn viết của ～ないで.",
              "formation": [ { "wordType": "Động từ nhóm 3", "rule": "する → せずに", "samples": ["勉強する → 勉強せずに"] } ],
              "examples": [ { "japanese": "朝ご飯を食べずに学校へ行った。", "vietnamese": "Tôi đến trường mà không ăn sáng." } ]
            }
            """);

        var result = EnrichmentLoader.Load(_dir);

        Assert.Empty(result.Errors);
        var e = result.Items["b6-3"];
        Assert.Equal("Dạng văn viết của ～ないで.", e.Explanation);
        Assert.Equal("する → せずに", e.Formation[0].Rule);
        Assert.Empty(e.Usage);
        Assert.Empty(e.Comparisons);
        Assert.Null(e.Examples[0].Ruby);
    }

    [Fact]
    public void Load_ReportsBadJsonBadNameAndMissingExplanation_WithFileNames()
    {
        File.WriteAllText(Path.Combine(_dir, "b1-1.json"), "{ \"explanation\": ");
        File.WriteAllText(Path.Combine(_dir, "zuni.json"), "{ \"explanation\": \"x\" }");
        File.WriteAllText(Path.Combine(_dir, "b1-2.json"), "{ \"usage\": [] }");

        var result = EnrichmentLoader.Load(_dir);

        Assert.Empty(result.Items);
        Assert.Contains(result.Errors, e => e.StartsWith("b1-1.json") && e.Contains("JSON"));
        Assert.Contains(result.Errors, e => e.StartsWith("zuni.json"));
        Assert.Contains(result.Errors, e => e.StartsWith("b1-2.json") && e.Contains("explanation"));
    }

    [Fact]
    public void Load_MissingDirectory_ReturnsEmpty()
    {
        var result = EnrichmentLoader.Load(Path.Combine(_dir, "khong-co"));

        Assert.Empty(result.Items);
        Assert.Empty(result.Errors);
    }
}
