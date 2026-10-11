using Japan.Core;
using Japan.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Japan.Tests.Web;

public class GrammarRepositoryTests
{
    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{"lessons":[{"number":1,"title":"x","points":null,"quickComparisons":[]}]}""")]
    public void Reload_BadFile_KeepsPreviousDataAndReportsError(string content)
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<GrammarRepository>();
        Assert.True(repo.IsLoaded);

        File.WriteAllText(factory.DataPath, content);

        Assert.False(repo.Reload());
        Assert.Equal(2, repo.Lessons.Count);
        Assert.NotNull(repo.Find("b7-1"));
        Assert.Contains(factory.DataPath, repo.LoadError);
    }

    [Fact]
    public void Reload_DuplicateIds_KeepsPreviousData()
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<GrammarRepository>();
        var point = TestBooks.Point(1, 1, "～x", "Nghĩa.");
        var duplicated = new GrammarBook([new Lesson(1, "Bài 1", [point, point], [])], DateTimeOffset.UnixEpoch);

        File.WriteAllText(factory.DataPath, GrammarJson.Serialize(duplicated));

        Assert.False(repo.Reload());
        Assert.NotNull(repo.Find("b7-1"));
    }

    [Fact]
    public void Locate_ReturnsPointAndNeighborsInBookOrder()
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<GrammarRepository>();

        var (point, previous, next) = repo.Locate("b7-1");

        Assert.Equal("b7-1", point?.Id);
        Assert.Equal("b6-3", previous?.Id);
        Assert.Equal("b7-3", next?.Id);
        Assert.Equal((null, null, null), repo.Locate("b99-1"));
    }
}
