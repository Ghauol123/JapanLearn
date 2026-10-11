using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Japan.Core;

public static class GrammarJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(GrammarBook book) => JsonSerializer.Serialize(book, Options);

    public static GrammarBook Deserialize(string json) =>
        JsonSerializer.Deserialize<GrammarBook>(json, Options)
        ?? throw new InvalidDataException("grammar.json rỗng.");
}
