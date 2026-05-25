namespace FolderSyncModule.Library.Models;

/// <summary>
/// 同期処理の設定オプションを保持するクラス。
/// Builder パターンで使いやすく設定できます。
/// </summary>
public class SyncOptions
{
    /// <summary>同期元ディレクトリパス</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>同期先ディレクトリパス</summary>
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>同期モード（デフォルト：単向同期）</summary>
    public SyncMode Mode { get; set; } = SyncMode.OneWay;

    /// <summary>同期タイプ（デフォルト：ワンタイム同期）</summary>
    public SyncType SyncType { get; set; } = SyncType.OneTime;

    /// <summary>同期範囲（デフォルト：差分のみ）</summary>
    public SyncScope Scope { get; set; } = SyncScope.DiffOnly;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public SyncOptions()
    {
    }

    /// <summary>
    /// パスを指定するコンストラクタ
    /// </summary>
    public SyncOptions(string sourcePath, string targetPath)
    {
        SourcePath = sourcePath;
        TargetPath = targetPath;
    }

    /// <summary>
    /// 同期モードを設定します
    /// </summary>
    public SyncOptions WithMode(SyncMode mode)
    {
        Mode = mode;
        return this;
    }

    /// <summary>
    /// 同期タイプを設定します
    /// </summary>
    public SyncOptions WithSyncType(SyncType syncType)
    {
        SyncType = syncType;
        return this;
    }

    /// <summary>
    /// 同期範囲を設定します
    /// </summary>
    public SyncOptions WithScope(SyncScope scope)
    {
        Scope = scope;
        return this;
    }
}
