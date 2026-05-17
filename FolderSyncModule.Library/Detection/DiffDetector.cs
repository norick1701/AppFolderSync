namespace FolderSyncModule.Library;

/// <summary>
/// ファイルの差分を検出するクラス。
/// 同期が必要なファイルや孤立したファイルの検出を担当します。
/// </summary>
public class DiffDetector : IDiffDetector
{
    /// <summary>
    /// ファイルが同期対象かどうかを判定します。
    /// ターゲットが存在しない場合、または、ソースが新しい場合に同期対象になります。
    /// </summary>
    public bool NeedsSync(string sourceFile, string targetFile)
    {
        // ターゲットが存在しない場合は同期が必要
        if (!File.Exists(targetFile))
            return true;

        // ソースの方が新しい場合は同期が必要
        var sourceTime = File.GetLastWriteTime(sourceFile);
        var targetTime = File.GetLastWriteTime(targetFile);

        return sourceTime > targetTime;
    }

    /// <summary>
    /// ターゲットに存在するがソースに存在しないファイルを取得します。
    /// 同期後に削除すべき「孤立ファイル」を特定するために使用されます。
    /// </summary>
    public IEnumerable<string> GetOrphanedFiles(string sourcePath, string targetPath)
    {
        var sourceFiles = Directory.GetFiles(sourcePath)
            .Select(f => Path.GetFileName(f))
            .ToHashSet();

        var targetFiles = Directory.GetFiles(targetPath);
        return targetFiles.Where(f => !sourceFiles.Contains(Path.GetFileName(f)));
    }

    /// <summary>
    /// ターゲットに存在するがソースに存在しないディレクトリを取得します。
    /// 同期後に削除すべき「孤立ディレクトリ」を特定するために使用されます。
    /// </summary>
    public IEnumerable<string> GetOrphanedDirectories(string sourcePath, string targetPath)
    {
        var sourceDirs = Directory.GetDirectories(sourcePath)
            .Select(d => Path.GetFileName(d))
            .ToHashSet();

        var targetDirs = Directory.GetDirectories(targetPath);
        return targetDirs.Where(d => !sourceDirs.Contains(Path.GetFileName(d)));
    }
}

