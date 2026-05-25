using FolderSyncModule.Library.Models;

namespace FolderSyncModule.Library;

/// <summary>
/// ファイルシステムの操作をカプセル化するインターフェース。
/// テスト時にモック化可能にするために設計されています。
/// </summary>
public interface IFileSystemOperations
{
    /// <summary>ファイルをコピーします。</summary>
    void CopyFile(string sourceFile, string targetFile);

    /// <summary>ディレクトリを作成します。</summary>
    void CreateDirectory(string directoryPath);

    /// <summary>ファイルを削除します。</summary>
    void DeleteFile(string filePath);

    /// <summary>ディレクトリを削除します。</summary>
    void DeleteDirectory(string directoryPath);

    /// <summary>指定されたパスのすべてのファイルを取得します。</summary>
    string[] GetFiles(string path);

    /// <summary>指定されたパスのすべてのディレクトリを取得します。</summary>
    string[] GetDirectories(string path);

    /// <summary>ファイルが存在するかを確認します。</summary>
    bool FileExists(string path);

    /// <summary>ディレクトリが存在するかを確認します。</summary>
    bool DirectoryExists(string path);

    // 例外安全なメソッド（エラーを Result 型で返す）

    /// <summary>ファイルを安全にコピーします。失敗時は Result でエラー情報を返します。</summary>
    OperationResult TryCopyFile(string sourceFile, string targetFile);

    /// <summary>ファイルを安全に削除します。失敗時は Result でエラー情報を返します。</summary>
    OperationResult TryDeleteFile(string filePath);

    /// <summary>ディレクトリを安全に削除します。失敗時は Result でエラー情報を返します。</summary>
    OperationResult TryDeleteDirectory(string directoryPath);

    /// <summary>指定されたパスのすべてのファイルを取得します（シンボリックリンク除外オプション付き）。</summary>
    OperationResult<string[]> TryGetFiles(string path, bool excludeSymbolicLinks = true);

    /// <summary>指定されたパスのすべてのディレクトリを取得します（シンボリックリンク除外オプション付き）。</summary>
    OperationResult<string[]> TryGetDirectories(string path, bool excludeSymbolicLinks = true);

    /// <summary>ファイルサイズを取得します。</summary>
    long GetFileSize(string filePath);
}
