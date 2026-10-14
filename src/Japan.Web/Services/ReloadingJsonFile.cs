namespace Japan.Web.Services;

public sealed class ReloadingJsonFile<T> : IDisposable where T : class
{
    private const int DebounceMs = 300;
    private const int RetryMs = 1000;
    private const int MaxRetries = 3;

    private readonly Func<string, T> _parse;
    private readonly Func<T, string> _describe;
    private readonly ILogger _logger;
    private readonly FileSystemWatcher? _watcher;
    private readonly Timer _debounce;
    private readonly Lock _reloadLock = new();
    private volatile T? _value;
    private int _retries;

    public ReloadingJsonFile(string path, Func<string, T> parse, Func<T, string> describe, ILogger logger)
    {
        Path = System.IO.Path.GetFullPath(path);
        _parse = parse;
        _describe = describe;
        _logger = logger;
        _debounce = new Timer(_ => OnDebounce(), null, Timeout.Infinite, Timeout.Infinite);
        Reload();

        var directory = System.IO.Path.GetDirectoryName(Path)!;
        if (!Directory.Exists(directory)) return;

        _watcher = new FileSystemWatcher(directory, System.IO.Path.GetFileName(Path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += (_, _) => ScheduleReload();
        _watcher.Created += (_, _) => ScheduleReload();
        _watcher.Renamed += (_, _) => ScheduleReload();
        _watcher.EnableRaisingEvents = true;
    }

    public string Path { get; }

    public string? LoadError { get; private set; }

    public T? Value => _value;

    public bool IsLoaded
    {
        get
        {
            if (_value is null) Reload();
            return _value is not null;
        }
    }

    public bool Reload()
    {
        lock (_reloadLock)
        {
            try
            {
                if (!File.Exists(Path))
                {
                    LoadError = $"Không tìm thấy {Path}";
                    _logger.LogWarning("Không tìm thấy {Path}", Path);
                    return false;
                }

                var value = _parse(File.ReadAllText(Path));
                _value = value;
                LoadError = null;
                _logger.LogInformation("Đã nạp {Content} từ {Path}", _describe(value), Path);
                return true;
            }
            catch (Exception ex)
            {
                LoadError = $"Không đọc được {Path}: {ex.Message}";
                _logger.LogError(ex, "Không đọc được {Path}, giữ dữ liệu cũ", Path);
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
