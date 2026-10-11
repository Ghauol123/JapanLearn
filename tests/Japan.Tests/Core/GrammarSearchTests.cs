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

    [Fact]
    public void Search_LongField_SnippetContainsMatchPastTheStart()
    {
        var meaning = new string('a', 300) + " điểm mấu chốt " + new string('b', 300);
        var book = new GrammarBook([new Lesson(1, "Bài 1", [TestBooks.Point(1, 1, "～x", meaning)], [])], DateTimeOffset.UnixEpoch);

        var hit = Assert.Single(new GrammarSearch(book).Search("diem mau chot"));

        Assert.Contains("điểm mấu chốt", hit.Snippet);
        Assert.StartsWith("…", hit.Snippet);
        Assert.EndsWith("…", hit.Snippet);
        Assert.Equal(160, hit.Snippet.Length);
    }

    [Theory]
    [InlineData("Tranh thủ làm", "tranh thu", 0, 9)]
    [InlineData("Hãy ăn khi CÒN nóng", "con nong", 11, 8)]
    [InlineData("熱いうちに食べて", "ウチニ", 2, 3)]
    [InlineData("A  B", "a b", 0, 4)]
    public void FindIn_ReturnsRangeInRawText(string raw, string query, int start, int length) =>
        Assert.Equal((start, length), TextNormalizer.FindIn(raw, TextNormalizer.Normalize(query)));

    [Fact]
    public void FindIn_NoMatch_ReturnsNull() => Assert.Null(TextNormalizer.FindIn("abc", "xyz"));

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
