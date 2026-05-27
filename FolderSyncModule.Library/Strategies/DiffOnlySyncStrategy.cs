using FolderSyncModule.Library.Models;

namespace FolderSyncModule.Library;

/// <summary>
/// 差分のみを同期する戦略。
/// ソースに存在しないか、ソースが新しいファイルのみをターゲットにコピーします。
/// パフォーマンスが重要な場合に使用します。
/// </summary>
public class DiffOnlySyncStrategy : ISyncStrategy
{
    /// <summary>
    /// ソースのファイルをターゲットに同期します（エラーハンドリング付き）。
    /// 新規ファイルまたはソースが新しいファイルのみコピーされます。
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

        foreach (var sourceFile in filesResult.Value ?? Array.Empty<string>())
        {
            try
            {
                string fileName = Path.GetFileName(sourceFile);
                string targetFile = Path.Combine(targetPath, fileName);

                if (NeedsCopy(sourceFile, targetFile))
                {
                    var copyResult = fileOps.TryCopyFile(sourceFile, targetFile);
                    if (copyResult.IsSuccess)
                    {
                        totalBytes += fileOps.GetFileSize(sourceFile);
                        success++;
                        Console.WriteLine($"  ✓ コピー: {fileName}");
                    }
                    else
                    {
                        failed++;
                        errors.Add(new SyncError(sourceFile, copyResult.ErrorMessage ?? "コピー失敗", copyResult.ExceptionType));
                    }
                }
                else
                {
                    Console.WriteLine($"  - スキップ: {fileName} (変更なし)");
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
    /// 差分のみ同期なので、削除処理は実行しません（エラーハンドリング付き）。
    /// </summary>
    public (int success, List<SyncError> errors) DeleteOrphanedFilesWithResult(
        string sourcePath, string targetPath, IFileSystemOperations fileOps, IDiffDetector diffDetector)
    {
        return (0, new List<SyncError>());
    }

    private static bool NeedsCopy(string sourceFile, string targetFile)
    {
        if (!File.Exists(targetFile))
            return true;

        return File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(targetFile);
    }
}

