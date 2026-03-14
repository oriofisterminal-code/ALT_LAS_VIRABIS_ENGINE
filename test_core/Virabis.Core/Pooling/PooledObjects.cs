namespace Virabis.Core.Pooling;

/// <summary>
/// Base class for poolable objects with common functionality.
/// </summary>
public abstract class PoolableObject : IPoolable
{
    public bool IsActive { get; protected set; }

    public virtual void OnSpawn()
    {
        IsActive = true;
    }

    public virtual void OnDespawn()
    {
        IsActive = false;
    }

    /// <summary>
    /// Returns this object to its pool (if pooled).
    /// </summary>
    public virtual void ReturnToPool()
    {
        // Override in derived classes with pool reference
    }
}

// ========================================
// POOLED HIT CONTEXT
// ========================================

/// <summary>
/// Poolable hit context for damage calculations.
/// Avoids allocations in hot paths.
/// </summary>
public sealed class PooledHitContext : PoolableObject
{
    public Entity? Attacker { get; set; }
    public Entity? Target { get; set; }
    public float BaseDamage { get; set; }
    public int HitCount { get; set; } = 1;
    public bool IsStealthAttack { get; set; }
    public bool IsCritical { get; set; }
    public float FinalDamage { get; set; }
    public bool IsProcessed { get; set; }

    private static readonly ObjectPool<PooledHitContext> Pool =
        new(() => new PooledHitContext(), 200);

    public static PooledHitContext Rent()
        => Pool.Rent();

    public override void OnSpawn()
    {
        base.OnSpawn();
        Reset();
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        Reset();
    }

    private void Reset()
    {
        Attacker = null;
        Target = null;
        BaseDamage = 0;
        HitCount = 1;
        IsStealthAttack = false;
        IsCritical = false;
        FinalDamage = 0;
        IsProcessed = false;
    }

    public void Initialize(Entity attacker, Entity target, float baseDamage, int hitCount = 1, bool isStealth = false)
    {
        Attacker = attacker;
        Target = target;
        BaseDamage = baseDamage;
        HitCount = hitCount;
        IsStealthAttack = isStealth;
        IsProcessed = false;
    }

    public void Return()
    {
        Pool.Return(this);
    }

    public static implicit operator HitContext(PooledHitContext pooled)
        => new HitContext(pooled.Attacker!, pooled.Target!, pooled.BaseDamage, pooled.HitCount, pooled.IsStealthAttack);
}

// ========================================
// POOLED DAMAGE INFO
// ========================================

/// <summary>
/// Poolable damage information.
/// </summary>
public sealed class PooledDamageInfo : PoolableObject
{
    public Entity? Source { get; set; }
    public Entity? Target { get; set; }
    public float Amount { get; set; }
    public DamageType Type { get; set; }
    public bool IsCritical { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsAbsorbed { get; set; }
    public float AbsorbedAmount { get; set; }

    private static readonly ObjectPool<PooledDamageInfo> Pool =
        new(() => new PooledDamageInfo(), 100);

    public static PooledDamageInfo Rent()
        => Pool.Rent();

    public override void OnDespawn()
    {
        base.OnDespawn();
        Source = null;
        Target = null;
        Amount = 0;
        Type = DamageType.Physical;
        IsCritical = false;
        IsBlocked = false;
        IsAbsorbed = false;
        AbsorbedAmount = 0;
    }

    public void Return()
    {
        Pool.Return(this);
    }
}

/// <summary>
/// Damage type enumeration.
/// </summary>
public enum DamageType
{
    Physical,
    Magical,
    Fire,
    Ice,
    Lightning,
    Poison,
    True,    // Ignores resistances
    Pure     // Cannot be reduced
}

// ========================================
// POOLED EVENT ARGS
// ========================================

/// <summary>
/// Poolable generic event args.
/// </summary>
public sealed class PooledEventArgs : PoolableObject
{
    private static readonly ObjectPool<PooledEventArgs> Pool =
        new(() => new PooledEventArgs(), 50);

    public object? Sender { get; set; }
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; }

    public static PooledEventArgs Rent(object? sender = null, object? data = null)
    {
        var args = Pool.Rent();
        args.Sender = sender;
        args.Data = data;
        args.Timestamp = DateTime.UtcNow;
        return args;
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        Sender = null;
        Data = null;
    }

    public void Return()
    {
        Pool.Return(this);
    }
}

// ========================================
// POOLED STRING BUILDER
// ========================================

/// <summary>
/// Poolable string builder for formatting.
/// </summary>
public sealed class PooledStringBuilder : PoolableObject
{
    private static readonly ObjectPool<PooledStringBuilder> Pool =
        new(() => new PooledStringBuilder(), 20);

    private readonly System.Text.StringBuilder _sb = new(256);

    public System.Text.StringBuilder Builder => _sb;

    public static PooledStringBuilder Rent()
        => Pool.Rent();

    public override void OnDespawn()
    {
        base.OnDespawn();
        _sb.Clear();
    }

    public override string ToString() => _sb.ToString();

    public void Return()
    {
        Pool.Return(this);
    }

    // Convenience methods
    public PooledStringBuilder Append(string? value) { _sb.Append(value); return this; }
    public PooledStringBuilder AppendLine(string? value) { _sb.AppendLine(value); return this; }
    public PooledStringBuilder AppendFormat(string format, params object[] args) { _sb.AppendFormat(format, args); return this; }
    public PooledStringBuilder Clear() { _sb.Clear(); return this; }
}
