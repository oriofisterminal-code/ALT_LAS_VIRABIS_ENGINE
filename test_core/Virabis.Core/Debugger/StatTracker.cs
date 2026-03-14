using System.Collections.Concurrent;

namespace Virabis.Core.Debugger;

/// <summary>
/// Performance and gameplay statistics tracker.
/// "Rakamlar yalan söylemez" - Selim İstatistik (Kumarbaz)
///
/// v2.0: Fixed thread safety with ConcurrentQueue for history.
/// </summary>
public class StatTracker : IDebugProvider
{
    private readonly ConcurrentDictionary<string, StatEntry> _stats = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<float>> _history = new();
    private readonly int _maxHistoryLength;

    public string Name => "StatTracker";

    /// <summary>
    /// Creates a new StatTracker.
    /// </summary>
    /// <param name="maxHistoryLength">Max values to keep for averaging</param>
    public StatTracker(int maxHistoryLength = 100)
    {
        _maxHistoryLength = maxHistoryLength;
    }

    // ========================================
    // RECORDING METHODS
    // ========================================

    /// <summary>
    /// Records a value for a stat.
    /// </summary>
    public void Record(string statName, float value)
    {
        if (string.IsNullOrEmpty(statName))
            throw new ArgumentNullException(nameof(statName));

        var entry = _stats.GetOrAdd(statName, _ => new StatEntry());
        entry.Record(value);

        // Add to history with thread-safe queue
        var history = _history.GetOrAdd(statName, _ => new ConcurrentQueue<float>());
        history.Enqueue(value);

        // Trim old values
        while (history.Count > _maxHistoryLength)
        {
            history.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Increments a counter stat.
    /// </summary>
    public void Increment(string statName, float amount = 1f)
    {
        if (string.IsNullOrEmpty(statName))
            throw new ArgumentNullException(nameof(statName));

        var entry = _stats.GetOrAdd(statName, _ => new StatEntry());
        entry.Increment(amount);
    }

    /// <summary>
    /// Sets a stat to a specific value (no history).
    /// </summary>
    public void Set(string statName, float value)
    {
        if (string.IsNullOrEmpty(statName))
            throw new ArgumentNullException(nameof(statName));

        var entry = _stats.GetOrAdd(statName, _ => new StatEntry());
        entry.Set(value);
    }

    // ========================================
    // QUERY METHODS
    // ========================================

    /// <summary>
    /// Gets current value of a stat.
    /// </summary>
    public float? Get(string statName)
    {
        return _stats.TryGetValue(statName, out var entry) ? entry.Current : null;
    }

    /// <summary>
    /// Gets average of a stat over history.
    /// </summary>
    public float? GetAverage(string statName)
    {
        if (!_history.TryGetValue(statName, out var history) || history.IsEmpty)
            return null;

        return history.ToArray().Average();
    }

    /// <summary>
    /// Gets min/max of a stat.
    /// </summary>
    public (float? Min, float? Max) GetRange(string statName)
    {
        if (!_history.TryGetValue(statName, out var history) || history.IsEmpty)
            return (null, null);

        var values = history.ToArray();
        return (values.Min(), values.Max());
    }

    /// <summary>
    /// Gets all stat names.
    /// </summary>
    public IEnumerable<string> GetStatNames() => _stats.Keys;

    /// <summary>
    /// Gets total count for a counter stat.
    /// </summary>
    public float? GetTotal(string statName)
    {
        return _stats.TryGetValue(statName, out var entry) ? entry.Total : null;
    }

    /// <summary>
    /// Gets count of recordings for a stat.
    /// </summary>
    public int GetCount(string statName)
    {
        return _stats.TryGetValue(statName, out var entry) ? entry.Count : 0;
    }

    /// <summary>
    /// Gets history values for a stat.
    /// </summary>
    public IReadOnlyList<float> GetHistory(string statName)
    {
        if (!_history.TryGetValue(statName, out var history))
            return Array.Empty<float>();

        return history.ToArray();
    }

    // ========================================
    // IDebugProvider
    // ========================================

    public Dictionary<string, object> GetDebugData()
    {
        var data = new Dictionary<string, object>();

        foreach (var kvp in _stats.OrderBy(x => x.Key))
        {
            var avg = GetAverage(kvp.Key);
            var (min, max) = GetRange(kvp.Key);

            data[kvp.Key] = new
            {
                Current = kvp.Value.Current,
                Total = kvp.Value.Total,
                Count = kvp.Value.Count,
                Average = avg,
                Min = min,
                Max = max
            };
        }

        return data;
    }

    public string GetFormattedOutput()
    {
        var lines = new List<string> { "=== STAT TRACKER ===" };

        foreach (var statName in _stats.Keys.OrderBy(x => x))
        {
            var current = Get(statName);
            var avg = GetAverage(statName);
            var (min, max) = GetRange(statName);

            var line = $"  {statName}: {current:F1}";
            if (avg.HasValue)
                line += $" (avg: {avg:F1}, min: {min:F1}, max: {max:F1})";

            lines.Add(line);
        }

        return string.Join("\n", lines);
    }

    // ========================================
    // RESET
    // ========================================

    /// <summary>
    /// Clears all stats.
    /// </summary>
    public void Clear()
    {
        _stats.Clear();
        _history.Clear();
    }

    /// <summary>
    /// Clears a specific stat.
    /// </summary>
    public void Clear(string statName)
    {
        if (string.IsNullOrEmpty(statName))
            throw new ArgumentNullException(nameof(statName));

        _stats.TryRemove(statName, out _);
        _history.TryRemove(statName, out _);
    }

    // ========================================
    // PREDEFINED STATS
    // ========================================

    /// <summary>
    /// Common stat names for convenience.
    /// </summary>
    public static class Stats
    {
        public const string FPS = "Performance.FPS";
        public const string EntityCount = "Game.EntityCount";
        public const string DamageDealt = "Combat.DamageDealt";
        public const string DamageTaken = "Combat.DamageTaken";
        public const string Kills = "Combat.Kills";
        public const string Deaths = "Combat.Deaths";
        public const string FrameTime = "Performance.FrameTimeMs";
        public const string MemoryMB = "Performance.MemoryMB";
    }

    // ========================================
    // INNER CLASS
    // ========================================

    private class StatEntry
    {
        private float _current;
        private float _total;
        private int _count;
        private readonly object _lock = new();

        public float Current
        {
            get { lock (_lock) return _current; }
        }

        public float Total
        {
            get { lock (_lock) return _total; }
        }

        public int Count
        {
            get { lock (_lock) return _count; }
        }

        public void Record(float value)
        {
            lock (_lock)
            {
                _current = value;
                _total += value;
                _count++;
            }
        }

        public void Increment(float amount)
        {
            lock (_lock)
            {
                _current += amount;
                _total += amount;
                _count++;
            }
        }

        public void Set(float value)
        {
            lock (_lock)
            {
                _current = value;
            }
        }
    }
}

/// <summary>
/// Extension methods for StatTracker.
/// </summary>
public static class StatTrackerExtensions
{
    /// <summary>
    /// Records damage dealt.
    /// </summary>
    public static void RecordDamageDealt(this StatTracker tracker, float damage)
        => tracker.Increment(StatTracker.Stats.DamageDealt, Math.Max(0, damage));

    /// <summary>
    /// Records damage taken.
    /// </summary>
    public static void RecordDamageTaken(this StatTracker tracker, float damage)
        => tracker.Increment(StatTracker.Stats.DamageTaken, Math.Max(0, damage));

    /// <summary>
    /// Records a kill.
    /// </summary>
    public static void RecordKill(this StatTracker tracker)
        => tracker.Increment(StatTracker.Stats.Kills);

    /// <summary>
    /// Records a death.
    /// </summary>
    public static void RecordDeath(this StatTracker tracker)
        => tracker.Increment(StatTracker.Stats.Deaths);

    /// <summary>
    /// Records FPS.
    /// </summary>
    public static void RecordFPS(this StatTracker tracker, float fps)
        => tracker.Record(StatTracker.Stats.FPS, Math.Max(0, fps));

    /// <summary>
    /// Records entity count.
    /// </summary>
    public static void RecordEntityCount(this StatTracker tracker, int count)
        => tracker.Set(StatTracker.Stats.EntityCount, Math.Max(0, count));
}
