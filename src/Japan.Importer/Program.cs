using System.Text;
using System.Text.Json;
using Japan.Core;
using Japan.Importer;
using Japan.Importer.Furigana;

Console.OutputEncoding = Encoding.UTF8;

try
{
    var repoRoot = RepoRoot.Find(Directory.GetCurrentDirectory());
    var contentDirectory = Path.Combine(repoRoot, "content");
    var overrides = FuriganaOverrides.Load(Path.Combine(contentDirectory, "furigana-overrides.json"));
    using var generator = new NMeCabFuriganaGenerator(overrides);

    if (args is ["connectors", ..])
    {
        return ConnectorPipeline.Run(
            Path.Combine(contentDirectory, "connectors"),
            Path.Combine(repoRoot, "data", "connectors.json"),
            Console.Out,
            generator);
    }

    var config = JsonSerializer.Deserialize<ImporterConfig>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json")), GrammarJson.Options)
        ?? throw new InvalidDataException("appsettings.json rỗng.");
    var settings = new ImportSettings(
        SourceDirectory: args.Length > 0 ? args[0] : config.SourceDirectory,
        ContentDirectory: contentDirectory,
        OutputPath: Path.Combine(repoRoot, "data", "grammar.json"));

    return ImportPipeline.Run(settings, Console.Out, generator);
}
catch (Exception ex) when (ex is InvalidDataException or DirectoryNotFoundException)
{
    Console.Error.WriteLine($"LỖI: {ex.Message}");
    return 1;
}

internal sealed record ImporterConfig(string SourceDirectory);
