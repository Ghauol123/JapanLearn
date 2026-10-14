using System.Net;
using Japan.Core;

namespace Japan.Tests.Web;

public sealed class PagesTests : IDisposable
{
    private readonly GrammarWebFactory _factory = GrammarWebFactory.WithSampleData();

    public void Dispose() => _factory.Dispose();

    private Task<string> Get(string url) => _factory.CreateClient().GetStringAsync(url);

    [Fact]
    public async Task Index_ListsLessonsAndPatterns()
    {
        var html = await Get("/");

        Assert.Contains("Bài 7: Thời điểm của sự việc", html);
        Assert.Contains("～うちに（１）", html);
        Assert.Contains("href=\"/nguphap/b7-1\"", html);
    }

    [Fact]
    public async Task Lesson_ShowsPointsAndQuickComparison()
    {
        var html = await Get("/bai/7");

        Assert.Contains("～間・～間に", html);
        Assert.Contains("Tranh thủ khi còn...", html);
    }

    [Fact]
    public async Task Grammar_ShowsOriginalEnrichmentRubyAndNavigation()
    {
        var html = await Get("/nguphap/b7-1");

        Assert.Contains("<ruby>熱<rt>あつ</rt></ruby>", html);
        Assert.Contains("Bổ sung", html);
        Assert.Contains("Nhấn mạnh việc tranh thủ.", html);
        Assert.Contains("熱い → 熱いうちに", html);
        Assert.Contains("<ruby>若<rt>わか</rt></ruby>", html);
        Assert.Contains("href=\"/nguphap/b7-3\"", html);
        Assert.Contains("href=\"/nguphap/b6-3\"", html);
    }

    [Fact]
    public async Task Grammar_WithoutEnrichment_HasNoEnrichmentSection()
    {
        var html = await Get("/nguphap/b7-3");

        Assert.Contains("夏休みの間に", html);
        Assert.DoesNotContain("id=\"bo-sung\"", html);
    }

    [Fact]
    public async Task Grammar_TechniquePoint_HidesEmptyStructureAndExamples()
    {
        var technique = new GrammarPoint("b26-3", 26, 3, "Tốc độ làm bài", new Section(["Mỗi câu tối đa 1 phút."], []),
            Section.Empty, null, [], null, "File11.docx");
        using var factory = GrammarWebFactory.WithBook(
            new GrammarBook([new Lesson(26, "Kỹ thuật ráp câu", [technique], [])], DateTimeOffset.UnixEpoch));

        var html = await factory.CreateClient().GetStringAsync("/nguphap/b26-3");

        Assert.Contains("Mỗi câu tối đa 1 phút.", html);
        Assert.DoesNotContain(">Cấu trúc<", html);
        Assert.DoesNotContain(">Ví dụ<", html);
    }

    [Theory]
    [InlineData("/bai/99")]
    [InlineData("/nguphap/b99-1")]
    public async Task UnknownLessonOrPoint_Returns404(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MissingDataFile_ShowsImportHint()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"none-{Guid.NewGuid():N}");
        using var factory = new GrammarWebFactory(Path.Combine(missing, "grammar.json"), Path.Combine(missing, "connectors.json"));

        var response = await factory.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Chưa có dữ liệu", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DataFileChange_IsReloaded()
    {
        var client = _factory.CreateClient();
        await client.GetStringAsync("/");
        var book = TestBooks.Sample();
        var renamed = book with { Lessons = [book.Lessons[0] with { Title = "Tiêu đề mới" }, book.Lessons[1]] };
        File.WriteAllText(_factory.DataPath, GrammarJson.Serialize(renamed));

        var html = "";
        for (var i = 0; i < 50 && !html.Contains("Tiêu đề mới"); i++)
        {
            await Task.Delay(100);
            html = await client.GetStringAsync("/");
        }

        Assert.Contains("Bài 6: Tiêu đề mới", html);
    }
}
