using System.Text.Json;
using Japan.Core;

namespace Japan.Importer.Assembly;

public sealed record ConnectorLoadResult(IReadOnlyList<ConnectorGroup> Groups, IReadOnlyList<string> Errors);

public static class ConnectorLoader
{
    public static ConnectorLoadResult Load(string directory)
    {
        var groups = new List<ConnectorGroup>();
        var errors = new List<string>();
        if (!Directory.Exists(directory))
        {
            errors.Add($"Không tìm thấy thư mục {directory}");
            return new ConnectorLoadResult(groups, errors);
        }

        var files = Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal).ToList();
        if (files.Count == 0)
        {
            errors.Add($"Không có file .json nào trong {directory}");
            return new ConnectorLoadResult(groups, errors);
        }

        foreach (var path in files)
        {
            var name = Path.GetFileName(path);
            var id = Path.GetFileNameWithoutExtension(path);

            ConnectorGroup? group;
            try
            {
                var json = File.ReadAllText(path);
                var missing = MissingFields(json);
                if (missing.Count > 0)
                {
                    errors.AddRange(missing.Select(m => $"{name}: {m}"));
                    continue;
                }

                group = JsonSerializer.Deserialize<ConnectorGroup>(json, GrammarJson.Options);
            }
            catch (JsonException ex)
            {
                errors.Add($"{name}: JSON không hợp lệ — {ex.Message}");
                continue;
            }

            if (group is null)
            {
                errors.Add($"{name}: file rỗng");
                continue;
            }

            if (group.Id != id) errors.Add($"{name}: \"id\" là \"{group.Id}\" nhưng tên file là \"{id}\"");
            groups.Add(Normalize(group));
        }

        if (groups.Count > 0) errors.AddRange(ConnectorValidator.Validate(groups));
        return new ConnectorLoadResult(groups, errors);
    }

    // Trường enum/số bị bỏ trống sẽ nhận giá trị mặc định (N5, spoken, order 0) mà không báo lỗi, nên phải kiểm tra trước khi đọc.
    private static List<string> MissingFields(string json)
    {
        var missing = new List<string>();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return missing;
        if (!root.TryGetProperty("order", out _)) missing.Add("thiếu \"order\"");
        if (!root.TryGetProperty("connectors", out var connectors) || connectors.ValueKind != JsonValueKind.Array) return missing;

        var index = 0;
        foreach (var c in connectors.EnumerateArray())
        {
            index++;
            if (c.ValueKind != JsonValueKind.Object) continue;
            var where = c.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : $"từ nối thứ {index}";
            foreach (var field in new[] { "level", "register" })
            {
                if (!c.TryGetProperty(field, out _)) missing.Add($"{where}: thiếu \"{field}\"");
            }
        }

        return missing;
    }

    private static ConnectorGroup Normalize(ConnectorGroup group) => group with
    {
        Connectors = (group.Connectors ?? []).Select(c => c with
        {
            Usage = c.Usage ?? [],
            Pitfalls = c.Pitfalls ?? [],
            Comparisons = c.Comparisons ?? [],
            Examples = c.Examples ?? [],
        }).ToList(),
    };
}
