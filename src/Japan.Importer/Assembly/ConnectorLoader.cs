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
                group = JsonSerializer.Deserialize<ConnectorGroup>(File.ReadAllText(path), GrammarJson.Options);
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
