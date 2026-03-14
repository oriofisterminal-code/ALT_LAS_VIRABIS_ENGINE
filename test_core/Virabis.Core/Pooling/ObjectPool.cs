using System.Collections.Concurrent;

namespace Virabis.Core.Pooling;

/// <summary>
/// Thread-safe object pool implementation.
/// "Bir kere yarat, defalarca kullan" - Can Performans (Yarış Pilotu)
/// </summary>
public class ObjectPool<T> : IObjectPool<T>, IDisposable
    where T : class, IPoolable
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly ConcurrentHashSet<T> _active = new();
    private readonly Func<T> _factory;
    private readonly int _maxCapacity;
    private readonly Action<T>? _onSpawn;
    private readonly Action<T>? _onDespawn;
    private int _totalCreated;

    /// <summary>
    /// Number of objects available in pool.
    /// </summary>
    public int AvailableCount => _pool.Count;

    /// <summary>
    /// Number of objects currently in use.
    /// </summary>
    public int InUseCount => _active.Count;

    /// <summary>
    /// Maximum pool capacity.
    /// </summary>
    public int Capacity => _maxCapacity;

    /// <summary>
    /// Total objects ever created.
    /// </summary>
    public int TotalCreated => _totalCreated;

    /// <summary>
    /// Creates a new object pool.
    /// </summary>
    /// <param name="factory">Factory function to create new objects</param>
    /// <param name="maxCapacity">Maximum pool size (0 = unlimited)</param>
    /// <param name="onSpawn">Optional callback when object is spawned</param>
    /// <param name="onDespawn">Optional callback when object is despawned</param>
    public ObjectPool(
        Func<T> factory,
        int maxCapacity = 100,
        Action<T>? onSpawn = null,
        Action<T>? onDespawn = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _maxCapacity = maxCapacity > 0 ? maxCapacity : int.MaxValue;
        _onSpawn = onSpawn;
        _onDespawn = onDespawn;
    }

    // ========================================
    // RENT / RETURN
    // ========================================

    /// <summary>
    /// Gets an object from the pool (or creates new if empty).
    /// </summary>
    public T Rent()
    {
        T obj;

        if (_pool.TryTake(out var pooled))
        {
            obj = pooled;
        }
        else
        {
            obj = _factory();
            Interlocked.Increment(ref _totalCreated);
        }

        _active.Add(obj);
        obj.OnSpawn();
        _onSpawn?.Invoke(obj);

        return obj;
    }

    /// <summary>
    /// Tries to get an object from pool without creating new.
    /// </summary>
    public bool TryRent(out T? obj)
    {
        if (_pool.TryTake(out var pooled))
        {
            obj = pooled;
            _active.Add(obj);
            obj.OnSpawn();
            _onSpawn?.Invoke(obj);
            return true;
        }

        obj = null;
        return false;
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null) return;

        if (!_active.Remove(obj))
            return; // Wasn't active, ignore

        _onDespawn?.Invoke(obj);
        obj.OnDespawn();

        // Only add to pool if under capacity
        if (_pool.Count < _maxCapacity)
        {
            _pool.Add(obj);
        }
    }

    /// <summary>
    /// Returns all active objects to the pool.
    /// </summary>
    public void ReturnAll()
    {
        foreach (var obj in _active.ToList())
        {
            Return(obj);
        }
    }

    // ========================================
    // PREWARM
    // ========================================

    /// <summary>
    /// Pre-creates objects in the pool.
    /// </summary>
    public void Prewarm(int count)
    {
        count = Math.Min(count, _maxCapacity);

        for (int i = 0; i < count; i++)
        {
            var obj = _factory();
            Interlocked.Increment(ref _totalCreated);
            _pool.Add(obj);
        }
    }

    // ========================================
    // CLEAR
    // ========================================

    /// <summary>
    /// Clears all objects from pool.
    /// </summary>
    public void Clear()
    {
        while (_pool.TryTake(out _) { }
        _active.Clear();
    }

    /// <summary>
    /// Disposes the pool and all disposable objects.
    /// </summary>
    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }

    // ========================================
    // STATISTICS
    // ========================================

    /// <summary>
    /// Gets pool statistics.
    /// </summary>
    public PoolStats GetStats()
    {
        return new PoolStats
        {
            Available = AvailableCount,
            InUse = InUseCount,
            TotalCreated = TotalCreated,
            Capacity = Capacity
        };
    }

    /// <summary>
    /// Checks if an object is active from this pool.
    /// </summary>
    public bool IsActive(T obj) => _active.Contains(obj);
}

/// <summary>
/// Pool statistics.
/// </summary>
public readonly struct PoolStats
{
    public int Available { get; init; }
    public int InUse { get; init; }
    public int TotalCreated { get; init; }
    public int Capacity { get; init; }
    public int Total => Available + InUse;
    public float Utilization => Capacity > 0 ? (float)InUse / Capacity : 0f;
}

/// <summary>
/// Thread-safe hash set for tracking active objects.
/// </summary>
internal class ConcurrentHashSet<T> where T : class
{
    private readonly ConcurrentDictionary<T, byte> _dict = new();

    public int Count => _dict.Count;

    public bool Add(T item) => _dict.TryAdd(item, 0);

    public bool Remove(T item) => _dict.TryRemove(item, out _);

    public bool Contains(T item) => _dict.ContainsKey(item);

    public void Clear() => _dict.Clear();

    public List<T> ToList() => _dict.Keys.ToList();
}
