using System.Text.Json.Serialization;

namespace Virabis.Core.Nexus.Manifest;

/// <summary>
/// Defines lifecycle behavior and initialization settings for a feature.
/// Controls how and when a feature is loaded, initialized, and shutdown.
/// </summary>
/// <example>
/// Example configurations:
/// <code>
/// // Eager-loaded, synchronous feature (default)
/// "lifecycle": {
///   "async_init": false,
///   "lazy_load": false,
///   "auto_shutdown": false,
///   "priority": 100
/// }
/// 
/// // Lazy-loaded, async feature with auto-shutdown
/// "lifecycle": {
///   "async_init": true,
///   "lazy_load": true,
///   "auto_shutdown": true,
///   "priority": 50
/// }
/// </code>
/// </example>
/// <remarks>
/// Priority determines initialization order. Higher priority features initialize first.
/// Features with dependencies should have lower priority than their dependencies.
/// </remarks>
public class LifecycleConfig
{
    /// <summary>
    /// Whether feature initialization should be asynchronous.
    /// Set to true for features with long initialization times or I/O operations.
    /// </summary>
    [JsonPropertyName("async_init")]
    public bool AsyncInit { get; set; }
    
    /// <summary>
    /// Whether feature should be loaded on-demand rather than at startup.
    /// Lazy-loaded features are initialized only when first accessed.
    /// </summary>
    [JsonPropertyName("lazy_load")]
    public bool LazyLoad { get; set; }
    
    /// <summary>
    /// Whether feature should automatically shutdown when idle.
    /// Useful for resource-intensive features that aren't always needed.
    /// </summary>
    [JsonPropertyName("auto_shutdown")]
    public bool AutoShutdown { get; set; }
    
    /// <summary>
    /// Initialization priority (higher values = earlier initialization).
    /// Features with dependencies should have lower priority than their dependencies.
    /// Default: 100
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 100;
}
