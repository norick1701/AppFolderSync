namespace FolderSyncModule.Library.Models;

/// <summary>
/// ファイルシステム操作の結果を表すクラス。
/// 成功または失敗の情報と、失敗時のエラー詳細を保持します。
/// </summary>
public class OperationResult
{
    /// <summary>
    /// 操作が成功したかどうか。
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// エラーメッセージ（失敗時）。
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 例外の種類（失敗時）。
    /// </summary>
    public string? ExceptionType { get; init; }

    /// <summary>
    /// 操作対象のファイルまたはディレクトリのパス。
    /// </summary>
    public string? TargetPath { get; init; }

    /// <summary>
    /// 成功結果を作成します。
    /// </summary>
    public static OperationResult Success(string? targetPath = null)
    {
        return new OperationResult
        {
            IsSuccess = true,
            TargetPath = targetPath
        };
    }

    /// <summary>
    /// 失敗結果を作成します。
    /// </summary>
    public static OperationResult Failure(string errorMessage, string? exceptionType = null, string? targetPath = null)
    {
        return new OperationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExceptionType = exceptionType,
            TargetPath = targetPath
        };
    }
}

/// <summary>
/// ジェネリック型のファイルシステム操作結果。
/// 成功時に値を返す操作に使用します。
/// </summary>
public class OperationResult<T>
{
    /// <summary>
    /// 操作が成功したかどうか。
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 操作の結果値（成功時）。
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// エラーメッセージ（失敗時）。
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 例外の種類（失敗時）。
    /// </summary>
    public string? ExceptionType { get; init; }

    /// <summary>
    /// 成功結果を作成します。
    /// </summary>
    public static OperationResult<T> Success(T value)
    {
        return new OperationResult<T>
        {
            IsSuccess = true,
            Value = value
        };
    }

    /// <summary>
    /// 失敗結果を作成します。
    /// </summary>
    public static OperationResult<T> Failure(string errorMessage, string? exceptionType = null)
    {
        return new OperationResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExceptionType = exceptionType
        };
    }
}
