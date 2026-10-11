using Japan.Core;

namespace Japan.Tests.Core;

public class GrammarSearchTests
{
    private readonly GrammarSearch _search = new(TestBooks.Sample());

    [Theory]
    [InlineData("うちに")]
    [InlineData("～うちに")]
    [InlineData("ウチニ")]
    public void Search_PatternMatchesInKanaVariants(string query)
    {
        var hit = _search.Search(query)[0];

        Assert.Equal("b7-1", hit.Point.Id);
        Assert.Equal("Mẫu", hit.Field);
    }

    [Theory]
    [InlineData("tranh thủ")]
    [InlineData("tranh thu")]
    [InlineData("TRANH THU")]
    public void Search_VietnameseIgnoresDiacriticsAndCase(string query)
    {
        var hit = Assert.Single(_search.Search(query));

        Assert.Equal("b7-1", hit.Point.Id);
        Assert.Equal("Ý nghĩa", hit.Field);
    }

    [Fact]
    public void Search_RanksPatternAboveComparison()
    {
        var hits = _search.Search("間に");

        Assert.Equal(["b7-3", "b7-1"], hits.Select(h => h.Point.Id));
        Assert.Equal("So sánh", hits[1].Field);
    }

    [Fact]
    public void Search_MatchesExampleTranslation()
    {
        var hit = Assert.Single(_search.Search("phu si"));

        Assert.Equal("b7-3", hit.Point.Id);
        Assert.Equal("Ví dụ", hit.Field);
        Assert.Contains("富士山", hit.Snippet);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("～")]
    public void Search_EmptyQuery_ReturnsNothing(string? query) => Assert.Empty(_search.Search(query));

    [Fact]
    public void Search_RespectsLimit()
    {
        Assert.True(_search.Search("vる").Count >= 2);
        Assert.Single(_search.Search("vる", limit: 1));
    }
}
