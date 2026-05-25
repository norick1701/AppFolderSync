namespace FolderSyncModule.Library.Logging;

/// <summary>
/// 複数のロガーに同時にログを出力する複合ロガーです。
/// </summary>
public class CompositeLogger : ILogger, IDisposable
{
    private readonly List<ILogger> _loggers;
    private bool _disposed;

    public LogLevel MinLevel { get; set; } = LogLevel.Info;

    /// <summary>
    /// CompositeLoggerを初期化します。
    /// </summary>
    /// <param name="loggers">統合するロガーのコレクション</param>
    public CompositeLogger(params ILogger[] loggers)
    {
        _loggers = new List<ILogger>(loggers);
    }

    /// <summary>
    /// 新しいロガーを追加します。
    /// </summary>
    public void AddLogger(ILogger logger)
    {
        _loggers.Add(logger);
    }

    public void Debug(string message)
    {
        if (_disposed) return;
        foreach (var logger in _loggers)
        {
            logger.Debug(message);
        }
    }

    public void Info(string message)
    {
        if (_disposed) return;
        foreach (var logger in _loggers)
        {
            logger.Info(message);
        }
    }

    public void Warning(string message)
    {
        if (_disposed) return;
        foreach (var logger in _loggers)
        {
            logger.Warning(message);
        }
    }

    public void Error(string message, Exception? exception = null)
    {
        if (_disposed) return;
        foreach (var logger in _loggers)
        {
            logger.Error(message, exception);
        }
    }

    public void Critical(string message, Exception? exception = null)
    {
        if (_disposed) return;
        foreach (var logger in _loggers)
        {
            logger.Critical(message, exception);
        }
    }

    public void Flush()
    {
        if (_disposed) return;
        foreach (var logger in _loggers)
        {
            logger.Flush();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var logger in _loggers)
        {
            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _loggers.Clear();
    }
}
