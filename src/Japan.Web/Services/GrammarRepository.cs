using Japan.Core;
using Microsoft.Extensions.Options;

namespace Japan.Web.Services;

public sealed class GrammarRepository : IDisposable
{
    private const int DebounceMs = 300;
    private const int RetryMs = 1000;
    private const int MaxRetries = 3;

    private sealed record Snapshot(
        GrammarBook Book,
        IReadOnlyList<GrammarPoint> Ordered,
        IReadOnlyDictionary<string, int> IndexById,
        GrammarSearch Search);

    private readonly ILogger<GrammarRepository> _logger;
    private readonly FileSystemWatcher? _watcher;
    private readonly Timer _debounce;
    private readonly Lock _reloadLock = new();
    private volatile Snapshot? _snapshot;
    private int _retries;

    public GrammarRepository(IOptions<GrammarDataOptions> options, IWebHostEnvironment env, ILogger<GrammarRepository> logger)
    {
        _logger = logger;
        DataPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.Path));
        _debounce = new Timer(_ => OnDebounce(), null, Timeout.Infinite, Timeout.Infinite);
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

    public string? LoadError { get; private set; }

    public bool IsLoaded
    {
        get
        {
            if (_snapshot is null) Reload();
            return _snapshot is not null;
        }
    }

    public IReadOnlyList<Lesson> Lessons => _snapshot?.Book.Lessons ?? [];

    public Lesson? FindLesson(int number) => Lessons.FirstOrDefault(l => l.Number == number);

    public GrammarPoint? Find(string id) => Locate(id).Point;

    public (GrammarPoint? Point, GrammarPoint? Previous, GrammarPoint? Next) Locate(string id)
    {
        if (_snapshot is not { } s || !s.IndexById.TryGetValue(id, out var i)) return (null, null, null);
        return (s.Ordered[i], i > 0 ? s.Ordered[i - 1] : null, i < s.Ordered.Count - 1 ? s.Ordered[i + 1] : null);
    }

    public IReadOnlyList<SearchHit> Search(string? query) => _snapshot?.Search.Search(query) ?? [];

    public bool Reload()
    {
        lock (_reloadLock)
        {
            try
            {
                if (!File.Exists(DataPath))
                {
                    LoadError = $"Không tìm thấy {DataPath}";
                    _logger.LogWarning("Không tìm thấy {Path}", DataPath);
                    return false;
                }

                var book = GrammarJson.Deserialize(File.ReadAllText(DataPath));
                var ordered = book.Lessons.SelectMany(l => l.Points).ToList();
                var index = ordered.Select((p, i) => (p.Id, i)).ToDictionary(x => x.Id, x => x.i, StringComparer.Ordinal);
                _snapshot = new Snapshot(book, ordered, index, new GrammarSearch(book));
                LoadError = null;
                _logger.LogInformation("Đã nạp {Count} điểm ngữ pháp từ {Path}", ordered.Count, DataPath);
                return true;
            }
            catch (Exception ex)
            {
                LoadError = $"Không đọc được {DataPath}: {ex.Message}";
                _logger.LogError(ex, "Không đọc được {Path}, giữ dữ liệu cũ", DataPath);
                return false;
            }
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounce.Dispose();
    }

    private void ScheduleReload()
    {
        Interlocked.Exchange(ref _retries, 0);
        _debounce.Change(DebounceMs, Timeout.Infinite);
    }

    private void OnDebounce()
    {
        if (Reload()) return;
        if (Interlocked.Increment(ref _retries) <= MaxRetries)
            _debounce.Change(RetryMs, Timeout.Infinite);
    }
}
