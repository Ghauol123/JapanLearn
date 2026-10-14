using System.Text.RegularExpressions;

namespace Japan.Core;

public static partial class ConnectorValidator
{
    public const int MinExamples = 2;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex IdRegex();

    public static IReadOnlyList<string> Validate(IReadOnlyList<ConnectorGroup>? groups)
    {
        var errors = new List<string>();
        if (groups is null || groups.Count == 0)
        {
            errors.Add("Không có nhóm từ nối nào.");
            return errors;
        }

        var groupIds = new HashSet<string>(StringComparer.Ordinal);
        var orders = new HashSet<int>();
        var all = new List<(string Where, Connector Connector)>();

        foreach (var g in groups)
        {
            var name = string.IsNullOrWhiteSpace(g.Id) ? "(nhóm không có id)" : g.Id;
            if (string.IsNullOrWhiteSpace(g.Id) || !IdRegex().IsMatch(g.Id))
                errors.Add($"{name}: id phải là chữ thường không dấu, nối bằng '-'");
            else if (!groupIds.Add(g.Id))
                errors.Add($"{name}: trùng id nhóm");
            if (!orders.Add(g.Order)) errors.Add($"{name}: trùng order {g.Order} với nhóm khác");
            if (string.IsNullOrWhiteSpace(g.Title)) errors.Add($"{name}: thiếu \"title\"");
            if (string.IsNullOrWhiteSpace(g.JaTitle)) errors.Add($"{name}: thiếu \"jaTitle\"");
            if (string.IsNullOrWhiteSpace(g.Description)) errors.Add($"{name}: thiếu \"description\"");
            if (g.Connectors is null || g.Connectors.Count == 0)
            {
                errors.Add($"{name}: không có từ nối nào");
                continue;
            }

            foreach (var c in g.Connectors)
                all.Add(($"{name}/{(string.IsNullOrWhiteSpace(c.Id) ? "(không có id)" : c.Id)}", c));
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (where, c) in all)
        {
            if (string.IsNullOrWhiteSpace(c.Id) || !IdRegex().IsMatch(c.Id))
                errors.Add($"{where}: id phải là chữ thường không dấu, nối bằng '-'");
            else if (!ids.Add(c.Id))
                errors.Add($"{where}: trùng id từ nối");

            foreach (var (field, value) in new[] { ("word", c.Word), ("reading", c.Reading), ("meaning", c.Meaning), ("explanation", c.Explanation) })
            {
                if (string.IsNullOrWhiteSpace(value)) errors.Add($"{where}: thiếu \"{field}\"");
            }

            if (!Enum.IsDefined(c.Level)) errors.Add($"{where}: level phải là N5, N4, N3 hoặc N2");
            if (!Enum.IsDefined(c.Register)) errors.Add($"{where}: register phải là spoken, written hoặc both");

            var examples = c.Examples ?? [];
            if (examples.Count < MinExamples) errors.Add($"{where}: cần ít nhất {MinExamples} ví dụ");
            if (examples.Any(e => string.IsNullOrWhiteSpace(e.Japanese) || string.IsNullOrWhiteSpace(e.Vietnamese)))
                errors.Add($"{where}: ví dụ thiếu câu tiếng Nhật hoặc bản dịch");
        }

        foreach (var (where, c) in all)
        {
            foreach (var cmp in c.Comparisons ?? [])
            {
                if (cmp.TargetId is null) continue;
                if (cmp.TargetId == c.Id) errors.Add($"{where}: so sánh trỏ về chính nó");
                else if (!ids.Contains(cmp.TargetId)) errors.Add($"{where}: targetId \"{cmp.TargetId}\" không tồn tại");
            }
        }

        return errors;
    }
}
