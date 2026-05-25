using System.Diagnostics;
using FolderSyncModule.Library.Models;
using FolderSyncModule.Library.Utils;
using FolderSyncModule.Library.Logging;

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
    private readonly ILogger _logger;
    private RealtimeSyncWatcher? _sourceWatcher;
    private RealtimeSyncWatcher? _targetWatcher;
    private bool _disposed;

    /// <summary>
    /// デフォルトコンストラクタ。
    /// 標準的なファイルシステム操作と差分検出を使用します。
    /// </summary>
    public SyncCoordinator()
        : this(new FileSystemOperations(), new DiffDetector(), new ConsoleLogger())
    {
    }

    /// <summary>
    /// DI対応コンストラクタ。
    /// 依存するコンポーネントを外部から注入可能にします。
    /// テスト時にモックを渡すことで動作を検証できます。
    /// </summary>
    public SyncCoordinator(
        IFileSystemOperations fileOps, 
        IDiffDetector diffDetector, 
        ILogger? logger = null)
    {
        _fileOps = fileOps;
        _diffDetector = diffDetector;
        _logger = logger ?? new ConsoleLogger();
    }

    /// <summary>
    /// フォルダの同期を実行します。
    /// 指定された設定に従い、ワンタイム同期またはリアルタイム監視を行います。
    /// </summary>
    public SyncResult Sync(
        string sourcePath, 
        string targetPath, 
        SyncMode mode, 
        SyncType syncType, 
        SyncScope scope)
    {
        var options = new SyncOptions(sourcePath, targetPath)
        {
            Mode = mode,
            SyncType = syncType,
            Scope = scope
        };
        return Sync(options);
    }

    /// <summary>
    /// フォルダの同期を実行します（設定オブジェクト使用）。
    /// SyncOptions を使って引数を簡潔にします。
    /// </summary>
    public SyncResult Sync(SyncOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            ValidatePaths(options.SourcePath, options.TargetPath);

            _logger.Info($"同期モード: {(options.Mode == SyncMode.OneWay ? "単向同期" : "双方向同期")}");
            _logger.Info($"同期タイプ: {(options.SyncType == SyncType.OneTime ? "ワンタイム同期" : "リアルタイム監視")}");
            _logger.Info($"同期範囲: {GetScopeDescription(options.Scope)}");

            // ワンタイム同期を実行
            var result = PerformSync(options.SourcePath, options.TargetPath, options.Mode, options.Scope);

            // リアルタイム監視が指定されている場合は開始
            if (options.SyncType == SyncType.Realtime)
            {
                StartRealtimeSync(options.SourcePath, options.TargetPath, options.Mode, options.Scope);
            }

            stopwatch.Stop();
            
            // 実行時間を更新して返す
            if (result.IsSuccess)
            {
                return SyncResult.Success(
                    result.FilesSucceeded,
                    result.FilesDeleted,
                    result.TotalBytesCopied,
                    stopwatch.Elapsed);
            }
            else
            {
                return SyncResult.PartialSuccess(
                    result.FilesSucceeded,
                    result.FilesFailed,
                    result.FilesDeleted,
                    result.TotalBytesCopied,
                    stopwatch.Elapsed,
                    result.Errors);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error($"同期失敗: {ex.Message}", ex);
            return SyncResult.Failure($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// リアルタイム監視を停止します。
    /// </summary>
    public void StopRealtimeSync()
    {
        _sourceWatcher?.StopWatching();
        _targetWatcher?.StopWatching();
        _logger.Info("リアルタイム監視を停止しました");
    }

    /// <summary>
    /// 実際の同期処理を実行します。
    /// 同期スコープに応じた戦略を選択し、実行します。
    /// </summary>
    private SyncResult PerformSync(
        string sourcePath, 
        string targetPath, 
        SyncMode mode, 
        SyncScope scope)
    {
        var errors = new List<SyncError>();
        int filesSucceeded = 0;
        int filesFailed = 0;
        int filesDeleted = 0;
        long totalBytesCopied = 0;

        var strategy = CreateStrategy(scope);

        // ソース → ターゲットの同期
        var (dirSuccess, dirErrors) = SyncDirectoriesRecursive(sourcePath, targetPath, mode, scope);
        errors.AddRange(dirErrors);

        var (fileSuccess, fileFailed, bytesCopied, fileErrors) = 
            strategy.SyncFilesWithResult(sourcePath, targetPath, _fileOps);
        filesSucceeded += fileSuccess;
        filesFailed += fileFailed;
        totalBytesCopied += bytesCopied;
        errors.AddRange(fileErrors);

        var (deleteSuccess, deleteErrors) = 
            strategy.DeleteOrphanedFilesWithResult(sourcePath, targetPath, _fileOps, _diffDetector);
        filesDeleted += deleteSuccess;
        errors.AddRange(deleteErrors);

        // 双方向同期の場合、ターゲット → ソースの同期も行う
        if (mode == SyncMode.TwoWay)
        {
            _logger.Info("ターゲット → ソースの同期:");
            
            var (fileSuccess2, fileFailed2, bytesCopied2, fileErrors2) = 
                strategy.SyncFilesWithResult(targetPath, sourcePath, _fileOps);
            filesSucceeded += fileSuccess2;
            filesFailed += fileFailed2;
            totalBytesCopied += bytesCopied2;
            errors.AddRange(fileErrors2);

            var (deleteSuccess2, deleteErrors2) = 
                strategy.DeleteOrphanedFilesWithResult(targetPath, sourcePath, _fileOps, _diffDetector);
            filesDeleted += deleteSuccess2;
            errors.AddRange(deleteErrors2);
        }

        if (errors.Count == 0)
        {
            _logger.Info("同期完了しました");
            return SyncResult.Success(filesSucceeded, filesDeleted, totalBytesCopied, TimeSpan.Zero);
        }
        else
        {
            _logger.Warning($"同期完了（{filesFailed}個のエラーあり）");
            return SyncResult.PartialSuccess(filesSucceeded, filesFailed, filesDeleted, totalBytesCopied, TimeSpan.Zero, errors);
        }
    }

    /// <summary>
    /// ディレクトリを再帰的に同期します。
    /// サブディレクトリが存在しない場合は作成します。
    /// </summary>
    private (int success, List<SyncError> errors) SyncDirectoriesRecursive(
        string sourcePath, 
        string targetPath, 
        SyncMode mode, 
        SyncScope scope)
    {
        var errors = new List<SyncError>();
        int success = 0;

        var dirsResult = _fileOps.TryGetDirectories(sourcePath, excludeSymbolicLinks: true);
        
        if (!dirsResult.IsSuccess)
        {
            errors.Add(new SyncError(sourcePath, dirsResult.ErrorMessage ?? "ディレクトリ取得失敗", dirsResult.ExceptionType));
            return (0, errors);
        }

        var sourceDirs = dirsResult.Value ?? Array.Empty<string>();

        foreach (var sourceDir in sourceDirs)
        {
            try
            {
                string dirName = Path.GetFileName(sourceDir);
                string targetDir = Path.Combine(targetPath, dirName);

                if (!_fileOps.DirectoryExists(targetDir))
                {
                    _fileOps.CreateDirectory(targetDir);
                    _logger.Debug($"フォルダ作成: {dirName}");
                }

                // 再帰的に同期(ワンタイムに限定してスタックオーバーフローを防ぐ)
                var result = Sync(sourceDir, targetDir, mode, SyncType.OneTime, scope);
                
                if (!result.IsSuccess && result.IsPartialSuccess)
                {
                    errors.AddRange(result.Errors);
                }
                
                success++;
            }
            catch (Exception ex)
            {
                errors.Add(new SyncError(sourceDir, $"ディレクトリ同期エラー: {ex.Message}", ex.GetType().Name));
            }
        }

        return (success, errors);
    }

    /// <summary>
    /// リアルタイム監視を開始します。
    /// ファイル変更を検出するたびに同期が実行されます。
    /// </summary>
    private void StartRealtimeSync(
        string sourcePath, 
        string targetPath, 
        SyncMode mode, 
        SyncScope scope)
    {
        _logger.Info("【リアルタイム監視開始】");
        _logger.Info("ファイル変更を監視中... (終了するには Ctrl+C を押してください)");

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
    private void OnSourceChanged(
        string path, 
        string sourcePath, 
        string targetPath, 
        string action, string relativePath)
    {
        try
        {
            // パストラバーサル検証
            string targetPath2 = PathValidator.SafeCombine(targetPath, relativePath);
            _logger.Debug($"[ソース {action}] {relativePath}");

            if (action == "削除")
            {
                var deleteFileResult = _fileOps.TryDeleteFile(targetPath2);
                var deleteDirResult = _fileOps.TryDeleteDirectory(targetPath2);
                
                if (deleteFileResult.IsSuccess || deleteDirResult.IsSuccess)
                {
                    _logger.Debug($"  ターゲットから削除");
                }
            }
            else if (_fileOps.FileExists(path))
            {
                var copyResult = _fileOps.TryCopyFile(path, targetPath2);
                if (copyResult.IsSuccess)
                {
                    _logger.Debug($"  ターゲットに反映");
                }
                else
                {
                    _logger.Error($"  エラー: {copyResult.ErrorMessage}");
                }
            }
            else if (Directory.Exists(path))
            {
                _fileOps.CreateDirectory(targetPath2);
                _logger.Debug($"  ターゲットフォルダ作成");
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.Error($"  セキュリティエラー: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error($"  エラー: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// ターゲットでのファイル変更を処理します。
    /// 双方向同期時のみ呼び出されます。
    /// </summary>
    private void OnTargetChanged(
        string path, 
        string targetPath, 
        string sourcePath, 
        string action, 
        string relativePath)
    {
        try
        {
            // パストラバーサル検証
            string sourcePath2 = PathValidator.SafeCombine(sourcePath, relativePath);
            _logger.Debug($"[ターゲット {action}] {relativePath}");

            if (action == "削除")
            {
                var deleteResult = _fileOps.TryDeleteFile(sourcePath2);
                if (deleteResult.IsSuccess)
                {
                    _logger.Debug($"  ソースから削除");
                }
            }
            else if (_fileOps.FileExists(path))
            {
                var copyResult = _fileOps.TryCopyFile(path, sourcePath2);
                if (copyResult.IsSuccess)
                {
                    _logger.Debug($"  ソースに反映");
                }
                else
                {
                    _logger.Error($"  エラー: {copyResult.ErrorMessage}");
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.Error($"  セキュリティエラー: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error($"  エラー: {ex.Message}", ex);
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
    /// ソースとターゲットが同一、または一方が他方のサブフォルダである場合は例外を投げます。
    /// </summary>
    private void ValidatePaths(string sourcePath, string targetPath)
    {
        if (!_fileOps.DirectoryExists(sourcePath))
            throw new DirectoryNotFoundException($"ソースフォルダが見つかりません: {sourcePath}");

        // パスを正規化して比較（大文字小文字・末尾スラッシュの違いを吸収）
        string normalizedSource = Path.GetFullPath(sourcePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedTarget = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"ソースとターゲットが同じフォルダを指しています: {normalizedSource}");

        string sourcePrefix = normalizedSource + Path.DirectorySeparatorChar;
        string targetPrefix = normalizedTarget + Path.DirectorySeparatorChar;

        if (normalizedTarget.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"ターゲットフォルダ '{normalizedTarget}' はソースフォルダ '{normalizedSource}' のサブフォルダです。循環参照が発生するため同期できません。");

        if (normalizedSource.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"ソースフォルダ '{normalizedSource}' はターゲットフォルダ '{normalizedTarget}' のサブフォルダです。循環参照が発生するため同期できません。");

        if (!_fileOps.DirectoryExists(targetPath))
        {
            _fileOps.CreateDirectory(targetPath);
            _logger.Info($"ターゲットフォルダを作成しました: {targetPath}");
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
            // マネージドリソースの解放
            _sourceWatcher?.StopWatching();
            _targetWatcher?.StopWatching();
            _sourceWatcher = null;
            _targetWatcher = null;
        }

        _disposed = true;
    }
}
