namespace Virabis.Core.Abstractions;

/// <summary>
/// Logging abstraction for Virabis Core.
/// Replaces Console.WriteLine throughout the codebase.
/// </summary>
/// <remarks>
/// v3.0: Added for proper logging support.
///
/// This interface follows the Microsoft.Extensions.Logging pattern
/// but remains lightweight for Godot compatibility.
///
/// Log levels:
/// - Trace: Very detailed logs (development only)
/// - Debug: Debug information
/// - Information: General information
/// - Warning: Warning conditions
/// - Error: Error conditions
/// - Critical: Critical/fatal conditions
/// </remarks>
public interface ILoggingService
{
    /// <summary>
    /// Logs a trace message (most detailed).
    /// </summary>
    void LogTrace(string message, params object[] args);

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    void LogDebug(string message, params object[] args);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void LogInformation(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void LogWarning(string message, params object[] args);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void LogError(string message, params object[] args);

    /// <summary>
    /// Logs an error with exception details.
    /// </summary>
    void LogError(Exception exception, string message, params object[] args);

    /// <summary>
    /// Logs a critical error message.
    /// </summary>
    void LogCritical(string message, params object[] args);

    /// <summary>
    /// Creates a scoped logger with a category prefix.
    /// </summary>
    /// <param name="category">Category name (e.g., "Nexus", "Combat")</param>
    /// <returns>A new logging service with the category prefix</returns>
    ILoggingService CreateScope(string category);
}
