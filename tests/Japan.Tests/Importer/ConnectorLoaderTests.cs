using System.Text.Json;
using Japan.Core;
using Japan.Importer.Assembly;

namespace Japan.Tests.Importer;

public sealed class ConnectorLoaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("connectors-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private void Write(ConnectorGroup group, string? fileName = null) =>
        File.WriteAllText(Path.Combine(_dir, (fileName ?? group.Id) + ".json"), JsonSerializer.Serialize(group, GrammarJson.Options));

    private void WriteSample()
    {
        foreach (var g in TestConnectors.Sample().Groups) Write(g);
    }

    [Fact]
    public void Load_ValidFiles_ReturnsGroups()
    {
        WriteSample();

        var result = ConnectorLoader.Load(_dir);

        Assert.Empty(result.Errors);
        Assert.Equal(["nghich-tiep", "thuan-tiep"], result.Groups.Select(g => g.Id));
    }

    [Fact]
    public void Load_MissingLists_BecomeEmpty()
    {
        WriteSample();
        File.WriteAllText(Path.Combine(_dir, "nghich-tiep.json"), """
            {
              "id": "nghich-tiep", "order": 2, "title": "Nghịch tiếp", "jaTitle": "逆接", "description": "Mô tả.",
              "connectors": [
                { "id": "demo", "word": "でも", "reading": "でも", "level": "N5", "meaning": "nhưng", "register": "spoken",
                  "explanation": "Giải thích.",
                  "examples": [ { "japanese": "a。でも、b。", "vietnamese": "x" }, { "japanese": "c。でも、d。", "vietnamese": "y" } ] }
              ]
            }
            """);

        var result = ConnectorLoader.Load(_dir);

        Assert.Empty(result.Errors);
        var demo = result.Groups.Single(g => g.Id == "nghich-tiep").Connectors[0];
        Assert.Empty(demo.Usage);
        Assert.Empty(demo.Pitfalls);
        Assert.Empty(demo.Comparisons);
    }

    [Fact]
    public void Load_BrokenJson_ReportsFile()
    {
        WriteSample();
        File.WriteAllText(Path.Combine(_dir, "hong.json"), "{ hỏng");

        Assert.Contains(ConnectorLoader.Load(_dir).Errors, e => e.Contains("hong.json"));
    }

    [Fact]
    public void Load_GroupIdDifferentFromFileName_IsError()
    {
        WriteSample();
        Write(TestConnectors.Sample().Groups[1] with { Order = 9 }, "ten-khac");

        Assert.Contains(ConnectorLoader.Load(_dir).Errors, e => e.Contains("ten-khac.json") && e.Contains("nghich-tiep"));
    }

    [Fact]
    public void Load_RunsValidator()
    {
        var group = TestConnectors.Sample().Groups[1];
        Write(group with { Connectors = [group.Connectors[0] with { Examples = [group.Connectors[0].Examples[0]] }] });

        Assert.Contains(ConnectorLoader.Load(_dir).Errors, e => e.Contains("ít nhất 2 ví dụ"));
    }

    [Fact]
    public void Load_EmptyOrMissingDirectory_IsError()
    {
        Assert.NotEmpty(ConnectorLoader.Load(_dir).Errors);
        Assert.NotEmpty(ConnectorLoader.Load(Path.Combine(_dir, "khong-co")).Errors);
    }
}
