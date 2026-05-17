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
}

