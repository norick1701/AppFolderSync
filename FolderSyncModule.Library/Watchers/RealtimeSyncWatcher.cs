using System.Collections.Concurrent;

namespace FolderSyncModule.Library;

/// <summary>
/// ファイルシステムの変更をリアルタイムで監視するクラス。
/// FileSystemWatcher を使用してファイルの作成・更新・削除を検出し、
/// コールバック経由で呼び出し元に通知します。
/// イベントキューにより、大量のイベントを安全に処理します。
/// </summary>
public class RealtimeSyncWatcher : IRealtimeSyncWatcher, IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly Action<string, string, string, string> _onFileChanged;
    private readonly BlockingCollection<FileChangeEvent> _eventQueue = new(new ConcurrentQueue<FileChangeEvent>());
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _processingTask;
    private bool _disposed = false;

    /// <summary>
    /// ファイル変更イベントを表す構造体。
    /// </summary>
    private record FileChangeEvent(string FullPath, string BasePath, string Action);

    /// <summary>
    /// コンストラクタ。
    /// ファイル変更時に呼び出されるコールバックを受け取ります。
    /// </summary>
    /// <param name="onFileChanged">
    /// (fullPath, basePath, action, relativePath) のシグネチャを持つコールバック
    /// </param>
    public RealtimeSyncWatcher(Action<string, string, string, string> onFileChanged)
    {
        _onFileChanged = onFileChanged;
    }

    /// <summary>
    /// 指定されたフォルダの監視を開始します。
    /// 既に監視中の場合は何もしません。
    /// イベント処理用のバックグラウンドタスクを起動します。
    /// </summary>
    public void StartWatching(string folderPath)
    {
        if (_watcher != null)
            return;

        _watcher = new FileSystemWatcher(folderPath);
        _watcher.IncludeSubdirectories = true;
        _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;

        // ファイルの作成・変更・削除・名前変更をハンドル
        _watcher.Created += (s, e) => OnChanged(e.FullPath, folderPath, "作成");
        _watcher.Changed += (s, e) => OnChanged(e.FullPath, folderPath, "更新");
        _watcher.Deleted += (s, e) => OnChanged(e.FullPath, folderPath, "削除");
        _watcher.Renamed += (s, e) => OnChanged(e.FullPath, folderPath, "名前変更");

        _watcher.EnableRaisingEvents = true;

        // イベント処理タスクを開始
        _processingTask = Task.Run(() => ProcessEventQueue(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// 監視を停止し、リソースをクリーンアップします。
    /// </summary>
    public void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;

        // イベント処理を停止
        _eventQueue.CompleteAdding();
        _processingTask?.Wait(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// ファイル変更イベントの処理。
    /// イベントをキューに追加します。
    /// </summary>
    private void OnChanged(string fullPath, string basePath, string action)
    {
        try
        {
            // イベントをキューに追加（キューが満杯になることを防ぐ）
            if (!_eventQueue.IsAddingCompleted)
            {
                _eventQueue.TryAdd(new FileChangeEvent(fullPath, basePath, action), TimeSpan.FromMilliseconds(100));
            }
        }
        catch (InvalidOperationException)
        {
            // キューが既に完了している場合は無視
        }
    }

    /// <summary>
    /// イベントキューを処理するバックグラウンドタスク。
    /// 重複イベントを排除し、順次処理します。
    /// </summary>
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

                // 重複イベントを排除（デバウンス処理）
                var key = $"{evt.FullPath}:{evt.Action}";
                var now = DateTime.UtcNow;

                if (lastProcessedEvents.TryGetValue(key, out var lastTime))
                {
                    if (now - lastTime < debounceTime)
                    {
                        // 短時間内の重複イベントをスキップ
                        continue;
                    }
                }

                lastProcessedEvents[key] = now;

                // ファイル操作の完了を待つ
                Thread.Sleep(100);

                string relativePath = Path.GetRelativePath(evt.BasePath, evt.FullPath);
                _onFileChanged(evt.FullPath, evt.BasePath, evt.Action, relativePath);

                // 古いエントリをクリーンアップ（メモリリーク防止）
                var oldKeys = lastProcessedEvents
                    .Where(kv => now - kv.Value > TimeSpan.FromMinutes(5))
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var oldKey in oldKeys)
                {
                    lastProcessedEvents.Remove(oldKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"イベント処理エラー: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// リソースを解放します。
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

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


