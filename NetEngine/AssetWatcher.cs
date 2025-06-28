namespace NetEngine;

public static class AssetWatcher
{
    private static string? _folderPath;
    private static FileSystemWatcher? _watcher;
    private static System.Timers.Timer? _debounceTimer;
    private static System.Timers.Timer? _retryTimer;

    private const int DebounceTimeout = 500;
    private const int RetryInterval = 1000;

    public static event EventHandler? ScriptsChanged;

    public static void Initialize(string folderPath)
    {
        Console.Log($"AssetWatcher: Инициализация с путём '{folderPath}'");
        _folderPath = folderPath;
        InitializeWatcherWithRetry();
    }

    private static void InitializeWatcherWithRetry()
    {
        Console.Log("AssetWatcher: Попытка инициализации слежения с повтором...");
        DisposeWatcher();

        if (string.IsNullOrEmpty(_folderPath))
        {
            Console.Log("AssetWatcher: Путь пустой, запускаю таймер повторных попыток...");
            StartRetryTimer();
            return;
        }

        if (!Directory.Exists(_folderPath))
        {
            Console.Log($"AssetWatcher: Папка '{_folderPath}' не найдена, запускаю таймер повторных попыток...");
            StartRetryTimer();
            return;
        }

        Console.Log($"AssetWatcher: Настраиваю FileSystemWatcher на '{_folderPath}'");

        _watcher = new FileSystemWatcher(_folderPath, "*.cs")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
                 | NotifyFilters.DirectoryName
                 | NotifyFilters.Attributes
                 | NotifyFilters.Size
                 | NotifyFilters.LastWrite
                 | NotifyFilters.LastAccess
                 | NotifyFilters.CreationTime
                 | NotifyFilters.Security
        };

        _watcher.Changed += OnAnyScriptChanged;
        _watcher.Created += OnAnyScriptChanged;
        _watcher.Deleted += OnAnyScriptChanged;
        _watcher.Renamed += OnAnyScriptChanged;

        _watcher.EnableRaisingEvents = true;

        _debounceTimer = new System.Timers.Timer(DebounceTimeout);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += (s, e) =>
        {
            Console.Log("AssetWatcher: Обнаружено изменение скриптов, вызываю событие.");
            ScriptsChanged?.Invoke(null, EventArgs.Empty);
        };

        StopRetryTimer();
        Console.Log("AssetWatcher: Слежение запущено, таймер повторных попыток остановлен.");
    }

    private static void OnAnyScriptChanged(object sender, FileSystemEventArgs e)
    {
        Console.Log($"AssetWatcher: Изменён файл: {e.ChangeType} — {e.FullPath}");
        _debounceTimer?.Stop();
        _debounceTimer?.Start();
    }

    private static void StartRetryTimer()
    {
        if (_retryTimer == null)
        {
            Console.Log("AssetWatcher: Создаю таймер повторных попыток...");
            _retryTimer = new System.Timers.Timer(RetryInterval);
            _retryTimer.AutoReset = true;
            _retryTimer.Elapsed += (s, e) =>
            {
                Console.Log("AssetWatcher: Таймер повторных попыток сработал, повторная инициализация...");
                InitializeWatcherWithRetry();
            };
        }

        if (!_retryTimer.Enabled)
        {
            Console.Log("AssetWatcher: Запускаю таймер повторных попыток...");
            _retryTimer.Start();
        }
    }

    private static void StopRetryTimer()
    {
        if (_retryTimer != null)
        {
            Console.Log("AssetWatcher: Останавливаю и удаляю таймер повторных попыток...");
            _retryTimer.Stop();
            _retryTimer.Dispose();
            _retryTimer = null;
        }
    }

    private static void DisposeWatcher()
    {
        if (_watcher != null)
        {
            Console.Log("AssetWatcher: Останавливаю и удаляю watcher...");
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnAnyScriptChanged;
            _watcher.Created -= OnAnyScriptChanged;
            _watcher.Deleted -= OnAnyScriptChanged;
            _watcher.Renamed -= OnAnyScriptChanged;
            _watcher.Dispose();
            _watcher = null;
        }

        if (_debounceTimer != null)
        {
            Console.Log("AssetWatcher: Останавливаю и удаляю таймер дебаунса...");
            _debounceTimer.Stop();
            _debounceTimer.Dispose();
            _debounceTimer = null;
        }

        StopRetryTimer();
    }

    public static void Dispose()
    {
        Console.Log("AssetWatcher: Dispose вызван.");
        DisposeWatcher();
    }
}
