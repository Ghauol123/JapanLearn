using Japan.Core;
using Japan.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Japan.Tests.Web;

public class ConnectorRepositoryTests
{
    [Fact]
    public void Locate_ReturnsConnectorGroupAndNeighbors()
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();

        var middle = repo.Locate("shitagatte");
        var first = repo.Locate("dakara");

        Assert.Equal("thuan-tiep", middle?.Group.Id);
        Assert.Equal("dakara", middle?.Previous?.Id);
        Assert.Equal("shikashi", middle?.Next?.Id);
        Assert.Null(first?.Previous);
        Assert.Null(repo.Locate("khong-co"));
    }

    [Fact]
    public void Filter_KeepsOnlyLevelAndDropsEmptyGroups()
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();

        var n3 = repo.Filter(JlptLevel.N3);

        var group = Assert.Single(n3);
        Assert.Equal("thuan-tiep", group.Id);
        Assert.Equal(["shitagatte"], group.Connectors.Select(c => c.Id));
        Assert.Equal(2, repo.Filter(null).Count);
        Assert.Equal(3, repo.Count);
    }

    [Fact]
    public void MissingFile_IsNotLoaded_AndGrammarStillLoads()
    {
        using var factory = GrammarWebFactory.WithoutConnectors();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();

        Assert.False(repo.IsLoaded);
        Assert.Contains(factory.ConnectorDataPath, repo.LoadError);
        Assert.Empty(repo.Groups);
        Assert.True(factory.Services.GetRequiredService<GrammarRepository>().IsLoaded);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{"groups":[],"generatedAt":"2026-10-14T00:00:00+00:00"}""")]
    public void Reload_BadFile_KeepsPreviousData(string content)
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();
        Assert.True(repo.IsLoaded);

        File.WriteAllText(factory.ConnectorDataPath, content);

        Assert.False(repo.Reload());
        Assert.NotNull(repo.Locate("dakara"));
        Assert.Contains(factory.ConnectorDataPath, repo.LoadError);
    }

    [Fact]
    public void Reload_DuplicateIds_KeepsPreviousData()
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();
        var sample = TestConnectors.Sample();
        var duplicated = sample with
        {
            Groups = [sample.Groups[0], sample.Groups[1] with { Connectors = [.. sample.Groups[1].Connectors, sample.Groups[0].Connectors[0]] }],
        };

        File.WriteAllText(factory.ConnectorDataPath, ConnectorJson.Serialize(duplicated));

        Assert.False(repo.Reload());
        Assert.Equal(3, repo.Count);
    }
}
