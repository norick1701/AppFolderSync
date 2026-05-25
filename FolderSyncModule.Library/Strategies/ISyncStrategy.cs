using FolderSyncModule.Library.Models;

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

    /// <summary>
    /// ソースディレクトリのファイルをターゲットディレクトリに同期します（エラーハンドリング付き）。
    /// </summary>
    /// <returns>(成功数, 失敗数, コピーバイト数, エラーリスト)</returns>
    (int success, int failed, long bytesCopied, List<SyncError> errors) SyncFilesWithResult(
        string sourcePath, string targetPath, IFileSystemOperations fileOps);

    /// <summary>
    /// ターゲットに存在するがソースに存在しないファイルを削除します（エラーハンドリング付き）。
    /// </summary>
    /// <returns>(削除成功数, エラーリスト)</returns>
    (int success, List<SyncError> errors) DeleteOrphanedFilesWithResult(
        string sourcePath, string targetPath, IFileSystemOperations fileOps, IDiffDetector diffDetector);
}


