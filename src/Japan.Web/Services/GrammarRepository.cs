using Japan.Core;
using Microsoft.Extensions.Options;

namespace Japan.Web.Services;

public sealed class GrammarRepository : IDisposable
{
    private sealed record Snapshot(
        GrammarBook Book,
        IReadOnlyList<GrammarPoint> Ordered,
        IReadOnlyDictionary<string, int> IndexById,
        GrammarSearch Search);

    private readonly ReloadingJsonFile<Snapshot> _file;

    public GrammarRepository(IOptions<GrammarDataOptions> options, IWebHostEnvironment env, ILogger<GrammarRepository> logger) =>
        _file = new ReloadingJsonFile<Snapshot>(
            Path.Combine(env.ContentRootPath, options.Value.Path), Parse, s => $"{s.Ordered.Count} điểm ngữ pháp", logger);

    public string DataPath => _file.Path;

    public string? LoadError => _file.LoadError;

    public bool IsLoaded => _file.IsLoaded;

    public IReadOnlyList<Lesson> Lessons => _file.Value?.Book.Lessons ?? [];

    public Lesson? FindLesson(int number) => Lessons.FirstOrDefault(l => l.Number == number);

    public GrammarPoint? Find(string id) => Locate(id).Point;

    public (GrammarPoint? Point, GrammarPoint? Previous, GrammarPoint? Next) Locate(string id)
    {
        if (_file.Value is not { } s || !s.IndexById.TryGetValue(id, out var i)) return (null, null, null);
        return (s.Ordered[i], i > 0 ? s.Ordered[i - 1] : null, i < s.Ordered.Count - 1 ? s.Ordered[i + 1] : null);
    }

    public IReadOnlyList<SearchHit> Search(string? query) => _file.Value?.Search.Search(query) ?? [];

    public bool Reload() => _file.Reload();

    public void Dispose() => _file.Dispose();

    private static Snapshot Parse(string json)
    {
        var book = GrammarJson.Deserialize(json);
        var ordered = book.Lessons.SelectMany(l => l.Points).ToList();
        var index = ordered.Select((p, i) => (p.Id, i)).ToDictionary(x => x.Id, x => x.i, StringComparer.Ordinal);
        return new Snapshot(book, ordered, index, new GrammarSearch(book));
    }
}
