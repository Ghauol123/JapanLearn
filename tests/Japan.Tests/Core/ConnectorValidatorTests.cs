using Japan.Core;

namespace Japan.Tests.Core;

public class ConnectorValidatorTests
{
    private static IReadOnlyList<ConnectorGroup> Groups => TestConnectors.Sample().Groups;

    private static IReadOnlyList<ConnectorGroup> WithFirst(Func<Connector, Connector> change)
    {
        var g = Groups;
        return [g[0] with { Connectors = [change(g[0].Connectors[0]), .. g[0].Connectors.Skip(1)] }, .. g.Skip(1)];
    }

    private static string Single(IReadOnlyList<ConnectorGroup> groups) => Assert.Single(ConnectorValidator.Validate(groups));

    [Fact]
    public void Sample_IsValid() => Assert.Empty(ConnectorValidator.Validate(Groups));

    [Fact]
    public void NoGroups_IsError()
    {
        Assert.NotEmpty(ConnectorValidator.Validate(null));
        Assert.NotEmpty(ConnectorValidator.Validate([]));
    }

    [Fact]
    public void DuplicateConnectorIdAcrossGroups_IsError()
    {
        var g = Groups;
        var groups = new[] { g[0], g[1] with { Connectors = [.. g[1].Connectors, g[0].Connectors[0]] } };

        Assert.Contains("trùng id", Single(groups));
    }

    [Theory]
    [InlineData("Da Kara")]
    [InlineData("dakara-")]
    [InlineData("")]
    public void BadConnectorId_IsError(string id) =>
        Assert.Contains(ConnectorValidator.Validate(WithFirst(c => c with { Id = id })), e => e.Contains("id phải là"));

    [Fact]
    public void DuplicateGroupOrder_IsError()
    {
        var g = Groups;

        Assert.Contains("trùng order", Single([g[0], g[1] with { Order = g[0].Order }]));
    }

    [Fact]
    public void UndefinedLevelOrRegister_IsError()
    {
        Assert.Contains("level", Single(WithFirst(c => c with { Level = (JlptLevel)99 })));
        Assert.Contains("register", Single(WithFirst(c => c with { Register = (ConnectorRegister)99 })));
    }

    [Fact]
    public void MissingMeaning_IsError() =>
        Assert.Contains("\"meaning\"", Single(WithFirst(c => c with { Meaning = " " })));

    [Fact]
    public void FewerThanTwoExamples_IsError() =>
        Assert.Contains("ít nhất 2 ví dụ", Single(WithFirst(c => c with { Examples = [c.Examples[0]] })));

    [Fact]
    public void UnknownTargetId_IsError() =>
        Assert.Contains("\"khong-co\"", Single(WithFirst(c => c with { Comparisons = [new Comparison("x", "khong-co", "y")] })));

    [Fact]
    public void TargetIdPointingToItself_IsError() =>
        Assert.Contains("chính nó", Single(WithFirst(c => c with { Comparisons = [new Comparison("x", c.Id, "y")] })));
}
