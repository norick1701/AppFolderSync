namespace FolderSyncModule.Library.Logging;

/// <summary>
/// コンソールにログを出力する実装です。
/// </summary>
public class ConsoleLogger : ILogger
{
    public LogLevel MinLevel { get; set; } = LogLevel.Info;

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
        // コンソール出力は即座に反映されるため、特に処理は不要
    }

    private void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (level < MinLevel)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var levelStr = GetLevelString(level);
        var color = GetLevelColor(level);

        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.Write($"[{timestamp}] {levelStr} ");
            Console.ForegroundColor = originalColor;
            Console.WriteLine(message);

            if (exception != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  例外: {exception.GetType().Name}");
                Console.WriteLine($"  メッセージ: {exception.Message}");
                if (!string.IsNullOrEmpty(exception.StackTrace))
                {
                    Console.WriteLine($"  スタックトレース:\n{exception.StackTrace}");
                }
                Console.ForegroundColor = originalColor;
            }
        }
        finally
        {
            Console.ForegroundColor = originalColor;
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

    private static ConsoleColor GetLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.Magenta,
            _ => ConsoleColor.White
        };
    }
}
