namespace FolderSyncModule.Library.Logging;

/// <summary>
/// ロギング機能を提供するインターフェースです。
/// </summary>
public interface ILogger
{
    /// <summary>
    /// 最小ログレベルを取得または設定します。
    /// これより低いレベルのログは出力されません。
    /// </summary>
    LogLevel MinLevel { get; set; }

    /// <summary>
    /// デバッグレベルのログを記録します。
    /// </summary>
    void Debug(string message);

    /// <summary>
    /// 情報レベルのログを記録します。
    /// </summary>
    void Info(string message);

    /// <summary>
    /// 警告レベルのログを記録します。
    /// </summary>
    void Warning(string message);

    /// <summary>
    /// エラーレベルのログを記録します。
    /// </summary>
    void Error(string message, Exception? exception = null);

    /// <summary>
    /// 致命的なエラーレベルのログを記録します。
    /// </summary>
    void Critical(string message, Exception? exception = null);

    /// <summary>
    /// ログ出力をフラッシュします。
    /// </summary>
    void Flush();
}
