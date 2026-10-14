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

    [Fact]
    public async Task List_ShowsGroupsAndConnectors()
    {
        var html = await Get("/tu-noi");

        Assert.Contains("Thuận tiếp — kết quả, hệ quả", html);
        Assert.Contains("順接", html);
        Assert.Contains("Nghịch tiếp — trái ngược", html);
        Assert.Contains("href=\"/tu-noi/dakara\"", html);
        Assert.Contains("vì vậy, cho nên", html);
        Assert.Contains("href=\"/tu-noi?cap=N3\"", html);
    }

    [Theory]
    [InlineData("/tu-noi?cap=N3")]
    [InlineData("/tu-noi?cap=n3")]
    [InlineData("/tu-noi?cap=%20N3%20")]
    public async Task List_FilterByLevel_HidesOtherLevelsAndEmptyGroups(string url)
    {
        var html = await Get(url);

        Assert.Contains("href=\"/tu-noi/shitagatte\"", html);
        Assert.DoesNotContain("href=\"/tu-noi/dakara\"", html);
        Assert.DoesNotContain("Nghịch tiếp — trái ngược", html);
    }

    [Theory]
    [InlineData("/tu-noi?cap=abc")]
    [InlineData("/tu-noi?cap=3")]
    [InlineData("/tu-noi?cap=N1")]
    public async Task List_UnknownLevel_ShowsAll(string url)
    {
        var html = await Get(url);

        Assert.Contains("href=\"/tu-noi/dakara\"", html);
        Assert.Contains("href=\"/tu-noi/shikashi\"", html);
    }

    [Fact]
    public async Task HeaderAndIndex_LinkToConnectors()
    {
        var index = await Get("/");
        var lesson = await Get("/bai/7");

        Assert.Contains("3 từ nối theo chức năng", index);
        Assert.Contains("href=\"/tu-noi\"", lesson);
    }

    [Fact]
    public async Task MissingConnectors_ListShowsHint_IndexStillWorks()
    {
        using var factory = GrammarWebFactory.WithoutConnectors();
        var client = factory.CreateClient();

        var list = await client.GetStringAsync("/tu-noi");
        var index = await client.GetStringAsync("/");

        Assert.Contains("Chưa có dữ liệu từ nối", list);
        Assert.Contains("-- connectors", list);
        Assert.Contains("Thời điểm của sự việc", index);
        Assert.DoesNotContain("từ nối theo chức năng", index);
    }

    [Fact]
    public async Task List_KanjiWord_ShowsReadingOnCard()
    {
        var sample = TestConnectors.Sample();
        var ippou = sample.Groups[1].Connectors[0] with { Id = "ippou", Word = "一方", Reading = "いっぽう", Comparisons = [] };
        using var factory = GrammarWebFactory.WithConnectors(sample with
        {
            Groups = [sample.Groups[0], sample.Groups[1] with { Connectors = [ippou] }],
        });

        var html = await factory.CreateClient().GetStringAsync("/tu-noi");

        Assert.Contains("connector-reading ja\">いっぽう<", html);
    }
}
