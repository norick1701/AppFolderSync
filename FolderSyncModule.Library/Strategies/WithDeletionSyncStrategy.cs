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
}

