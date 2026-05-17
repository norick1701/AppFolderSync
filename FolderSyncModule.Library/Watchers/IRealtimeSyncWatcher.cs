namespace FolderSyncModule.Library;

/// <summary>
/// ファイルシステムの変更をリアルタイムで監視するインターフェース。
/// テスト時にモック化可能にするために設計されています。
/// </summary>
public interface IRealtimeSyncWatcher
{
    /// <summary>指定されたフォルダの監視を開始します。</summary>
    void StartWatching(string folderPath);

    /// <summary>監視を停止し、リソースをクリーンアップします。</summary>
    void StopWatching();
}
