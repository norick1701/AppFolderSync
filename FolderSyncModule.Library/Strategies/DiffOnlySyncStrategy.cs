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

