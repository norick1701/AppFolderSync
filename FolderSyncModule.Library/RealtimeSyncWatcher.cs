using System.Collections.Concurrent;

namespace FolderSyncModule.Library;

/// <summary>
/// ファイルシステムの変更をリアルタイムで監視するインターフェース。
/// </summary>
public interface IRealtimeSyncWatcher : IDisposable
{
    void StartWatching(string folderPath);
    void StopWatching();
}

/// <summary>
/// ファイルシステムの変更をリアルタイムで監視するクラス。
/// </summary>
public class RealtimeSyncWatcher : IRealtimeSyncWatcher, IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly Action<string, string, string, string> _onFileChanged;
    private readonly BlockingCollection<FileChangeEvent> _eventQueue = new(new ConcurrentQueue<FileChangeEvent>());
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _processingTask;
    private bool _disposed = false;

    private record FileChangeEvent(string FullPath, string BasePath, string Action);

    public RealtimeSyncWatcher(Action<string, string, string, string> onFileChanged)
    {
        _onFileChanged = onFileChanged;
    }

    public void StartWatching(string folderPath)
    {
        if (_watcher != null)
            return;

        _watcher = new FileSystemWatcher(folderPath);
        _watcher.IncludeSubdirectories = true;
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;

        _watcher.Created += (s, e) => OnChanged(e.FullPath, folderPath, "作成");
        _watcher.Changed += (s, e) => OnChanged(e.FullPath, folderPath, "更新");
        _watcher.Deleted += (s, e) => OnChanged(e.FullPath, folderPath, "削除");
        _watcher.Renamed += (s, e) => OnChanged(e.FullPath, folderPath, "名前変更");

        _watcher.EnableRaisingEvents = true;
        _processingTask = Task.Run(() => ProcessEventQueue(_cancellationTokenSource.Token));
    }

    public void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
        _eventQueue.CompleteAdding();
        _processingTask?.Wait(TimeSpan.FromSeconds(5));
    }

    private void OnChanged(string fullPath, string basePath, string action)
    {
        try
        {
            if (!_eventQueue.IsAddingCompleted)
                _eventQueue.TryAdd(new FileChangeEvent(fullPath, basePath, action), TimeSpan.FromMilliseconds(100));
        }
        catch (InvalidOperationException) { }
    }

    private void ProcessEventQueue(CancellationToken cancellationToken)
    {
        var lastProcessedEvents = new Dictionary<string, DateTime>();
        var debounceTime = TimeSpan.FromMilliseconds(500);

        foreach (var evt in _eventQueue.GetConsumingEnumerable(cancellationToken))
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var key = $"{evt.FullPath}:{evt.Action}";
                var now = DateTime.UtcNow;

                if (lastProcessedEvents.TryGetValue(key, out var lastTime) && now - lastTime < debounceTime)
                    continue;

                lastProcessedEvents[key] = now;
                Thread.Sleep(100);

                string relativePath = Path.GetRelativePath(evt.BasePath, evt.FullPath);
                _onFileChanged(evt.FullPath, evt.BasePath, evt.Action, relativePath);

                var oldKeys = lastProcessedEvents
                    .Where(kv => now - kv.Value > TimeSpan.FromMinutes(5))
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var oldKey in oldKeys)
                    lastProcessedEvents.Remove(oldKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"イベント処理エラー: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            StopWatching();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _eventQueue.Dispose();
        }
        _disposed = true;
    }
}
