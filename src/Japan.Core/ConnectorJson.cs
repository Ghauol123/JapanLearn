using System.Text.Json;

namespace Japan.Core;

public static class ConnectorJson
{
    public static string Serialize(ConnectorBook book) => JsonSerializer.Serialize(book, GrammarJson.Options);

    public static ConnectorBook Deserialize(string json) =>
        JsonSerializer.Deserialize<ConnectorBook>(json, GrammarJson.Options)
        ?? throw new InvalidDataException("connectors.json rỗng.");
}
