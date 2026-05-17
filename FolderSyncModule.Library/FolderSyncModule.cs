namespace FolderSyncModule.Library;

/// <summary>
/// /// 同期モードの定義。
/// </summary>
public enum SyncMode
{
    /// <summary>ソース → ターゲットの一方向同期</summary>
    OneWay,
    /// <summary>ソース ↔ ターゲットの双方向同期</summary>
    TwoWay
}

/// <summary>
/// 同期タイプの定義。
/// </summary>
public enum SyncType
{
    /// <summary>一度だけ実行される同期</summary>
    OneTime,
    /// <summary>ファイル変更を監視して継続的に同期</summary>
    Realtime
}

/// <summary>
/// 同期範囲の定義。
/// </summary>
public enum SyncScope
{
    /// <summary>ファイルコピーのみ。ターゲット固有のファイルは削除されない</summary>
    FileOnly,
    /// <summary>ファイルコピーと削除の両方。ターゲットをソースと完全に一致させる</summary>
    WithDeletion,
    /// <summary>差分のみ同期。新規または更新されたファイルのみコピー</summary>
    DiffOnly
}

/// <summary>
/// フォルダ同期機能のメインAPI。
/// ファサードパターンを使用して、SyncCoordinator の複雑性を隠蔽します。
/// 外部から呼び出すのはこのクラスのメソッドのみです。
/// </summary>
public class FolderSyncModule
{
    private static SyncCoordinator? _coordinator;

    /// <summary>
    /// フォルダの同期を実行します。
    /// </summary>
    /// <param name="sourcePath">同期元ディレクトリパス</param>
    /// <param name="targetPath">同期先ディレクトリパス</param>
    /// <param name="mode">同期モード（デフォルト：単向同期）</param>
    /// <param name="syncType">同期タイプ（デフォルト：ワンタイム同期）</param>
    /// <param name="scope">同期範囲（デフォルト：差分のみ）</param>
    public static void Sync(string sourcePath, string targetPath, SyncMode mode = SyncMode.OneWay,
        SyncType syncType = SyncType.OneTime, SyncScope scope = SyncScope.DiffOnly)
    {
        _coordinator = new SyncCoordinator();
        _coordinator.Sync(sourcePath, targetPath, mode, syncType, scope);
    }

    /// <summary>
    /// リアルタイム監視を停止します。
    /// SyncType.Realtime で同期している場合に呼び出します。
    /// </summary>
    public static void StopRealtimeSync()
    {
        _coordinator?.StopRealtimeSync();
    }
}






