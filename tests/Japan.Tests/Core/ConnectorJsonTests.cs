using System.Text.Json;
using Japan.Core;

namespace Japan.Tests.Core;

public class ConnectorJsonTests
{
    [Fact]
    public void RoundTrip_KeepsContent_AndWritesEnumsAsStrings()
    {
        var json = ConnectorJson.Serialize(TestConnectors.Sample());

        var again = ConnectorJson.Serialize(ConnectorJson.Deserialize(json));

        Assert.Equal(json, again);
        Assert.Contains("\"level\": \"N3\"", json);
        Assert.Contains("\"register\": \"written\"", json);
        Assert.Contains("だから", json);
    }

    [Fact]
    public void Deserialize_UnknownLevel_Throws()
    {
        var json = ConnectorJson.Serialize(TestConnectors.Sample()).Replace("\"N4\"", "\"N9\"");

        Assert.Throws<JsonException>(() => ConnectorJson.Deserialize(json));
    }

    [Theory]
    [InlineData("N3", true, JlptLevel.N3)]
    [InlineData("n3", true, JlptLevel.N3)]
    [InlineData(" N2 ", true, JlptLevel.N2)]
    [InlineData("N5", true, JlptLevel.N5)]
    [InlineData("3", false, default(JlptLevel))]
    [InlineData("N1", false, default(JlptLevel))]
    [InlineData("", false, default(JlptLevel))]
    [InlineData(null, false, default(JlptLevel))]
    public void TryParse_AcceptsOnlyLevelNames(string? text, bool ok, JlptLevel expected)
    {
        Assert.Equal(ok, JlptLevels.TryParse(text, out var level));
        if (ok) Assert.Equal(expected, level);
    }
}
