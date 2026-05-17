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
