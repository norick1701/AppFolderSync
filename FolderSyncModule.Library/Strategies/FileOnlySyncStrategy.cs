using FolderSyncModule.Library.Models;

namespace FolderSyncModule.Library;

/// <summary>
/// ファイルコピーのみを行う同期戦略。
/// ソースのファイルをターゲットにコピーし、ターゲット固有のファイルは削除しません。
/// </summary>
public class FileOnlySyncStrategy : ISyncStrategy
{
    /// <summary>
    /// ソースのすべてのファイルをターゲットにコピーします。
    /// 既存ファイルは上書きされます。
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
    /// ファイルコピーのみの戦略なので、削除処理は実行しません。
    /// </summary>
    public void DeleteOrphanedFiles(string sourcePath, string targetPath)
    {
        // ファイルコピーのみなので、削除は行わない
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
        
        // 10ファイル以上の場合は並列処理で高速化
        if (sourceFiles.Length >= 10)
        {
            object lockObj = new object();
            
            Parallel.ForEach(sourceFiles, sourceFile =>
            {
                try
                {
                    string fileName = Path.GetFileName(sourceFile);
                    string targetFile = Path.Combine(targetPath, fileName);
                    
                    var copyResult = fileOps.TryCopyFile(sourceFile, targetFile);
                    if (copyResult.IsSuccess)
                    {
                        long fileSize = fileOps.GetFileSize(sourceFile);
                        lock (lockObj)
                        {
                            totalBytes += fileSize;
                            success++;
                        }
                        Console.WriteLine($"  ✓ コピー: {fileName}");
                    }
                    else
                    {
                        lock (lockObj)
                        {
                            failed++;
                            errors.Add(new SyncError(sourceFile, copyResult.ErrorMessage ?? "コピー失敗", copyResult.ExceptionType));
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        failed++;
                        errors.Add(new SyncError(sourceFile, ex.Message, ex.GetType().Name));
                    }
                }
            });
        }
        else
        {
            // 少数のファイルは順次処理（並列化のオーバーヘッドを避ける）
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
        }

        return (success, failed, totalBytes, errors);
    }

    /// <summary>
    /// ファイルコピーのみの戦略なので、削除処理は実行しません（エラーハンドリング付き）。
    /// </summary>
    public (int success, List<SyncError> errors) DeleteOrphanedFilesWithResult(
        string sourcePath, string targetPath, IFileSystemOperations fileOps, IDiffDetector diffDetector)
    {
        // ファイルコピーのみなので、削除は行わない
        return (0, new List<SyncError>());
    }
}

