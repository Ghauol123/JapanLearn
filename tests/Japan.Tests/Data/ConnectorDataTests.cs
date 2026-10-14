using System.Text.Json;
using Japan.Core;
using Japan.Importer;
using Japan.Importer.Assembly;

namespace Japan.Tests.Data;

public class ConnectorDataTests
{
    private static readonly string Root = RepoRoot.Find(AppContext.BaseDirectory);

    private static ConnectorBook Data() =>
        ConnectorJson.Deserialize(File.ReadAllText(Path.Combine(Root, "data", "connectors.json")));

    [Fact]
    public void ConnectorsJson_IsValid_AndEveryExampleHasFurigana()
    {
        var book = Data();

        Assert.Empty(ConnectorValidator.Validate(book.Groups));
        Assert.All(book.Groups.SelectMany(g => g.Connectors).SelectMany(c => c.Examples),
            e => Assert.False(string.IsNullOrEmpty(e.Ruby), e.Japanese));
    }

    [Fact]
    public void ConnectorsJson_MatchesContentFiles()
    {
        var content = ConnectorLoader.Load(Path.Combine(Root, "content", "connectors"));
        Assert.Empty(content.Errors);

        static string Key(Connector c) => JsonSerializer.Serialize(
            c with { Examples = c.Examples.Select(e => e with { Ruby = null }).ToList() }, GrammarJson.Options);
        static string GroupKey(ConnectorGroup g) => $"{g.Id}|{g.Order}|{g.Title}|{g.JaTitle}|{g.Description}";

        var data = Data();
        Assert.Equal(content.Groups.Select(GroupKey).Order(), data.Groups.Select(GroupKey).Order());
        Assert.Equal(
            content.Groups.SelectMany(g => g.Connectors).Select(Key).Order(),
            data.Groups.SelectMany(g => g.Connectors).Select(Key).Order());
    }
}
