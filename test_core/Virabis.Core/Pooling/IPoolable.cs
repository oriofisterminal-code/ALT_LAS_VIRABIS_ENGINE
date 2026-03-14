namespace Virabis.Core.Pooling;

/// <summary>
/// Interface for poolable objects.
/// Objects that implement this can be recycled instead of garbage collected.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Called when object is retrieved from pool.
    /// </summary>
    void OnSpawn();

    /// <summary>
    /// Called when object is returned to pool.
    /// Reset state here.
    /// </summary>
    void OnDespawn();

    /// <summary>
    /// Whether this object is currently active (not in pool).
    /// </summary>
    bool IsActive { get; }
}

/// <summary>
/// Interface for object pools.
/// </summary>
public interface IObjectPool<T> where T : class, IPoolable
{
    /// <summary>
    /// Gets an object from the pool (or creates new).
    /// </summary>
    T Rent();

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    void Return(T obj);

    /// <summary>
    /// Pre-warms the pool with objects.
    /// </summary>
    void Prewarm(int count);

    /// <summary>
    /// Number of objects currently available in pool.
    /// </summary>
    int AvailableCount { get; }

    /// <summary>
    /// Number of objects currently in use.
    /// </summary>
    int InUseCount { get; }

    /// <summary>
    /// Total capacity.
    /// </summary>
    int Capacity { get; }

    /// <summary>
    /// Clears all pooled objects.
    /// </summary>
    void Clear();

    /// <summary>
    /// Returns all active objects to pool.
    /// </summary>
    void ReturnAll();
}
