namespace FolderSyncModule.Library.Logging;

/// <summary>
/// ログレベルを定義します。
/// </summary>
public enum LogLevel
{
    /// <summary>デバッグ情報</summary>
    Debug = 0,

    /// <summary>情報メッセージ</summary>
    Info = 1,

    /// <summary>警告メッセージ</summary>
    Warning = 2,

    /// <summary>エラーメッセージ</summary>
    Error = 3,

    /// <summary>致命的なエラー</summary>
    Critical = 4
}
