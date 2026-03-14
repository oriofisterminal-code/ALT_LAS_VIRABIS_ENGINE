namespace Virabis.Core.Debugger;

/// <summary>
/// Runtime entity inspector for debugging.
/// "Bulmaca çözüyorum, ipuçları lazım" - Fatma Test (Dedektif)
/// </summary>
public static class EntityDebug
{
    /// <summary>
    /// Gets a formatted snapshot of entity state.
    /// </summary>
    public static EntitySnapshot Snapshot(Entity entity)
    {
        return new EntitySnapshot
        {
            Id = entity.Id,
            TeamId = entity.TeamId,
            IsAlive = entity.Health.IsAlive,
            CurrentHealth = entity.Health.CurrentHealth,
            MaxHealth = entity.Health.MaxHealth,
            HealthPercent = entity.Health.HealthPercent,
            CritChance = entity.Stats.CritChance,
            CritMultiplier = entity.Stats.CritMultiplier,
            CurrentState = entity.State?.CurrentStateName ?? "None",
            CurrentStateId = entity.State?.CurrentStateId ?? -1,
            TimeInState = entity.State?.TimeInCurrentState ?? 0f,
            LegacyTags = entity.Tags.GetTags().ToList(),
            GameplayTags = entity.Tags.GameplayTags.Tags.Select(t => t.Name).ToList()
        };
    }

    /// <summary>
    /// Logs entity details to console.
    /// </summary>
    public static void Log(Entity entity, string? label = null)
    {
        var snapshot = Snapshot(entity);

        Debug.Log($"=== {label ?? "Entity"} ===");
        Debug.Log($"  ID: {snapshot.Id}");
        Debug.Log($"  Team: {snapshot.TeamId}");
        Debug.Log($"  State: {snapshot.CurrentState} (id: {snapshot.CurrentStateId}, time: {snapshot.TimeInState:F2}s)");
        Debug.Log($"  Health: {snapshot.CurrentHealth:F1}/{snapshot.MaxHealth:F1} ({snapshot.HealthPercent:P0})");
        Debug.Log($"  Alive: {snapshot.IsAlive}");
        Debug.Log($"  Crit: {snapshot.CritChance:P0} x{snapshot.CritMultiplier:F1}");
        Debug.Log($"  Tags: [{string.Join(", ", snapshot.LegacyTags)}]");
        Debug.Log($"  GameplayTags: [{string.Join(", ", snapshot.GameplayTags)}]");
    }

    /// <summary>
    /// Quick health status string.
    /// </summary>
    public static string HealthStatus(Entity entity)
        => $"HP: {entity.Health.CurrentHealth:F0}/{entity.Health.MaxHealth:F0} ({entity.Health.HealthPercent:P0})";

    /// <summary>
    /// Quick state status string.
    /// </summary>
    public static string StateStatus(Entity entity)
        => $"State: {entity.State?.CurrentStateName ?? "None"} ({entity.State?.TimeInCurrentState:F1}s)";

    /// <summary>
    /// Compact entity summary.
    /// </summary>
    public static string Summary(Entity entity)
        => $"[{entity.TeamId}] HP:{entity.Health.CurrentHealth:F0}/{entity.Health.MaxHealth:F0} | State:{entity.State?.CurrentStateName ?? "-"}";

    /// <summary>
    /// Creates a comparison between two entities.
    /// </summary>
    public static string Compare(Entity a, Entity b)
    {
        return $"""
=== Entity Comparison ===
           A                    B
ID:        {a.Id}    {b.Id}
Team:      {a.TeamId,-20} {b.TeamId}
Health:    {a.Health.CurrentHealth:F0}/{a.Health.MaxHealth:F0,-15} {b.Health.CurrentHealth:F0}/{b.Health.MaxHealth:F0}
State:     {a.State?.CurrentStateName ?? "None",-20} {b.State?.CurrentStateName ?? "None"}
Crit:      {a.Stats.CritChance:P0} x{a.Stats.CritMultiplier,-12} {b.Stats.CritChance:P0} x{b.Stats.CritMultiplier}
""";
    }
}

/// <summary>
/// Immutable snapshot of entity state for debugging.
/// </summary>
public readonly record struct EntitySnapshot
{
    public Guid Id { get; init; }
    public TeamId TeamId { get; init; }
    public bool IsAlive { get; init; }
    public float CurrentHealth { get; init; }
    public float MaxHealth { get; init; }
    public float HealthPercent { get; init; }
    public float CritChance { get; init; }
    public float CritMultiplier { get; init; }
    public string CurrentState { get; init; }
    public int CurrentStateId { get; init; }
    public float TimeInState { get; init; }
    public IReadOnlyList<string> LegacyTags { get; init; }
    public IReadOnlyList<string> GameplayTags { get; init; }

    /// <summary>
    /// Formats as JSON-like string.
    /// </summary>
    public override string ToString()
        => $$"""
{
  "id": "{{Id}}",
  "team": "{{TeamId}}",
  "alive": {{IsAlive.ToString().ToLower()}},
  "health": {{CurrentHealth:F1}}/{{MaxHealth:F1}},
  "state": "{{CurrentState}}",
  "crit": "{{CritChance:P0}} x{{CritMultiplier:F1}}",
  "tags": [{{string.Join(", ", LegacyTags.Select(t => $"\"{t}\""))}}]
}
""";
}

/// <summary>
/// Debug history tracker for entities.
/// </summary>
public class EntityHistory
{
    private readonly Queue<EntitySnapshot> _history = new();
    private readonly int _maxSize;

    public EntityHistory(int maxSize = 100)
    {
        _maxSize = maxSize;
    }

    /// <summary>
    /// Records a snapshot.
    /// </summary>
    public void Record(Entity entity)
    {
        if (_history.Count >= _maxSize)
            _history.Dequeue();

        _history.Enqueue(EntityDebug.Snapshot(entity));
    }

    /// <summary>
    /// Gets all snapshots.
    /// </summary>
    public IReadOnlyCollection<EntitySnapshot> GetHistory() => _history;

    /// <summary>
    /// Clears history.
    /// </summary>
    public void Clear() => _history.Clear();
}
