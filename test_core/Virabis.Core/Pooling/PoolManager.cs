namespace Virabis.Core.Pooling;

/// <summary>
/// Central manager for all object pools.
/// "Havuzları tek elden yönetelim" - Selim Entegrasyon (Lego Ustası)
/// </summary>
public static class PoolManager
{
    private static readonly ConcurrentDictionary<Type, object> _pools = new();
    private static readonly ConcurrentDictionary<string, object> _namedPools = new(StringComparer.OrdinalIgnoreCase);

    // ========================================
    // GENERIC POOL ACCESS
    // ========================================

    /// <summary>
    /// Gets or creates a pool for a type.
    /// </summary>
    public static ObjectPool<T> GetPool<T>(
        Func<T>? factory = null,
        int capacity = 100,
        Action<T>? onSpawn = null,
        Action<T>? onDespawn = null)
        where T : class, IPoolable, new()
    {
        var type = typeof(T);

        if (_pools.TryGetValue(type, out var existingPool))
            return (ObjectPool<T>)existingPool;

        var pool = new ObjectPool<T>(
            factory ?? (() => new T()),
            capacity,
            onSpawn,
            onDespawn);

        _pools[type] = pool;
        return pool;
    }

    /// <summary>
    /// Gets an existing pool or returns null.
    /// </summary>
    public static ObjectPool<T>? GetExistingPool<T>()
        where T : class, IPoolable
    {
        return _pools.TryGetValue(typeof(T), out var pool) ? pool as ObjectPool<T> : null;
    }

    // ========================================
    // NAMED POOLS
    // ========================================

    /// <summary>
    /// Registers a named pool (for multiple pools of same type).
    /// </summary>
    public static void RegisterNamedPool<T>(string name, ObjectPool<T> pool)
        where T : class, IPoolable
    {
        _namedPools[name] = pool;
    }

    /// <summary>
    /// Gets a named pool.
    /// </summary>
    public static ObjectPool<T>? GetNamedPool<T>(string name)
        where T : class, IPoolable
    {
        return _namedPools.TryGetValue(name, out var pool) ? pool as ObjectPool<T> : null;
    }

    // ========================================
    // CONVENIENCE METHODS
    // ========================================

    /// <summary>
    /// Rents an object from the type's pool.
    /// </summary>
    public static T Rent<T>() where T : class, IPoolable, new()
        => GetPool<T>().Rent();

    /// <summary>
    /// Returns an object to its pool.
    /// </summary>
    public static void Return<T>(T obj) where T : class, IPoolable, new()
    {
        var pool = GetExistingPool<T>();
        pool?.Return(obj);
    }

    /// <summary>
    /// Prewarms a pool.
    /// </summary>
    public static void Prewarm<T>(int count) where T : class, IPoolable, new()
        => GetPool<T>().Prewarm(count);

    // ========================================
    // STATISTICS
    // ========================================

    /// <summary>
    /// Gets statistics for all pools.
    /// </summary>
    public static IReadOnlyDictionary<Type, PoolStats> GetAllStats()
    {
        return _pools.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var poolType = kvp.Value.GetType();
                var getStatsMethod = poolType.GetMethod("GetStats");
                return getStatsMethod?.Invoke(kvp.Value, null) is PoolStats stats
                    ? stats
                    : default;
            });
    }

    /// <summary>
    /// Gets total objects across all pools.
    /// </summary>
    public static (int Available, int InUse) GetTotals()
    {
        int available = 0, inUse = 0;

        foreach (var kvp in _pools)
        {
            var poolType = kvp.Value.GetType();
            var getStatsMethod = poolType.GetMethod("GetStats");
            if (getStatsMethod?.Invoke(kvp.Value, null) is PoolStats stats)
            {
                available += stats.Available;
                inUse += stats.InUse;
            }
        }

        return (available, inUse);
    }

    // ========================================
    // CLEAR
    // ========================================

    /// <summary>
    /// Returns all objects in all pools.
    /// </summary>
    public static void ReturnAll()
    {
        foreach (var kvp in _pools)
        {
            var poolType = kvp.Value.GetType();
            var returnAllMethod = poolType.GetMethod("ReturnAll");
            returnAllMethod?.Invoke(kvp.Value, null);
        }
    }

    /// <summary>
    /// Clears all pools.
    /// </summary>
    public static void ClearAll()
    {
        foreach (var kvp in _pools)
        {
            if (kvp.Value is IDisposable disposable)
                disposable.Dispose();
        }

        _pools.Clear();
        _namedPools.Clear();
    }

    /// <summary>
    /// Clears a specific pool.
    /// </summary>
    public static void Clear<T>() where T : class, IPoolable
    {
        if (_pools.TryRemove(typeof(T), out var pool) && pool is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
