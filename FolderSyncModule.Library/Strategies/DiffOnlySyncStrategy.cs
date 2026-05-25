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
    /// ソースのファイルをターゲットに同期します。
    /// 新規ファイルまたはソースが新しいファイルのみコピーされます。
    /// ターゲットが新しい場合はスキップされます。
    /// </summary>
    public void SyncFiles(string sourcePath, string targetPath)
    {
        var sourceFiles = Directory.GetFiles(sourcePath);
        foreach (var sourceFile in sourceFiles)
        {
            string fileName = Path.GetFileName(sourceFile);
            string targetFile = Path.Combine(targetPath, fileName);

            if (NeedsCopy(sourceFile, targetFile))
            {
                File.Copy(sourceFile, targetFile, overwrite: true);
                Console.WriteLine($"  ✓ コピー: {fileName}");
            }
            else
            {
                Console.WriteLine($"  - スキップ: {fileName} (変更なし)");
            }
        }
    }

    /// <summary>
    /// 差分のみ同期なので、削除処理は実行しません。
    /// </summary>
    public void DeleteOrphanedFiles(string sourcePath, string targetPath)
    {
        // 差分のみ同期なので、削除は行わない
    }

    /// <summary>
    /// ソースのファイルをターゲットに同期します（エラーハンドリング付き）。
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

                if (NeedsCopy(sourceFile, targetFile))
                {
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
        // 差分のみ同期なので、削除は行わない
        return (0, new List<SyncError>());
    }

    /// <summary>
    /// ターゲットファイルがない、またはソースが新しい場合にコピーが必要と判定します。
    /// ファイルのタイムスタンプを比較することで差分を検出します。
    /// </summary>
    private bool NeedsCopy(string sourceFile, string targetFile)
    {
        // ターゲットが存在しない場合はコピー対象
        if (!File.Exists(targetFile))
            return true;

        // ソースの方が新しい場合はコピー対象
        var sourceTime = File.GetLastWriteTime(sourceFile);
        var targetTime = File.GetLastWriteTime(targetFile);

        return sourceTime > targetTime;
    }
}

