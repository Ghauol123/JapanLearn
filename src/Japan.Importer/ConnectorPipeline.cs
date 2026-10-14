using Japan.Core;
using Japan.Importer.Assembly;
using Japan.Importer.Furigana;

namespace Japan.Importer;

public static class ConnectorPipeline
{
    public static int Run(string contentDirectory, string outputPath, TextWriter output, IFuriganaGenerator generator, TimeProvider? clock = null)
    {
        var loaded = ConnectorLoader.Load(contentDirectory);
        if (loaded.Errors.Count > 0)
        {
            output.WriteLine($"LỖI ({loaded.Errors.Count}) — không ghi {outputPath}:");
            foreach (var error in loaded.Errors) output.WriteLine($"  - {error}");
            return 1;
        }

        var groups = loaded.Groups
            .OrderBy(g => g.Order)
            .Select(g => g with { Connectors = g.Connectors.OrderBy(c => c.Level).ToList() })
            .ToList();
        var book = FuriganaApplier.Apply(new ConnectorBook(groups, (clock ?? TimeProvider.System).GetUtcNow()), generator);
        OutputFile.Write(outputPath, ConnectorJson.Serialize(book));

        var connectors = book.Groups.SelectMany(g => g.Connectors).ToList();
        var byLevel = string.Join(", ", JlptLevels.All.Select(l => $"{l}: {connectors.Count(c => c.Level == l)}"));
        output.WriteLine($"Đã ghi {outputPath}: {book.Groups.Count} nhóm, {connectors.Count} từ nối ({byLevel}).");
        return 0;
    }
}
