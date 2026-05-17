namespace FolderSyncModule.Library;

/// <summary>
/// ファイルシステムの変更をリアルタイムで監視するクラス。
/// FileSystemWatcher を使用してファイルの作成・更新・削除を検出し、
/// コールバック経由で呼び出し元に通知します。
/// </summary>
public class RealtimeSyncWatcher : IRealtimeSyncWatcher
{
    private FileSystemWatcher? _watcher;
    private bool _syncInProgress = false;
    private readonly Action<string, string, string, string> _onFileChanged;

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
    }

    /// <summary>
    /// 監視を停止し、リソースをクリーンアップします。
    /// </summary>
    public void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
    }

    /// <summary>
    /// ファイル変更イベントの処理。
    /// 複数のイベントが短時間に発火するのを防ぐため、
    /// _syncInProgress フラグで同期中の重複呼び出しをブロックしています。
    /// </summary>
    private void OnChanged(string fullPath, string basePath, string action)
    {
        // 同期中の場合は重複呼び出しをスキップ
        if (_syncInProgress)
            return;

        _syncInProgress = true;
        try
        {
            // ファイル操作の完了を待つ
            System.Threading.Thread.Sleep(500);
            string relativePath = Path.GetRelativePath(basePath, fullPath);
            _onFileChanged(fullPath, basePath, action, relativePath);
        }
        finally
        {
            _syncInProgress = false;
        }
    }
}


