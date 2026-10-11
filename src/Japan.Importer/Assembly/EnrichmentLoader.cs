using System.Text.Json;
using System.Text.RegularExpressions;
using Japan.Core;

namespace Japan.Importer.Assembly;

public sealed record EnrichmentLoadResult(IReadOnlyDictionary<string, GrammarEnrichment> Items, IReadOnlyList<string> Errors);

public static partial class EnrichmentLoader
{
    [GeneratedRegex(@"^b\d+-\d+$")]
    private static partial Regex IdRegex();

    public static EnrichmentLoadResult Load(string directory)
    {
        var items = new Dictionary<string, GrammarEnrichment>(StringComparer.Ordinal);
        var errors = new List<string>();
        if (!Directory.Exists(directory)) return new EnrichmentLoadResult(items, errors);

        foreach (var path in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(path);
            var id = Path.GetFileNameWithoutExtension(path);
            if (!IdRegex().IsMatch(id))
            {
                errors.Add($"{name}: tên file phải có dạng b<bài>-<điểm>.json");
                continue;
            }

            GrammarEnrichment? enrichment;
            try
            {
                enrichment = JsonSerializer.Deserialize<GrammarEnrichment>(File.ReadAllText(path), GrammarJson.Options);
            }
            catch (JsonException ex)
            {
                errors.Add($"{name}: JSON không hợp lệ — {ex.Message}");
                continue;
            }

            if (enrichment is null || string.IsNullOrWhiteSpace(enrichment.Explanation))
            {
                errors.Add($"{name}: thiếu \"explanation\"");
                continue;
            }

            items[id] = enrichment with
            {
                Formation = enrichment.Formation ?? [],
                Usage = enrichment.Usage ?? [],
                Pitfalls = enrichment.Pitfalls ?? [],
                Comparisons = enrichment.Comparisons ?? [],
                Examples = enrichment.Examples ?? [],
            };
        }

        return new EnrichmentLoadResult(items, errors);
    }
}
