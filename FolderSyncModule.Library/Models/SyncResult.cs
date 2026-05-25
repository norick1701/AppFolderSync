namespace FolderSyncModule.Library.Models;

/// <summary>
/// フォルダ同期処理の結果を表すクラス。
/// 成功、失敗、部分成功を含む詳細な結果情報を提供します。
/// </summary>
public class SyncResult
{
    /// <summary>
    /// 同期が完全に成功したかどうか。
    /// すべてのファイル・ディレクトリが正常に処理された場合に true。
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 部分的に成功したかどうか。
    /// 一部のファイル・ディレクトリが失敗したが、残りは成功した場合に true。
    /// </summary>
    public bool IsPartialSuccess { get; init; }

    /// <summary>
    /// 処理されたファイルの総数。
    /// </summary>
    public int TotalFilesProcessed { get; init; }

    /// <summary>
    /// 正常にコピーされたファイルの数。
    /// </summary>
    public int FilesSucceeded { get; init; }

    /// <summary>
    /// コピーに失敗したファイルの数。
    /// </summary>
    public int FilesFailed { get; init; }

    /// <summary>
    /// 削除されたファイル・ディレクトリの数。
    /// </summary>
    public int FilesDeleted { get; init; }

    /// <summary>
    /// コピーされた総バイト数。
    /// </summary>
    public long TotalBytesCopied { get; init; }

    /// <summary>
    /// 同期処理の実行時間。
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// 失敗したファイルのパスとエラーメッセージのリスト。
    /// </summary>
    public List<SyncError> Errors { get; init; } = new();

    /// <summary>
    /// 完全成功の結果を作成します。
    /// </summary>
    public static SyncResult Success(int filesProcessed, int filesDeleted, long bytesCopied, TimeSpan executionTime)
    {
        return new SyncResult
        {
            IsSuccess = true,
            IsPartialSuccess = false,
            TotalFilesProcessed = filesProcessed,
            FilesSucceeded = filesProcessed,
            FilesFailed = 0,
            FilesDeleted = filesDeleted,
            TotalBytesCopied = bytesCopied,
            ExecutionTime = executionTime
        };
    }

    /// <summary>
    /// 部分成功の結果を作成します。
    /// </summary>
    public static SyncResult PartialSuccess(int filesSucceeded, int filesFailed, int filesDeleted, 
        long bytesCopied, TimeSpan executionTime, List<SyncError> errors)
    {
        return new SyncResult
        {
            IsSuccess = false,
            IsPartialSuccess = true,
            TotalFilesProcessed = filesSucceeded + filesFailed,
            FilesSucceeded = filesSucceeded,
            FilesFailed = filesFailed,
            FilesDeleted = filesDeleted,
            TotalBytesCopied = bytesCopied,
            ExecutionTime = executionTime,
            Errors = errors
        };
    }

    /// <summary>
    /// 完全失敗の結果を作成します。
    /// </summary>
    public static SyncResult Failure(string errorMessage)
    {
        return new SyncResult
        {
            IsSuccess = false,
            IsPartialSuccess = false,
            TotalFilesProcessed = 0,
            FilesSucceeded = 0,
            FilesFailed = 0,
            FilesDeleted = 0,
            TotalBytesCopied = 0,
            ExecutionTime = TimeSpan.Zero,
            Errors = new List<SyncError> { new SyncError(string.Empty, errorMessage) }
        };
    }

    /// <summary>
    /// 結果のサマリーを文字列で取得します。
    /// </summary>
    public string GetSummary()
    {
        if (IsSuccess)
        {
            return $"✓ 同期完了: {FilesSucceeded}個のファイルを処理 ({FormatBytes(TotalBytesCopied)})、{FilesDeleted}個を削除、実行時間: {ExecutionTime.TotalSeconds:F2}秒";
        }
        else if (IsPartialSuccess)
        {
            return $"⚠ 部分成功: {FilesSucceeded}個成功、{FilesFailed}個失敗、{FilesDeleted}個削除、{FormatBytes(TotalBytesCopied)} コピー、実行時間: {ExecutionTime.TotalSeconds:F2}秒";
        }
        else
        {
            return $"✗ 同期失敗: {Errors.FirstOrDefault()?.ErrorMessage ?? "不明なエラー"}";
        }
    }

    /// <summary>
    /// バイト数を人間が読みやすい形式にフォーマットします。
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 同期エラーの詳細情報。
/// </summary>
public class SyncError
{
    /// <summary>
    /// エラーが発生したファイルまたはディレクトリのパス。
    /// </summary>
    public string FilePath { get; init; }

    /// <summary>
    /// エラーメッセージ。
    /// </summary>
    public string ErrorMessage { get; init; }

    /// <summary>
    /// エラーの種類（IOException, UnauthorizedAccessException など）。
    /// </summary>
    public string? ExceptionType { get; init; }

    public SyncError(string filePath, string errorMessage, string? exceptionType = null)
    {
        FilePath = filePath;
        ErrorMessage = errorMessage;
        ExceptionType = exceptionType;
    }
}
