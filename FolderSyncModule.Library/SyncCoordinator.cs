using System.Diagnostics;
using FolderSyncModule.Library.Models;
using FolderSyncModule.Library.Logging;

namespace FolderSyncModule.Library;

/// <summary>
/// フォルダ同期の全体調整を行うコーディネータクラス。
/// </summary>
public class SyncCoordinator : IDisposable
{
    private readonly IFileSystemOperations _fileOps;
    private readonly IDiffDetector _diffDetector;
    private readonly ILogger _logger;
    private RealtimeSyncWatcher? _sourceWatcher;
    private RealtimeSyncWatcher? _targetWatcher;
    private bool _disposed;

    public SyncCoordinator()
        : this(new FileSystemOperations(), new DiffDetector(), new ConsoleLogger())
    {
    }

    public SyncCoordinator(
        IFileSystemOperations fileOps,
        IDiffDetector diffDetector,
        ILogger? logger = null)
    {
        _fileOps = fileOps;
        _diffDetector = diffDetector;
        _logger = logger ?? new ConsoleLogger();
    }

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

    public SyncResult Sync(SyncOptions options)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            ValidatePaths(options.SourcePath, options.TargetPath);

            _logger.Info($"同期モード: {(options.Mode == SyncMode.OneWay ? "単向同期" : "双方向同期")}");
            _logger.Info($"同期タイプ: {(options.SyncType == SyncType.OneTime ? "ワンタイム同期" : "リアルタイム監視")}");
            _logger.Info($"同期範囲: {GetScopeDescription(options.Scope)}");

            var result = PerformSync(options.SourcePath, options.TargetPath, options.Mode, options.Scope);

            if (options.SyncType == SyncType.Realtime)
                StartRealtimeSync(options.SourcePath, options.TargetPath, options.Mode, options.Scope);

            stopwatch.Stop();

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

    public void StopRealtimeSync()
    {
        _sourceWatcher?.StopWatching();
        _targetWatcher?.StopWatching();
        _logger.Info("リアルタイム監視を停止しました");
    }

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

        var (_, dirErrors) = SyncDirectoriesRecursive(sourcePath, targetPath, mode, scope);
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

                var result = PerformSync(sourceDir, targetDir, mode, scope);

                if (!result.IsSuccess && result.IsPartialSuccess)
                    errors.AddRange(result.Errors);

                success++;
            }
            catch (Exception ex)
            {
                errors.Add(new SyncError(sourceDir, $"ディレクトリ同期エラー: {ex.Message}", ex.GetType().Name));
            }
        }

        return (success, errors);
    }

    private void StartRealtimeSync(
        string sourcePath,
        string targetPath,
        SyncMode mode,
        SyncScope scope)
    {
        _logger.Info("【リアルタイム監視開始】");
        _logger.Info("ファイル変更を監視中... (終了するには Ctrl+C を押してください)");

        _sourceWatcher = new RealtimeSyncWatcher((path, basePath, action, relativePath) =>
            OnSourceChanged(path, sourcePath, targetPath, action, relativePath));
        _sourceWatcher.StartWatching(sourcePath);

        if (mode == SyncMode.TwoWay)
        {
            _targetWatcher = new RealtimeSyncWatcher((path, basePath, action, relativePath) =>
                OnTargetChanged(path, targetPath, sourcePath, action, relativePath));
            _targetWatcher.StartWatching(targetPath);
        }
    }

    private void OnSourceChanged(
        string path,
        string sourcePath,
        string targetPath,
        string action,
        string relativePath)
    {
        try
        {
            string targetPath2 = PathValidator.SafeCombine(targetPath, relativePath);
            _logger.Debug($"[ソース {action}] {relativePath}");

            if (action == "削除")
            {
                var deleteFileResult = _fileOps.TryDeleteFile(targetPath2);
                var deleteDirResult = _fileOps.TryDeleteDirectory(targetPath2);

                if (deleteFileResult.IsSuccess || deleteDirResult.IsSuccess)
                    _logger.Debug($"  ターゲットから削除");
            }
            else if (_fileOps.FileExists(path))
            {
                var copyResult = _fileOps.TryCopyFile(path, targetPath2);
                if (copyResult.IsSuccess)
                    _logger.Debug($"  ターゲットに反映");
                else
                    _logger.Error($"  エラー: {copyResult.ErrorMessage}");
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

    private void OnTargetChanged(
        string path,
        string targetPath,
        string sourcePath,
        string action,
        string relativePath)
    {
        try
        {
            string sourcePath2 = PathValidator.SafeCombine(sourcePath, relativePath);
            _logger.Debug($"[ターゲット {action}] {relativePath}");

            if (action == "削除")
            {
                var deleteResult = _fileOps.TryDeleteFile(sourcePath2);
                if (deleteResult.IsSuccess)
                    _logger.Debug($"  ソースから削除");
            }
            else if (_fileOps.FileExists(path))
            {
                var copyResult = _fileOps.TryCopyFile(path, sourcePath2);
                if (copyResult.IsSuccess)
                    _logger.Debug($"  ソースに反映");
                else
                    _logger.Error($"  エラー: {copyResult.ErrorMessage}");
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

    private ISyncStrategy CreateStrategy(SyncScope scope) => scope switch
    {
        SyncScope.FileOnly => new FileOnlySyncStrategy(),
        SyncScope.WithDeletion => new WithDeletionSyncStrategy(),
        _ => new DiffOnlySyncStrategy()
    };

    private string GetScopeDescription(SyncScope scope) => scope switch
    {
        SyncScope.FileOnly => "ファイルコピーのみ",
        SyncScope.WithDeletion => "ファイルコピー＋削除も同期",
        SyncScope.DiffOnly => "差分のみ同期",
        _ => "不明"
    };

    private void ValidatePaths(string sourcePath, string targetPath)
    {
        if (!_fileOps.DirectoryExists(sourcePath))
            throw new DirectoryNotFoundException($"ソースフォルダが見つかりません: {sourcePath}");

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
            _sourceWatcher?.StopWatching();
            _targetWatcher?.StopWatching();
            _sourceWatcher = null;
            _targetWatcher = null;
        }

        _disposed = true;
    }
}
