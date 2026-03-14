using System.Text.Json.Serialization;

namespace Virabis.Core.Nexus.Manifest;

/// <summary>
/// Defines which events a feature publishes and subscribes to.
/// Used for event flow documentation and validation.
/// </summary>
/// <example>
/// Example event configuration:
/// <code>
/// "events": {
///   "publishes": ["DamageDealt", "EntityDied", "CombatStarted"],
///   "subscribes": ["EntitySpawned", "EntityMoved", "PlayerInput"]
/// }
/// </code>
/// </example>
/// <remarks>
/// Event names should be descriptive and follow PascalCase convention.
/// This configuration helps document event flow and can be used for validation.
/// </remarks>
public class EventConfig
{
    /// <summary>
    /// List of event names this feature publishes.
    /// Other features can subscribe to these events.
    /// </summary>
    [JsonPropertyName("publishes")]
    public string[] Publishes { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// List of event names this feature subscribes to.
    /// These events must be published by other features or the system.
    /// </summary>
    [JsonPropertyName("subscribes")]
    public string[] Subscribes { get; set; } = Array.Empty<string>();
}
