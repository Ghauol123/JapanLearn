using System.Net;

namespace Japan.Tests.Web;

public sealed class ConnectorPagesTests : IDisposable
{
    private readonly GrammarWebFactory _factory = GrammarWebFactory.WithSampleData();

    public void Dispose() => _factory.Dispose();

    private Task<string> Get(string url) => _factory.CreateClient().GetStringAsync(url);

    [Fact]
    public async Task Detail_ShowsContentRubyAndLinks()
    {
        var html = await Get("/tu-noi/dakara");

        Assert.Contains("vì vậy, cho nên", html);
        Assert.Contains("Văn nói", html);
        Assert.Contains("Nối nguyên nhân ở câu trước", html);
        Assert.Contains("Không dùng trong văn bản trang trọng.", html);
        Assert.Contains("<ruby>雨<rt>あめ</rt></ruby>", html);
        Assert.Contains("href=\"/tu-noi/shitagatte\"", html);
        Assert.Contains("rel=\"next\"", html);
        Assert.DoesNotContain("rel=\"prev\"", html);
    }

    [Fact]
    public async Task Detail_MiddleItem_HasBothNeighbors_AndPlainComparison()
    {
        var middle = await Get("/tu-noi/shitagatte");
        var last = await Get("/tu-noi/shikashi");

        Assert.Contains("href=\"/tu-noi/dakara\"", middle);
        Assert.Contains("href=\"/tu-noi/shikashi\"", middle);
        Assert.Contains("でも thân mật hơn", last);
        Assert.DoesNotContain("rel=\"next\"", last);
    }

    [Fact]
    public async Task Detail_KanjiWord_ShowsReading()
    {
        var sample = TestConnectors.Sample();
        var ippou = sample.Groups[1].Connectors[0] with { Id = "ippou", Word = "一方", Reading = "いっぽう", Comparisons = [] };
        using var factory = GrammarWebFactory.WithConnectors(sample with
        {
            Groups = [sample.Groups[0], sample.Groups[1] with { Connectors = [ippou] }],
        });

        var page = await factory.CreateClient().GetStringAsync("/tu-noi/ippou");

        Assert.Contains("一方", page);
        Assert.Contains("いっぽう", page);
    }

    [Fact]
    public async Task Detail_UnknownId_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/tu-noi/khong-co");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
