using System.Net;

namespace Japan.Tests.Web;

public sealed class SearchPageTests : IDisposable
{
    private readonly GrammarWebFactory _factory = GrammarWebFactory.WithSampleData();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task FullPage_FindsByVietnameseWithoutDiacritics()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?q=tranh%20thu");

        Assert.Contains("<html", html);
        Assert.Contains("～うちに（１）", html);
        Assert.Contains("1 kết quả", html);
    }

    [Fact]
    public async Task Partial_ReturnsOnlyResults_WithHighlight()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?handler=Partial&q=%E9%96%93%E3%81%AB");

        Assert.DoesNotContain("<html", html);
        Assert.Contains("<mark>間に</mark>", html);
        Assert.Contains("href=\"/nguphap/b7-3\"", html);
    }

    [Fact]
    public async Task NoMatch_ShowsMessage()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?q=xyzxyz");

        Assert.Contains("Không tìm thấy kết quả", html);
    }

    [Theory]
    [InlineData("/timkiem")]
    [InlineData("/timkiem?q=%20%20")]
    [InlineData("/timkiem?q=%EF%BD%9E")]
    [InlineData("/timkiem?handler=Partial&q=")]
    public async Task EmptyQueries_Return200WithoutResults(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("kết quả", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FullPage_FindsConnectors_InTheirOwnGroup()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?q=cho%20nen");

        Assert.Contains("class=\"result-group\">Từ nối<", html);
        Assert.DoesNotContain("class=\"result-group\">Ngữ pháp<", html);
        Assert.Contains("href=\"/tu-noi/dakara\"", html);
        Assert.Contains("1 kết quả", html);
    }

    [Fact]
    public async Task GrammarOnlyQuery_HasNoConnectorGroup()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?q=tranh%20thu");

        Assert.Contains("class=\"result-group\">Ngữ pháp<", html);
        Assert.DoesNotContain("class=\"result-group\">Từ nối<", html);
    }

    [Fact]
    public async Task Partial_IncludesConnectorHits()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?handler=Partial&q=%E3%81%97%E3%81%8B%E3%81%97");

        Assert.DoesNotContain("<html", html);
        Assert.Contains("href=\"/tu-noi/shikashi\"", html);
        Assert.Contains("<mark>しかし</mark>", html);
    }
}
