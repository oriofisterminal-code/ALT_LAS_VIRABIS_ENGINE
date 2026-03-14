namespace Virabis.Core.Nexus.Manifest;

/// <summary>
/// Represents the complete metadata for a Virabis feature.
/// Loaded from JSON manifest files.
/// </summary>
/// <example>
/// Example JSON manifest:
/// <code>
/// {
///   "name": "Combat",
///   "version": "1.0.0",
///   "author": "Virabis Team",
///   "description": "Core combat system",
///   "capabilities": ["damage", "entity"],
///   "dependencies": ["Movement"],
///   "resources": {
///     "init_time_ms": 100,
///     "memory_mb": 10,
///     "cpu_percent": 5
///   },
///   "lifecycle": {
///     "async_init": false,
///     "lazy_load": false,
///     "auto_shutdown": false,
///     "priority": 100
///   },
///   "events": {
///     "publishes": ["DamageDealt", "EntityDied"],
///     "subscribes": ["EntitySpawned"]
///   }
/// }
/// </code>
/// </example>
public class FeatureManifest
{
    /// <summary>
    /// Unique name of the feature (e.g., "Combat", "Movement")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Semantic version (e.g., "1.0.0")
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Author or team name
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// Brief description of the feature
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// List of capabilities this feature provides
    /// </summary>
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// List of feature names this feature depends on
    /// </summary>
    public string[] Dependencies { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Resource budget constraints
    /// </summary>
    public ResourceBudget? Resources { get; set; }
    
    /// <summary>
    /// Lifecycle configuration
    /// </summary>
    public LifecycleConfig? Lifecycle { get; set; }
    
    /// <summary>
    /// Event configuration (published/subscribed events)
    /// </summary>
    public EventConfig? Events { get; set; }
}
