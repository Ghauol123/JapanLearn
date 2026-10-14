using System.Text.Json;
using Japan.Core;
using Japan.Importer;
using Japan.Importer.Furigana;

namespace Japan.Tests.Importer;

public sealed class ConnectorPipelineTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("connector-import-").FullName;
    private string Content => Path.Combine(_root, "connectors");
    private string Output => Path.Combine(_root, "data", "connectors.json");

    public ConnectorPipelineTests()
    {
        Directory.CreateDirectory(Content);
        var sample = TestConnectors.Sample();
        var thuanTiep = sample.Groups[0];
        // Ghi ngược thứ tự cấp (N3 trước N4) để kiểm tra việc sắp xếp.
        var reversed = thuanTiep with { Connectors = [thuanTiep.Connectors[1], thuanTiep.Connectors[0]] };
        File.WriteAllText(Path.Combine(Content, "thuan-tiep.json"), JsonSerializer.Serialize(reversed, GrammarJson.Options));
        File.WriteAllText(Path.Combine(Content, "nghich-tiep.json"), JsonSerializer.Serialize(sample.Groups[1], GrammarJson.Options));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Run_WritesSortedBookWithFurigana()
    {
        var output = new StringWriter();

        var exit = ConnectorPipeline.Run(Content, Output, output, new FakeGenerator());

        Assert.Equal(0, exit);
        var book = ConnectorJson.Deserialize(File.ReadAllText(Output));
        Assert.Equal(["thuan-tiep", "nghich-tiep"], book.Groups.Select(g => g.Id));
        Assert.Equal(["dakara", "shitagatte"], book.Groups[0].Connectors.Select(c => c.Id));
        Assert.Equal("R:高い。しかし、品質はいい。", book.Groups[1].Connectors[0].Examples[0].Ruby);
        Assert.Contains("2 nhóm, 3 từ nối", output.ToString());
        Assert.Contains("N5: 1, N4: 1, N3: 1, N2: 0", output.ToString());
    }

    [Fact]
    public void Run_BadContent_FailsWithoutOverwritingOutput()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Output)!);
        File.WriteAllText(Output, "cũ");
        File.WriteAllText(Path.Combine(Content, "hong.json"), "{ hỏng");
        var output = new StringWriter();

        var exit = ConnectorPipeline.Run(Content, Output, output, new FakeGenerator());

        Assert.Equal(1, exit);
        Assert.Equal("cũ", File.ReadAllText(Output));
        Assert.Contains("hong.json", output.ToString());
    }

    private sealed class FakeGenerator : IFuriganaGenerator
    {
        public string ToRuby(string japanese) => "R:" + japanese;
    }
}
