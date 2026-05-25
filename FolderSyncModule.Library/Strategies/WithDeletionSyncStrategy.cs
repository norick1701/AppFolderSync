using FolderSyncModule.Library.Models;

namespace FolderSyncModule.Library;

/// <summary>
/// ファイルコピーと削除を行う同期戦略。
/// ソースのファイルをターゲットにコピーし、ターゲット固有のファイルも削除します。
/// ターゲットをソースの完全なコピーにしたい場合に使用します。
/// </summary>
public class WithDeletionSyncStrategy : ISyncStrategy
{
    /// <summary>
    /// ソースのすべてのファイルをターゲットにコピーします。
    /// </summary>
    public void SyncFiles(string sourcePath, string targetPath)
    {
        var sourceFiles = Directory.GetFiles(sourcePath);
        foreach (var sourceFile in sourceFiles)
        {
            string fileName = Path.GetFileName(sourceFile);
            string targetFile = Path.Combine(targetPath, fileName);
            File.Copy(sourceFile, targetFile, overwrite: true);
            Console.WriteLine($"  ✓ コピー: {fileName}");
        }
    }

    /// <summary>
    /// ターゲットに存在するがソースに存在しないファイルを削除します。
    /// ターゲットをソースと完全に一致させるために使用されます。
    /// </summary>
    public void DeleteOrphanedFiles(string sourcePath, string targetPath)
    {
        var targetFiles = Directory.GetFiles(targetPath);
        var sourceFiles = Directory.GetFiles(sourcePath).Select(f => Path.GetFileName(f)).ToHashSet();

        foreach (var targetFile in targetFiles)
        {
            if (!sourceFiles.Contains(Path.GetFileName(targetFile)))
            {
                File.Delete(targetFile);
                Console.WriteLine($"  ✓ 削除: {Path.GetFileName(targetFile)} (ソースに存在しない)");
            }
        }
    }

    /// <summary>
    /// ソースのすべてのファイルをターゲットにコピーします（エラーハンドリング付き）。
    /// </summary>
    public (int success, int failed, long bytesCopied, List<SyncError> errors) SyncFilesWithResult(
        string sourcePath, string targetPath, IFileSystemOperations fileOps)
    {
        var errors = new List<SyncError>();
        int success = 0, failed = 0;
        long totalBytes = 0;

        var filesResult = fileOps.TryGetFiles(sourcePath, excludeSymbolicLinks: true);
        if (!filesResult.IsSuccess)
        {
            errors.Add(new SyncError(sourcePath, filesResult.ErrorMessage ?? "ファイル取得失敗", filesResult.ExceptionType));
            return (0, 1, 0, errors);
        }

        var sourceFiles = filesResult.Value ?? Array.Empty<string>();
        foreach (var sourceFile in sourceFiles)
        {
            try
            {
                string fileName = Path.GetFileName(sourceFile);
                string targetFile = Path.Combine(targetPath, fileName);
                
                var copyResult = fileOps.TryCopyFile(sourceFile, targetFile);
                if (copyResult.IsSuccess)
                {
                    long fileSize = fileOps.GetFileSize(sourceFile);
                    totalBytes += fileSize;
                    success++;
                    Console.WriteLine($"  ✓ コピー: {fileName}");
                }
                else
                {
                    failed++;
                    errors.Add(new SyncError(sourceFile, copyResult.ErrorMessage ?? "コピー失敗", copyResult.ExceptionType));
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add(new SyncError(sourceFile, ex.Message, ex.GetType().Name));
            }
        }

        return (success, failed, totalBytes, errors);
    }

    /// <summary>
    /// ターゲットに存在するがソースに存在しないファイルを削除します（エラーハンドリング付き）。
    /// </summary>
    public (int success, List<SyncError> errors) DeleteOrphanedFilesWithResult(
        string sourcePath, string targetPath, IFileSystemOperations fileOps, IDiffDetector diffDetector)
    {
        var errors = new List<SyncError>();
        int success = 0;

        var targetFilesResult = fileOps.TryGetFiles(targetPath, excludeSymbolicLinks: true);
        if (!targetFilesResult.IsSuccess)
        {
            errors.Add(new SyncError(targetPath, targetFilesResult.ErrorMessage ?? "ファイル取得失敗", targetFilesResult.ExceptionType));
            return (0, errors);
        }

        var sourceFilesResult = fileOps.TryGetFiles(sourcePath, excludeSymbolicLinks: true);
        if (!sourceFilesResult.IsSuccess)
        {
            errors.Add(new SyncError(sourcePath, sourceFilesResult.ErrorMessage ?? "ファイル取得失敗", sourceFilesResult.ExceptionType));
            return (0, errors);
        }

        var targetFiles = targetFilesResult.Value ?? Array.Empty<string>();
        var sourceFiles = (sourceFilesResult.Value ?? Array.Empty<string>())
            .Select(f => Path.GetFileName(f))
            .ToHashSet();

        foreach (var targetFile in targetFiles)
        {
            if (!sourceFiles.Contains(Path.GetFileName(targetFile)))
            {
                var deleteResult = fileOps.TryDeleteFile(targetFile);
                if (deleteResult.IsSuccess)
                {
                    success++;
                    Console.WriteLine($"  ✓ 削除: {Path.GetFileName(targetFile)} (ソースに存在しない)");
                }
                else
                {
                    errors.Add(new SyncError(targetFile, deleteResult.ErrorMessage ?? "削除失敗", deleteResult.ExceptionType));
                }
            }
        }

        return (success, errors);
    }
}

