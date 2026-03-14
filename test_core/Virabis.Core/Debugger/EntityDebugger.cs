using System.Collections.Concurrent;
using Virabis.Core.State;

namespace Virabis.Core.Debugger;

/// <summary>
/// Runtime entity debugger and monitor.
/// "Her şeyi görürüm, hiçbir şey kaçmaz" - Kemal Dedektif (Roman Yazarı)
///
/// v2.0: Fixed memory leak with proper event unsubscription using WeakReference pattern.
/// </summary>
public class EntityDebugger : IDebugProvider
{
    private readonly ConcurrentDictionary<Guid, EntityDebugInfo> _entities = new();
    private readonly ConcurrentDictionary<Guid, EntitySubscriptions> _subscriptions = new();
    private readonly ConcurrentQueue<DebugEvent> _events = new();
    private readonly int _maxEvents;
    private readonly StatTracker? _stats;

    public string Name => "EntityDebugger";

    /// <summary>
    /// Number of tracked entities.
    /// </summary>
    public int EntityCount => _entities.Count;

    /// <summary>
    /// Creates a new EntityDebugger.
    /// </summary>
    public EntityDebugger(int maxEvents = 100, StatTracker? stats = null)
    {
        _maxEvents = maxEvents;
        _stats = stats;
    }

    // ========================================
    // REGISTRATION
    // ========================================

    /// <summary>
    /// Registers an entity for debugging.
    /// </summary>
    public void Register(Entity entity, string? displayName = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var info = new EntityDebugInfo
        {
            Id = entity.Id,
            DisplayName = displayName ?? $"Entity_{entity.Id.ToString()[..8]}",
            Team = entity.TeamId.ToString(),
            MaxHealth = entity.Health.MaxHealth,
            LastKnownHealth = entity.Health.CurrentHealth,
            LastKnownState = entity.State?.CurrentStateName ?? "None",
            Tags = entity.Tags.GetTags().ToArray(),
            WeakRef = new WeakReference<Entity>(entity)
        };

        _entities[entity.Id] = info;

        // Subscribe to events with proper tracking
        var subscriptions = new EntitySubscriptions();

        subscriptions.HealthHandler = (delta) =>
        {
            if (_entities.TryGetValue(entity.Id, out var eInfo))
            {
                eInfo.LastKnownHealth = entity.Health.CurrentHealth;
                var direction = delta < 0 ? "damage" : "heal";
                LogEvent("HealthChanged", entity.Id, $"{direction}: {Math.Abs(delta):F1}, now: {entity.Health.CurrentHealth:F1}");
            }
        };

        subscriptions.StateHandler = (evt) =>
        {
            if (_entities.TryGetValue(entity.Id, out var eInfo))
            {
                eInfo.LastKnownState = evt.NewStateName;
                LogEvent("StateChanged", entity.Id, $"{evt.PreviousStateName} -> {evt.NewStateName}");
            }
        };

        // Subscribe
        entity.Health.HealthChanged += subscriptions.HealthHandler;
        if (entity.State != null)
        {
            entity.State.OnStateChange += subscriptions.StateHandler;
        }

        _subscriptions[entity.Id] = subscriptions;

        LogEvent("EntityRegistered", entity.Id, $"Registered: {info.DisplayName}");
    }

    /// <summary>
    /// Unregisters an entity and cleans up subscriptions.
    /// </summary>
    public void Unregister(Guid entityId)
    {
        // Remove subscriptions first (prevent memory leak)
        if (_subscriptions.TryRemove(entityId, out var subscriptions))
        {
            // Try to get entity reference for unsubscription
            if (_entities.TryGetValue(entityId, out var info) &&
                info.WeakRef.TryGetTarget(out var entity))
            {
                // Unsubscribe from events
                entity.Health.HealthChanged -= subscriptions.HealthHandler;
                if (entity.State != null)
                {
                    entity.State.OnStateChange -= subscriptions.StateHandler;
                }
            }
        }

        if (_entities.TryRemove(entityId, out var removedInfo))
        {
            LogEvent("EntityUnregistered", entityId, $"Unregistered: {removedInfo.DisplayName}");
        }
    }

    // ========================================
    // SNAPSHOT
    // ========================================

    /// <summary>
    /// Gets a snapshot of an entity's current state.
    /// </summary>
    public TrackedEntitySnapshot GetSnapshot(Entity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        return new TrackedEntitySnapshot
        {
            Id = entity.Id,
            Name = _entities.TryGetValue(entity.Id, out var info) ? info.DisplayName : "Unknown",
            Team = entity.TeamId.ToString(),
            CurrentHealth = entity.Health.CurrentHealth,
            MaxHealth = entity.Health.MaxHealth,
            State = entity.State?.CurrentStateName ?? "None",
            Tags = entity.Tags.GetTags().ToArray(),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets snapshots of all tracked entities.
    /// </summary>
    public IEnumerable<TrackedEntitySnapshot> GetAllSnapshots()
    {
        return _entities.Values.Select(info => new TrackedEntitySnapshot
        {
            Id = info.Id,
            Name = info.DisplayName,
            Team = info.Team,
            CurrentHealth = info.LastKnownHealth,
            MaxHealth = info.MaxHealth,
            State = info.LastKnownState,
            Tags = info.Tags,
            Timestamp = DateTime.UtcNow
        });
    }

    // ========================================
    // QUERIES
    // ========================================

    /// <summary>
    /// Gets entities by team.
    /// </summary>
    public IEnumerable<EntityDebugInfo> GetByTeam(TeamId team)
    {
        return _entities.Values.Where(e => e.Team == team.ToString());
    }

    /// <summary>
    /// Gets entities by state.
    /// </summary>
    public IEnumerable<EntityDebugInfo> GetByState(string stateName)
    {
        return _entities.Values.Where(e =>
            e.LastKnownState.Equals(stateName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets entities below health threshold.
    /// </summary>
    public IEnumerable<EntityDebugInfo> GetLowHealth(float thresholdPercent = 0.25f)
    {
        return _entities.Values.Where(e =>
            e.MaxHealth > 0 && e.LastKnownHealth / e.MaxHealth < thresholdPercent);
    }

    /// <summary>
    /// Gets recent debug events.
    /// </summary>
    public IEnumerable<DebugEvent> GetRecentEvents(int count = 20)
    {
        return _events.TakeLast(count);
    }

    /// <summary>
    /// Gets live entity reference if still alive.
    /// </summary>
    public Entity? TryGetEntity(Guid entityId)
    {
        if (_entities.TryGetValue(entityId, out var info) &&
            info.WeakRef.TryGetTarget(out var entity))
        {
            return entity;
        }
        return null;
    }

    /// <summary>
    /// Cleans up dead references (entities that have been GC'd).
    /// </summary>
    public int CleanupDeadReferences()
    {
        var deadIds = _entities
            .Where(kvp => !kvp.Value.WeakRef.TryGetTarget(out _))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in deadIds)
        {
            Unregister(id);
        }

        return deadIds.Count;
    }

    // ========================================
    // LOGGING
    // ========================================

    /// <summary>
    /// Logs a custom debug event.
    /// </summary>
    public void LogEvent(string eventType, Guid? entityId, string message)
    {
        var debugEvent = new DebugEvent
        {
            Timestamp = DateTime.UtcNow,
            EventType = eventType,
            EntityId = entityId,
            Message = message
        };

        _events.Enqueue(debugEvent);

        // Trim old events
        while (_events.Count > _maxEvents)
        {
            _events.TryDequeue(out _);
        }
    }

    // ========================================
    // IDebugProvider
    // ========================================

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["EntityCount"] = EntityCount,
            ["Entities"] = GetAllSnapshots().ToList(),
            ["RecentEvents"] = GetRecentEvents(10).ToList()
        };
    }

    public string GetFormattedOutput()
    {
        var lines = new List<string>
        {
            "=== ENTITY DEBUGGER ===",
            $"Total Entities: {EntityCount}",
            ""
        };

        // Group by team
        var byTeam = _entities.Values.GroupBy(e => e.Team);
        foreach (var group in byTeam)
        {
            lines.Add($"[{group.Key}]");
            foreach (var entity in group)
            {
                var healthPercent = entity.MaxHealth > 0
                    ? entity.LastKnownHealth / entity.MaxHealth * 100
                    : 0;
                lines.Add($"  {entity.DisplayName}: {entity.LastKnownState} | HP: {entity.LastKnownHealth:F0}/{entity.MaxHealth:F0} ({healthPercent:F0}%)");
            }
        }

        // Recent events
        lines.Add("");
        lines.Add("=== RECENT EVENTS ===");
        foreach (var evt in GetRecentEvents(5))
        {
            lines.Add($"  [{evt.Timestamp:HH:mm:ss.fff}] {evt.EventType}: {evt.Message}");
        }

        return string.Join("\n", lines);
    }

    // ========================================
    // CLEAR
    // ========================================

    /// <summary>
    /// Clears all tracked entities and events.
    /// </summary>
    public void Clear()
    {
        // Unsubscribe all
        foreach (var kvp in _subscriptions)
        {
            if (_entities.TryGetValue(kvp.Key, out var info) &&
                info.WeakRef.TryGetTarget(out var entity))
            {
                entity.Health.HealthChanged -= kvp.Value.HealthHandler;
                if (entity.State != null)
                {
                    entity.State.OnStateChange -= kvp.Value.StateHandler;
                }
            }
        }

        _entities.Clear();
        _subscriptions.Clear();
        while (_events.TryDequeue(out _) { }
    }
}

/// <summary>
/// Debug info for a tracked entity.
/// </summary>
public class EntityDebugInfo
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = "Unknown";
    public string Team { get; init; } = "None";
    public float MaxHealth { get; init; }
    public float LastKnownHealth { get; set; }
    public string LastKnownState { get; set; } = "None";
    public string[] Tags { get; init; } = Array.Empty<string>();
    internal WeakReference<Entity> WeakRef { get; set; } = null!;
}

/// <summary>
/// Holds event subscriptions for cleanup.
/// </summary>
internal class EntitySubscriptions
{
    public Action<float> HealthHandler { get; set; } = _ => { };
    public Action<StateChangeEvent> StateHandler { get; set; } = _ => { };
}

/// <summary>
/// A debug event.
/// </summary>
public readonly struct DebugEvent
{
    public DateTime Timestamp { get; init; }
    public string EventType { get; init; }
    public Guid? EntityId { get; init; }
    public string Message { get; init; }
}
