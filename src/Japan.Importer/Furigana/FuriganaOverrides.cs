using System.Text.Json;

namespace Japan.Importer.Furigana;

public static class FuriganaOverrides
{
    public static IReadOnlyDictionary<string, string> Load(string path)
    {
        if (!File.Exists(path)) return new Dictionary<string, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                   ?? new Dictionary<string, string>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)}: JSON không hợp lệ — {ex.Message}", ex);
        }
    }
}
