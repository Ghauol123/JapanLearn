using System.Globalization;
using System.Text.RegularExpressions;

namespace Japan.Importer;

public static partial class SourceDiscovery
{
    [GeneratedRegex(@"^NguPhap_N3_File(\d+)_.+\.(docx|md)$", RegexOptions.IgnoreCase)]
    private static partial Regex FileRegex();

    public static IReadOnlyList<string> Find(string directory) =>
        Directory.EnumerateFiles(directory)
            .Select(file => (File: file, Match: FileRegex().Match(Path.GetFileName(file))))
            .Where(x => x.Match.Success)
            .GroupBy(x => int.Parse(x.Match.Groups[1].Value, CultureInfo.InvariantCulture))
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(x => x.Match.Groups[2].Value.Equals("md", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().File)
            .ToList();
}
