namespace FolderSyncModule.Library;

/// <summary>
/// フォルダ同期の全体調整を行うインターフェース。
/// 複数の責務を持つコンポーネントを統合し、同期処理全体を制御します。
/// </summary>
public interface ISyncCoordinator
{
    /// <summary>
    /// フォルダの同期を実行します。
    /// 指定された設定に従い、ワンタイム同期またはリアルタイム監視を行います。
    /// </summary>
    void Sync(string sourcePath, string targetPath, SyncMode mode, SyncType syncType, SyncScope scope);

    /// <summary>リアルタイム監視を停止します。</summary>
    void StopRealtimeSync();
}
