using System.Text.Json.Serialization;

namespace Japan.Core;

[JsonConverter(typeof(JsonStringEnumConverter<JlptLevel>))]
public enum JlptLevel
{
    N5,
    N4,
    N3,
    N2,
}

[JsonConverter(typeof(JsonStringEnumConverter<ConnectorRegister>))]
public enum ConnectorRegister
{
    [JsonStringEnumMemberName("spoken")] Spoken,
    [JsonStringEnumMemberName("written")] Written,
    [JsonStringEnumMemberName("both")] Both,
}

public static class JlptLevels
{
    public static IReadOnlyList<JlptLevel> All { get; } = [JlptLevel.N5, JlptLevel.N4, JlptLevel.N3, JlptLevel.N2];

    public static bool TryParse(string? text, out JlptLevel level)
    {
        var trimmed = text?.Trim();
        foreach (var candidate in All)
        {
            if (string.Equals(candidate.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                level = candidate;
                return true;
            }
        }

        level = default;
        return false;
    }
}

public sealed record ConnectorBook(IReadOnlyList<ConnectorGroup> Groups, DateTimeOffset GeneratedAt);

public sealed record ConnectorGroup(
    string Id,
    int Order,
    string Title,
    string JaTitle,
    string Description,
    IReadOnlyList<Connector> Connectors);

public sealed record Connector(
    string Id,
    string Word,
    string Reading,
    JlptLevel Level,
    string Meaning,
    ConnectorRegister Register,
    string Explanation,
    IReadOnlyList<string> Usage,
    IReadOnlyList<string> Pitfalls,
    IReadOnlyList<Comparison> Comparisons,
    IReadOnlyList<Example> Examples);
