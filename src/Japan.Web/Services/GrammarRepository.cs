using System.Text.Json;
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

    private readonly ILogger<GrammarRepository> _logger;
    private readonly FileSystemWatcher? _watcher;
    private readonly Timer _debounce;
    private volatile Snapshot? _snapshot;

    public GrammarRepository(IOptions<GrammarDataOptions> options, IWebHostEnvironment env, ILogger<GrammarRepository> logger)
    {
        _logger = logger;
        DataPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.Path));
        _debounce = new Timer(_ => Reload(), null, Timeout.Infinite, Timeout.Infinite);
        Reload();

        var directory = Path.GetDirectoryName(DataPath)!;
        if (!Directory.Exists(directory)) return;

        _watcher = new FileSystemWatcher(directory, Path.GetFileName(DataPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += (_, _) => ScheduleReload();
        _watcher.Created += (_, _) => ScheduleReload();
        _watcher.Renamed += (_, _) => ScheduleReload();
        _watcher.EnableRaisingEvents = true;
    }

    public string DataPath { get; }

    public bool IsLoaded => _snapshot is not null;

    public IReadOnlyList<Lesson> Lessons => _snapshot?.Book.Lessons ?? [];

    public Lesson? FindLesson(int number) => Lessons.FirstOrDefault(l => l.Number == number);

    public GrammarPoint? Find(string id) =>
        _snapshot is { } s && s.IndexById.TryGetValue(id, out var i) ? s.Ordered[i] : null;

    public (GrammarPoint? Previous, GrammarPoint? Next) Neighbors(string id)
    {
        if (_snapshot is not { } s || !s.IndexById.TryGetValue(id, out var i)) return (null, null);
        return (i > 0 ? s.Ordered[i - 1] : null, i < s.Ordered.Count - 1 ? s.Ordered[i + 1] : null);
    }

    public IReadOnlyList<SearchHit> Search(string? query) => _snapshot?.Search.Search(query) ?? [];

    public void Reload()
    {
        try
        {
            if (!File.Exists(DataPath))
            {
                _logger.LogWarning("Không tìm thấy {Path}", DataPath);
                return;
            }

            var book = GrammarJson.Deserialize(File.ReadAllText(DataPath));
            var ordered = book.Lessons.SelectMany(l => l.Points).ToList();
            var index = ordered.Select((p, i) => (p.Id, i)).ToDictionary(x => x.Id, x => x.i, StringComparer.Ordinal);
            _snapshot = new Snapshot(book, ordered, index, new GrammarSearch(book));
            _logger.LogInformation("Đã nạp {Count} điểm ngữ pháp từ {Path}", ordered.Count, DataPath);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
        {
            _logger.LogError(ex, "Không đọc được {Path}, giữ dữ liệu cũ", DataPath);
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounce.Dispose();
    }

    private void ScheduleReload() => _debounce.Change(300, Timeout.Infinite);
}
