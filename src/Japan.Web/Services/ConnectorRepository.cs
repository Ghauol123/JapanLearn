using Japan.Core;
using Microsoft.Extensions.Options;

namespace Japan.Web.Services;

public sealed record ConnectorLocation(Connector Connector, ConnectorGroup Group, Connector? Previous, Connector? Next);

public sealed class ConnectorRepository : IDisposable
{
    private sealed record Snapshot(
        ConnectorBook Book,
        IReadOnlyList<(Connector Connector, ConnectorGroup Group)> Ordered,
        IReadOnlyDictionary<string, int> IndexById,
        ConnectorSearch Search);

    private readonly ReloadingJsonFile<Snapshot> _file;

    public ConnectorRepository(IOptions<ConnectorDataOptions> options, IWebHostEnvironment env, ILogger<ConnectorRepository> logger) =>
        _file = new ReloadingJsonFile<Snapshot>(
            Path.Combine(env.ContentRootPath, options.Value.Path), Parse, s => $"{s.Ordered.Count} từ nối", logger);

    public string DataPath => _file.Path;

    public string? LoadError => _file.LoadError;

    public bool IsLoaded => _file.IsLoaded;

    public IReadOnlyList<ConnectorGroup> Groups => _file.Value?.Book.Groups ?? [];

    public int Count => _file.Value?.Ordered.Count ?? 0;

    public IReadOnlyList<ConnectorGroup> Filter(JlptLevel? level) => level is not { } l
        ? Groups
        : Groups.Select(g => g with { Connectors = g.Connectors.Where(c => c.Level == l).ToList() })
                .Where(g => g.Connectors.Count > 0)
                .ToList();

    public ConnectorLocation? Locate(string id)
    {
        if (_file.Value is not { } s || !s.IndexById.TryGetValue(id, out var i)) return null;
        var (connector, group) = s.Ordered[i];
        return new ConnectorLocation(connector, group,
            i > 0 ? s.Ordered[i - 1].Connector : null,
            i < s.Ordered.Count - 1 ? s.Ordered[i + 1].Connector : null);
    }

    public IReadOnlyList<ConnectorHit> Search(string? query) => _file.Value?.Search.Search(query) ?? [];

    public bool Reload() => _file.Reload();

    public void Dispose() => _file.Dispose();

    private static Snapshot Parse(string json)
    {
        var book = ConnectorJson.Deserialize(json);
        var errors = ConnectorValidator.Validate(book.Groups);
        if (errors.Count > 0) throw new InvalidDataException(string.Join("; ", errors));

        var ordered = book.Groups.SelectMany(g => g.Connectors.Select(c => (c, g))).ToList();
        var index = ordered.Select((x, i) => (x.c.Id, i)).ToDictionary(x => x.Id, x => x.i, StringComparer.Ordinal);
        return new Snapshot(book, ordered, index, new ConnectorSearch(book));
    }
}
