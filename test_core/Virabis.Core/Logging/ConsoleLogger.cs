using Virabis.Core.Abstractions;

namespace Virabis.Core.Logging;

/// <summary>
/// Console-based logging implementation for development and testing.
/// Thread-safe and lightweight.
/// </summary>
/// <remarks>
/// v3.0: Replaces Console.WriteLine throughout codebase.
///
/// Log format: [LEVEL] [Category] Message
/// Example: [INFO] [Nexus] Registered feature: Combat
///
/// For production, replace with:
/// - Serilog for structured logging
/// - Microsoft.Extensions.Logging for DI integration
/// </remarks>
public class ConsoleLogger : ILoggingService
{
    private readonly string? _category;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new ConsoleLogger with an optional category prefix.
    /// </summary>
    /// <param name="category">Category prefix for all log messages</param>
    public ConsoleLogger(string? category = null)
    {
        _category = category;
    }

    /// <inheritdoc />
    public void LogTrace(string message, params object[] args)
    {
        WriteLog("TRACE", message, args);
    }

    /// <inheritdoc />
    public void LogDebug(string message, params object[] args)
    {
        WriteLog("DEBUG", message, args);
    }

    /// <inheritdoc />
    public void LogInformation(string message, params object[] args)
    {
        WriteLog("INFO", message, args);
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object[] args)
    {
        WriteLog("WARN", message, args);
    }

    /// <inheritdoc />
    public void LogError(string message, params object[] args)
    {
        WriteLog("ERROR", message, args);
    }

    /// <inheritdoc />
    public void LogError(Exception exception, string message, params object[] args)
    {
        WriteLog("ERROR", $"{message} | Exception: {exception.Message}", args);
    }

    /// <inheritdoc />
    public void LogCritical(string message, params object[] args)
    {
        WriteLog("CRITICAL", message, args);
    }

    /// <inheritdoc />
    public ILoggingService CreateScope(string category)
    {
        var newCategory = string.IsNullOrEmpty(_category)
            ? category
            : $"{_category}.{category}";
        return new ConsoleLogger(newCategory);
    }

    private void WriteLog(string level, string message, params object[] args)
    {
        lock (_lock)
        {
            var formattedMessage = args.Length > 0
                ? string.Format(message, args)
                : message;

            var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
            var categoryPart = string.IsNullOrEmpty(_category) ? "" : $"[{_category}] ";

            Console.WriteLine($"[{timestamp}] [{level}] {categoryPart}{formattedMessage}");
        }
    }
}

/// <summary>
/// Null logger for testing or when logging is disabled.
/// </summary>
/// <remarks>
/// Usage in tests:
/// <code>
/// var service = new MyService(NullLogger.Instance);
/// // No log output during test
/// </code>
/// </remarks>
public class NullLogger : ILoggingService
{
    /// <summary>
    /// Singleton instance for reuse.
    /// </summary>
    public static NullLogger Instance { get; } = new();

    public void LogTrace(string message, params object[] args) { }
    public void LogDebug(string message, params object[] args) { }
    public void LogInformation(string message, params object[] args) { }
    public void LogWarning(string message, params object[] args) { }
    public void LogError(string message, params object[] args) { }
    public void LogError(Exception exception, string message, params object[] args) { }
    public void LogCritical(string message, params object[] args) { }
    public ILoggingService CreateScope(string category) => Instance;
}
