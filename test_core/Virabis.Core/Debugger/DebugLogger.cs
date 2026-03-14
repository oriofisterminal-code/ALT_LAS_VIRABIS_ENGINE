namespace Virabis.Core.Debugger;

/// <summary>
/// Debug level for log filtering.
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    None
}

/// <summary>
/// Interface for debug logging.
/// "Şüpheciyim, her detayı görürüm" - Fatma Test (Dedektif)
/// </summary>
public interface IDebugLogger
{
    LogLevel Level { get; set; }

    void Trace(string message);
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? ex = null);
}

/// <summary>
/// Console-based debug logger.
/// </summary>
public class ConsoleDebugLogger : IDebugLogger
{
    public LogLevel Level { get; set; } = LogLevel.Debug;

    public void Trace(string message) => Log(LogLevel.Trace, "TRACE", message);
    public void Debug(string message) => Log(LogLevel.Debug, "DEBUG", message);
    public void Info(string message) => Log(LogLevel.Info, "INFO", message);
    public void Warning(string message) => Log(LogLevel.Warning, "WARN", message);
    public void Error(string message, Exception? ex = null)
    {
        Log(LogLevel.Error, "ERROR", ex != null ? $"{message}\n{ex}" : message);
    }

    private void Log(LogLevel level, string prefix, string message)
    {
        if (level < Level) return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var color = level switch
        {
            LogLevel.Trace => "\x1b[90m",      // Gray
            LogLevel.Debug => "\x1b[36m",      // Cyan
            LogLevel.Info => "\x1b[32m",       // Green
            LogLevel.Warning => "\x1b[33m",    // Yellow
            LogLevel.Error => "\x1b[31m",      // Red
            _ => "\x1b[0m"
        };

        Console.WriteLine($"{color}[{timestamp}] [{prefix}] {message}\x1b[0m");
    }
}

/// <summary>
/// Static debug helper for quick access.
/// </summary>
public static class Debug
{
    private static IDebugLogger _logger = new ConsoleDebugLogger();

    public static IDebugLogger Logger
    {
        get => _logger;
        set => _logger = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static LogLevel Level
    {
        get => _logger.Level;
        set => _logger.Level = value;
    }

    public static void Trace(string message) => _logger.Trace(message);
    public static void Log(string message) => _logger.Debug(message);
    public static void Info(string message) => _logger.Info(message);
    public static void Warn(string message) => _logger.Warning(message);
    public static void Error(string message, Exception? ex = null) => _logger.Error(message, ex);

    /// <summary>
    /// Asserts condition, logs error if false.
    /// </summary>
    public static void Assert(bool condition, string message)
    {
        if (!condition)
            _logger.Error($"Assertion failed: {message}");
    }

    /// <summary>
    /// Logs object properties (reflection-based).
    /// </summary>
    public static void Dump(object obj, string? name = null)
    {
        if (obj == null)
        {
            _logger.Debug($"{name ?? "null"}: null");
            return;
        }

        var type = obj.GetType();
        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        _logger.Debug($"=== {name ?? type.Name} ===");
        foreach (var prop in props)
        {
            try
            {
                var value = prop.GetValue(obj);
                _logger.Debug($"  {prop.Name}: {value}");
            }
            catch (Exception ex)
            {
                _logger.Debug($"  {prop.Name}: <error: {ex.Message}>");
            }
        }
    }
}
