using System.Collections.Concurrent;

namespace FolderSyncModule.Library.Logging;

/// <summary>
/// ファイルにログを出力する実装です。
/// スレッドセーフで、バックグラウンドでバッファリング書き込みを行います。
/// </summary>
public class FileLogger : ILogger, IDisposable
{
    private readonly string _logFilePath;
    private readonly BlockingCollection<string> _logQueue;
    private readonly Task _writerTask;
    private readonly CancellationTokenSource _cts;
    private readonly object _lock = new object();
    private bool _disposed;

    public LogLevel MinLevel { get; set; } = LogLevel.Info;

    /// <summary>
    /// FileLoggerを初期化します。
    /// </summary>
    /// <param name="logFilePath">ログファイルのパス</param>
    /// <param name="bufferSize">バッファサイズ（デフォルト: 100）</param>
    public FileLogger(string logFilePath, int bufferSize = 100)
    {
        _logFilePath = logFilePath;
        _logQueue = new BlockingCollection<string>(bufferSize);
        _cts = new CancellationTokenSource();

        // ログディレクトリが存在しない場合は作成
        var directory = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // バックグラウンドでログを書き込むタスクを開始
        _writerTask = Task.Run(() => WriteLogsAsync(_cts.Token));
    }

    public void Debug(string message)
    {
        Log(LogLevel.Debug, message);
    }

    public void Info(string message)
    {
        Log(LogLevel.Info, message);
    }

    public void Warning(string message)
    {
        Log(LogLevel.Warning, message);
    }

    public void Error(string message, Exception? exception = null)
    {
        Log(LogLevel.Error, message, exception);
    }

    public void Critical(string message, Exception? exception = null)
    {
        Log(LogLevel.Critical, message, exception);
    }

    public void Flush()
    {
        // キューに残っているログがすべて書き込まれるまで待機
        while (_logQueue.Count > 0)
        {
            Thread.Sleep(10);
        }
    }

    private void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (level < MinLevel || _disposed)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = GetLevelString(level);
        var logEntry = $"[{timestamp}] {levelStr} {message}";

        if (exception != null)
        {
            logEntry += $"\n  例外: {exception.GetType().Name}";
            logEntry += $"\n  メッセージ: {exception.Message}";
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                logEntry += $"\n  スタックトレース:\n{exception.StackTrace}";
            }
        }

        try
        {
            _logQueue.Add(logEntry);
        }
        catch (InvalidOperationException)
        {
            // キューが完了している場合は無視
        }
    }

    private async Task WriteLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var writer = new StreamWriter(_logFilePath, append: true, System.Text.Encoding.UTF8)
            {
                AutoFlush = false
            };

            foreach (var logEntry in _logQueue.GetConsumingEnumerable(cancellationToken))
            {
                await writer.WriteLineAsync(logEntry);

                // 一定数溜まったらフラッシュ
                if (_logQueue.Count == 0 || _logQueue.Count % 10 == 0)
                {
                    await writer.FlushAsync();
                }
            }

            await writer.FlushAsync();
        }
        catch (OperationCanceledException)
        {
            // 正常なキャンセル
        }
        catch (Exception)
        {
            // ログ書き込みエラーは無視（ログの無限ループを防ぐ）
        }
    }

    private static string GetLevelString(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Info => "[INFO] ",
            LogLevel.Warning => "[WARN] ",
            LogLevel.Error => "[ERROR]",
            LogLevel.Critical => "[CRIT] ",
            _ => "[?????]"
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;

            // キューを完了としてマーク
            _logQueue.CompleteAdding();

            // 書き込みタスクの完了を待機（最大5秒）
            if (!_writerTask.Wait(TimeSpan.FromSeconds(5)))
            {
                _cts.Cancel();
            }

            _cts.Dispose();
            _logQueue.Dispose();
        }
    }
}
