namespace Virabis.Core.Debugger;

/// <summary>
/// Aggregates debug data for overlay display.
/// "Televizyon programcısıyım, her şeyi ekrana basarım" - Deniz Ekran (Gazeteci)
/// </summary>
public class DebugOverlay : IDebugProvider
{
    private readonly List<IDebugProvider> _providers = new();
    private readonly StatTracker _stats;
    private readonly EntityDebugger _entityDebugger;
    private readonly Dictionary<string, Func<string>> _customFields = new();

    public string Name => "DebugOverlay";

    /// <summary>
    /// Whether the overlay is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Access to stat tracker.
    /// </summary>
    public StatTracker Stats => _stats;

    /// <summary>
    /// Access to entity debugger.
    /// </summary>
    public EntityDebugger EntityDebugger => _entityDebugger;

    /// <summary>
    /// Creates a new DebugOverlay with default providers.
    /// </summary>
    public DebugOverlay()
    {
        _stats = new StatTracker();
        _entityDebugger = new EntityDebugger(stats: _stats);

        _providers.Add(_stats);
        _providers.Add(_entityDebugger);
    }

    /// <summary>
    /// Creates a DebugOverlay with custom providers.
    /// </summary>
    public DebugOverlay(StatTracker stats, EntityDebugger entityDebugger)
    {
        _stats = stats;
        _entityDebugger = entityDebugger;

        _providers.Add(_stats);
        _providers.Add(_entityDebugger);
    }

    // ========================================
    // REGISTRATION
    // ========================================

    /// <summary>
    /// Adds a debug provider.
    /// </summary>
    public void AddProvider(IDebugProvider provider)
    {
        _providers.Add(provider);
    }

    /// <summary>
    /// Adds a custom field.
    /// </summary>
    public void AddField(string name, Func<string> getValue)
    {
        _customFields[name] = getValue;
    }

    /// <summary>
    /// Removes a custom field.
    /// </summary>
    public void RemoveField(string name)
    {
        _customFields.Remove(name);
    }

    // ========================================
    // CONVENIENCE METHODS
    // ========================================

    /// <summary>
    /// Registers an entity for debugging.
    /// </summary>
    public void RegisterEntity(Entity entity, string? displayName = null)
    {
        _entityDebugger.Register(entity, displayName);
        _stats.RecordEntityCount(_entityDebugger.EntityCount);
    }

    /// <summary>
    /// Unregisters an entity.
    /// </summary>
    public void UnregisterEntity(Guid entityId)
    {
        _entityDebugger.Unregister(entityId);
        _stats.RecordEntityCount(_entityDebugger.EntityCount);
    }

    // ========================================
    // OUTPUT FORMATS
    // ========================================

    /// <summary>
    /// Gets formatted output for Godot overlay.
    /// </summary>
    public string GetOverlayText()
    {
        if (!IsEnabled) return "";

        var lines = new List<string>
        {
            "═══ VIRABIS DEBUG ═══",
            ""
        };

        // Custom fields first
        if (_customFields.Count > 0)
        {
            foreach (var kvp in _customFields)
            {
                lines.Add($"{kvp.Key}: {kvp.Value()}");
            }
            lines.Add("");
        }

        // Stats summary
        lines.Add(_stats.GetFormattedOutput());
        lines.Add("");

        // Entity debugger
        lines.Add(_entityDebugger.GetFormattedOutput());

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Gets compact one-line format for HUD.
    /// </summary>
    public string GetCompactText()
    {
        if (!IsEnabled) return "";

        var fps = _stats.Get(StatTracker.Stats.FPS) ?? 0;
        var entities = _entityDebugger.EntityCount;
        var damage = _stats.GetTotal(StatTracker.Stats.DamageDealt) ?? 0;
        var kills = _stats.GetTotal(StatTracker.Stats.Kills) ?? 0;

        return $"FPS: {fps:F0} | Entities: {entities} | Damage: {damage:F0} | Kills: {kills:F0}";
    }

    /// <summary>
    /// Gets JSON format for external tools.
    /// </summary>
    public string GetJson()
    {
        var data = GetDebugData();
        return System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    // ========================================
    // IDebugProvider
    // ========================================

    public Dictionary<string, object> GetDebugData()
    {
        var data = new Dictionary<string, object>
        {
            ["IsEnabled"] = IsEnabled
        };

        // Add all provider data
        foreach (var provider in _providers)
        {
            data[provider.Name] = provider.GetDebugData();
        }

        // Add custom fields
        foreach (var kvp in _customFields)
        {
            data[$"Custom_{kvp.Key}"] = kvp.Value();
        }

        return data;
    }

    public string GetFormattedOutput() => GetOverlayText();

    // ========================================
    // STATIC INSTANCE
    // ========================================

    private static DebugOverlay? _instance;

    /// <summary>
    /// Global debug overlay instance.
    /// </summary>
    public static DebugOverlay Instance => _instance ??= new DebugOverlay();

    /// <summary>
    /// Resets the global instance.
    /// </summary>
    public static void Reset()
    {
        _instance?.Clear();
        _instance = null;
    }

    /// <summary>
    /// Clears all data.
    /// </summary>
    public void Clear()
    {
        _stats.Clear();
        _entityDebugger.Clear();
        _customFields.Clear();
    }
}

/// <summary>
/// Extension methods for DebugOverlay.
/// </summary>
public static class DebugOverlayExtensions
{
    /// <summary>
    /// Registers entity with global debug overlay.
    /// </summary>
    public static Entity RegisterForDebug(this Entity entity, string? displayName = null)
    {
        DebugOverlay.Instance.RegisterEntity(entity, displayName);
        return entity;
    }
}
