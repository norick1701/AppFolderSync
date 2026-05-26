namespace FolderSyncModule.Library;

/// <summary>
/// ファイルの差分検出をカプセル化するインターフェース。
/// テスト時にモック化可能にするために設計されています。
/// </summary>
public interface IDiffDetector
{
    /// <summary>ファイルが同期対象かどうかを判定します。</summary>
    bool NeedsSync(string sourceFile, string targetFile);

    /// <summary>ターゲットに存在するがソースに存在しないファイルを取得します。</summary>
    IEnumerable<string> GetOrphanedFiles(string sourcePath, string targetPath);

    /// <summary>ターゲットに存在するがソースに存在しないディレクトリを取得します。</summary>
    IEnumerable<string> GetOrphanedDirectories(string sourcePath, string targetPath);
}

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
        if (!File.Exists(targetFile))
            return true;

        var sourceTime = File.GetLastWriteTime(sourceFile);
        var targetTime = File.GetLastWriteTime(targetFile);

        return sourceTime > targetTime;
    }

    /// <summary>
    /// ターゲットに存在するがソースに存在しないファイルを取得します。
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
