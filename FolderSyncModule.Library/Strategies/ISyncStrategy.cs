namespace FolderSyncModule.Library;

/// <summary>
/// ファイル同期の戦略を定義するインターフェース。
/// 異なる同期スコープ（ファイルのみ、削除を含める、差分のみ）に対応するための戦略パターン。
/// </summary>
public interface ISyncStrategy
{
    /// <summary>
    /// ソースディレクトリのファイルをターゲットディレクトリに同期します。
    /// </summary>
    void SyncFiles(string sourcePath, string targetPath);

    /// <summary>
    /// ターゲットに存在するがソースに存在しないファイルを削除します。
    /// </summary>
    void DeleteOrphanedFiles(string sourcePath, string targetPath);
}

