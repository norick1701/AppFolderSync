namespace FolderSyncModule.Library;

/// <summary>
/// フォルダ同期の全体調整を行うコーディネータクラス。
/// 複数の責務を持つクラスを組み合わせて、同期処理全体を統合します。
/// ファサードパターンを採用し、外部には単純なAPIを提供します。
/// </summary>
public class SyncCoordinator : ISyncCoordinator
{
    private readonly IFileSystemOperations _fileOps;
    private readonly IDiffDetector _diffDetector;
    private RealtimeSyncWatcher? _sourceWatcher;
    private RealtimeSyncWatcher? _targetWatcher;

    /// <summary>
    /// デフォルトコンストラクタ。
    /// 標準的なファイルシステム操作と差分検出を使用します。
    /// </summary>
    public SyncCoordinator()
        : this(new FileSystemOperations(), new DiffDetector())
    {
    }

    /// <summary>
    /// DI対応コンストラクタ。
    /// 依存するコンポーネントを外部から注入可能にします。
    /// テスト時にモックを渡すことで動作を検証できます。
    /// </summary>
    public SyncCoordinator(IFileSystemOperations fileOps, IDiffDetector diffDetector)
    {
        _fileOps = fileOps;
        _diffDetector = diffDetector;
    }

    /// <summary>
    /// フォルダの同期を実行します。
    /// 指定された設定に従い、ワンタイム同期またはリアルタイム監視を行います。
    /// </summary>
    public void Sync(string sourcePath, string targetPath, SyncMode mode, SyncType syncType, SyncScope scope)
    {
        ValidatePaths(sourcePath, targetPath);

        Console.WriteLine($"同期モード: {(mode == SyncMode.OneWay ? "単向同期" : "双方向同期")}");
        Console.WriteLine($"同期タイプ: {(syncType == SyncType.OneTime ? "ワンタイム同期" : "リアルタイム監視")}");
        Console.WriteLine($"同期範囲: {GetScopeDescription(scope)}\n");

        // ワンタイム同期を実行
        PerformSync(sourcePath, targetPath, mode, scope);

        // リアルタイム監視が指定されている場合は開始
        if (syncType == SyncType.Realtime)
        {
            StartRealtimeSync(sourcePath, targetPath, mode, scope);
        }
    }

    /// <summary>
    /// リアルタイム監視を停止します。
    /// </summary>
    public void StopRealtimeSync()
    {
        _sourceWatcher?.StopWatching();
        _targetWatcher?.StopWatching();
        Console.WriteLine("\n✓ リアルタイム監視を停止しました");
    }

    /// <summary>
    /// 実際の同期処理を実行します。
    /// 同期スコープに応じた戦略を選択し、実行します。
    /// </summary>
    private void PerformSync(string sourcePath, string targetPath, SyncMode mode, SyncScope scope)
    {
        var strategy = CreateStrategy(scope);

        // ソース → ターゲットの同期
        SyncDirectoriesRecursive(sourcePath, targetPath, mode, scope);
        strategy.SyncFiles(sourcePath, targetPath);
        strategy.DeleteOrphanedFiles(sourcePath, targetPath);

        // 双方向同期の場合、ターゲット → ソースの同期も行う
        if (mode == SyncMode.TwoWay)
        {
            Console.WriteLine("\nターゲット → ソースの同期:");
            strategy.SyncFiles(targetPath, sourcePath);
            strategy.DeleteOrphanedFiles(targetPath, sourcePath);
        }

        Console.WriteLine("\n✓ 同期完了しました");
    }

    /// <summary>
    /// ディレクトリを再帰的に同期します。
    /// サブディレクトリが存在しない場合は作成します。
    /// </summary>
    private void SyncDirectoriesRecursive(string sourcePath, string targetPath, SyncMode mode, SyncScope scope)
    {
        var sourceDirs = _fileOps.GetDirectories(sourcePath);

        foreach (var sourceDir in sourceDirs)
        {
            string dirName = Path.GetFileName(sourceDir);
            string targetDir = Path.Combine(targetPath, dirName);

            if (!_fileOps.DirectoryExists(targetDir))
            {
                _fileOps.CreateDirectory(targetDir);
                Console.WriteLine($"✓ フォルダ作成: {dirName}");
            }

            // 再帰的に同期(ワンタイムに限定してスタックオーバーフローを防ぐ)
            Sync(sourceDir, targetDir, mode, SyncType.OneTime, scope);
        }
    }

    /// <summary>
    /// リアルタイム監視を開始します。
    /// ファイル変更を検出するたびに同期が実行されます。
    /// </summary>
    private void StartRealtimeSync(string sourcePath, string targetPath, SyncMode mode, SyncScope scope)
    {
        Console.WriteLine("\n【リアルタイム監視開始】");
        Console.WriteLine("ファイル変更を監視中... (終了するには Ctrl+C を押してください)\n");

        // ソースの変更を監視
        _sourceWatcher = new RealtimeSyncWatcher((path, basePath, action, relativePath) =>
            OnSourceChanged(path, sourcePath, targetPath, action, relativePath));
        _sourceWatcher.StartWatching(sourcePath);

        // 双方向同期の場合、ターゲットの変更も監視
        if (mode == SyncMode.TwoWay)
        {
            _targetWatcher = new RealtimeSyncWatcher((path, basePath, action, relativePath) =>
                OnTargetChanged(path, targetPath, sourcePath, action, relativePath));
            _targetWatcher.StartWatching(targetPath);
        }
    }

    /// <summary>
    /// ソースでのファイル変更を処理します。
    /// </summary>
    private void OnSourceChanged(string path, string sourcePath, string targetPath, string action, string relativePath)
    {
        string targetPath2 = Path.Combine(targetPath, relativePath);
        Console.WriteLine($"[ソース {action}] {relativePath}");

        if (action == "削除")
        {
            _fileOps.DeleteFile(targetPath2);
            _fileOps.DeleteDirectory(targetPath2);
            Console.WriteLine($"  ✓ ターゲットから削除");
        }
        else if (_fileOps.FileExists(path))
        {
            _fileOps.CopyFile(path, targetPath2);
            Console.WriteLine($"  ✓ ターゲットに反映");
        }
        else if (Directory.Exists(path))
        {
            _fileOps.CreateDirectory(targetPath2);
            Console.WriteLine($"  ✓ ターゲットフォルダ作成");
        }
    }

    /// <summary>
    /// ターゲットでのファイル変更を処理します。
    /// 双方向同期時のみ呼び出されます。
    /// </summary>
    private void OnTargetChanged(string path, string targetPath, string sourcePath, string action, string relativePath)
    {
        string sourcePath2 = Path.Combine(sourcePath, relativePath);
        Console.WriteLine($"[ターゲット {action}] {relativePath}");

        if (action == "削除")
        {
            _fileOps.DeleteFile(sourcePath2);
            Console.WriteLine($"  ✓ ソースから削除");
        }
        else if (_fileOps.FileExists(path))
        {
            _fileOps.CopyFile(path, sourcePath2);
            Console.WriteLine($"  ✓ ソースに反映");
        }
    }

    /// <summary>
    /// 同期スコープに応じて適切な戦略を生成します。
    /// ストラテジーパターンを使用して、異なる同期ロジックを切り替えます。
    /// </summary>
    private ISyncStrategy CreateStrategy(SyncScope scope) => scope switch
    {
        SyncScope.FileOnly => new FileOnlySyncStrategy(),
        SyncScope.WithDeletion => new WithDeletionSyncStrategy(),
        SyncScope.DiffOnly => new DiffOnlySyncStrategy(),
        _ => new DiffOnlySyncStrategy()
    };

    /// <summary>
    /// 同期スコープを説明するテキストを返します。
    /// UI表示用です。
    /// </summary>
    private string GetScopeDescription(SyncScope scope) => scope switch
    {
        SyncScope.FileOnly => "ファイルコピーのみ",
        SyncScope.WithDeletion => "ファイルコピー＋削除も同期",
        SyncScope.DiffOnly => "差分のみ同期",
        _ => "不明"
    };

    /// <summary>
    /// パスの妥当性を検証します。
    /// ソースが存在しない場合は例外を投げます。
    /// ターゲットが存在しない場合は作成します。
    /// </summary>
    private void ValidatePaths(string sourcePath, string targetPath)
    {
        if (!_fileOps.DirectoryExists(sourcePath))
            throw new DirectoryNotFoundException($"ソースフォルダが見つかりません: {sourcePath}");

        if (!_fileOps.DirectoryExists(targetPath))
        {
            _fileOps.CreateDirectory(targetPath);
            Console.WriteLine($"✓ ターゲットフォルダを作成しました: {targetPath}");
        }
    }
}
