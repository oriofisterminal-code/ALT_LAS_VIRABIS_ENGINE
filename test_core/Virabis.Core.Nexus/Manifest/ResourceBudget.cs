using System.Text.Json.Serialization;

namespace Virabis.Core.Nexus.Manifest;

/// <summary>
/// Defines resource budget constraints for a feature.
/// Used to enforce performance and resource usage limits.
/// </summary>
/// <example>
/// Example usage in manifest:
/// <code>
/// "resources": {
///   "init_time_ms": 100,  // Max 100ms initialization
///   "memory_mb": 10,      // Max 10MB memory usage
///   "cpu_percent": 5      // Max 5% CPU usage
/// }
/// </code>
/// </example>
/// <remarks>
/// These values are used for monitoring and alerting, not hard limits.
/// Features exceeding their budget will log warnings but continue to function.
/// </remarks>
public class ResourceBudget
{
    /// <summary>
    /// Maximum initialization time in milliseconds.
    /// Features exceeding this limit may be flagged for optimization.
    /// </summary>
    [JsonPropertyName("init_time_ms")]
    public int InitTimeMs { get; set; }
    
    /// <summary>
    /// Maximum memory usage in megabytes.
    /// Used for monitoring and preventing memory leaks.
    /// </summary>
    [JsonPropertyName("memory_mb")]
    public int MemoryMb { get; set; }
    
    /// <summary>
    /// Maximum CPU usage percentage (0-100).
    /// Helps identify performance bottlenecks.
    /// </summary>
    [JsonPropertyName("cpu_percent")]
    public int CpuPercent { get; set; }
}
