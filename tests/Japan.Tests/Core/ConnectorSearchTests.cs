using Japan.Core;

namespace Japan.Tests.Core;

public class ConnectorSearchTests
{
    private readonly ConnectorSearch _search = new(TestConnectors.Sample());

    [Theory]
    [InlineData("cho nen")]
    [InlineData("CHO NÊN")]
    public void Search_VietnameseIgnoresDiacriticsAndCase(string query)
    {
        var hit = Assert.Single(_search.Search(query));

        Assert.Equal("dakara", hit.Connector.Id);
        Assert.Equal("Nghĩa", hit.Field);
    }

    [Theory]
    [InlineData("しかし")]
    [InlineData("シカシ")]
    public void Search_WordMatchesInKanaVariants(string query)
    {
        var hit = _search.Search(query)[0];

        Assert.Equal("shikashi", hit.Connector.Id);
        Assert.Equal("Từ nối", hit.Field);
    }

    [Fact]
    public void Search_RanksWordAboveComparison()
    {
        var hits = _search.Search("したがって");

        Assert.Equal(["shitagatte", "dakara"], hits.Select(h => h.Connector.Id));
        Assert.Equal("So sánh", hits[1].Field);
    }

    [Fact]
    public void Search_MatchesReadingWhenWordIsKanji()
    {
        var ippou = TestConnectors.Sample().Groups[0].Connectors[0] with { Id = "ippou", Word = "一方", Reading = "いっぽう" };
        var search = new ConnectorSearch(new ConnectorBook(
            [new ConnectorGroup("doi-lap", 1, "Đối lập", "対比", "Mô tả.", [ippou])], DateTimeOffset.UnixEpoch));

        var hit = Assert.Single(search.Search("イッポウ"));

        Assert.Equal("ippou", hit.Connector.Id);
        Assert.Equal("Từ nối", hit.Field);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Search_EmptyQuery_ReturnsNothing(string? query) => Assert.Empty(_search.Search(query));
}
