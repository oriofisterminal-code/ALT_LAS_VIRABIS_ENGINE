namespace Virabis.Core.Debugger;

/// <summary>
/// Interface for debug data providers.
/// "Herkese açık, herkes görsün" - Ahmet Göz (Fotoğrafçı)
/// </summary>
public interface IDebugProvider
{
    /// <summary>
    /// Name of this debug provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets debug data as key-value pairs.
    /// </summary>
    Dictionary<string, object> GetDebugData();

    /// <summary>
    /// Gets formatted debug output.
    /// </summary>
    string GetFormattedOutput();
}

/// <summary>
/// Interface for trackable entities.
/// </summary>
public interface ITrackableEntity
{
    /// <summary>
    /// Unique identifier for tracking.
    /// </summary>
    Guid TrackId { get; }

    /// <summary>
    /// Display name for debug.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets current state snapshot.
    /// </summary>
    TrackedEntitySnapshot GetSnapshot();
}

/// <summary>
/// Snapshot of entity state for tracking/debugging.
/// </summary>
public readonly struct TrackedEntitySnapshot
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Team { get; init; }
    public float CurrentHealth { get; init; }
    public float MaxHealth { get; init; }
    public string State { get; init; }
    public string[] Tags { get; init; }
    public DateTime Timestamp { get; init; }

    public static TrackedEntitySnapshot Empty => new()
    {
        Id = Guid.Empty,
        Name = "Unknown",
        Team = "None",
        CurrentHealth = 0,
        MaxHealth = 0,
        State = "None",
        Tags = Array.Empty<string>(),
        Timestamp = DateTime.UtcNow
    };
}
