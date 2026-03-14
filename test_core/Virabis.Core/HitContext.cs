namespace Virabis.Core;

/// <summary>
/// Encapsulates all attack information in a single immutable structure.
/// v2.0: Added validation in constructor.
/// </summary>
public readonly struct HitContext
{
    public Entity Attacker { get; }
    public Entity Target { get; }
    public float BaseDamage { get; }
    public int HitCount { get; }
    public bool IsStealthAttack { get; }

    /// <summary>
    /// Creates a new HitContext.
    /// </summary>
    /// <param name="attacker">Attacking entity (required)</param>
    /// <param name="target">Target entity (required)</param>
    /// <param name="baseDamage">Base damage value (must be non-negative)</param>
    /// <param name="hitCount">Number of hits (minimum 1)</param>
    /// <param name="isStealthAttack">Whether this is a stealth attack</param>
    /// <exception cref="ArgumentNullException">When attacker or target is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">When damage or hitCount is invalid</exception>
    public HitContext(Entity attacker, Entity target, float baseDamage,
                      int hitCount = 1, bool isStealthAttack = false)
    {
        Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        Target = target ?? throw new ArgumentNullException(nameof(target));

        if (baseDamage < 0)
            throw new ArgumentOutOfRangeException(nameof(baseDamage), baseDamage, "Base damage cannot be negative");

        if (hitCount < 1)
            throw new ArgumentOutOfRangeException(nameof(hitCount), hitCount, "Hit count must be at least 1");

        BaseDamage = baseDamage;
        HitCount = hitCount;
        IsStealthAttack = isStealthAttack;
    }

    /// <summary>
    /// Creates a HitContext with validation disabled (for testing/internal use).
    /// </summary>
    internal static HitContext CreateUnsafe(Entity attacker, Entity target, float baseDamage,
                                            int hitCount = 1, bool isStealthAttack = false)
    {
        return new HitContext
        {
            Attacker = attacker,
            Target = target,
            BaseDamage = Math.Max(0, baseDamage),
            HitCount = Math.Max(1, hitCount),
            IsStealthAttack = isStealthAttack
        };
    }

    /// <summary>
    /// Checks if this context is valid (has attacker and target).
    /// </summary>
    public bool IsValid => Attacker != null && Target != null;

    /// <summary>
    /// Creates a builder for constructing HitContext.
    /// </summary>
    public static HitContextBuilder Create() => new();

    /// <summary>
    /// Builder for HitContext.
    /// </summary>
    public struct HitContextBuilder
    {
        private Entity? _attacker;
        private Entity? _target;
        private float _baseDamage;
        private int _hitCount = 1;
        private bool _isStealthAttack;

        public HitContextBuilder()
        {
            _baseDamage = 10f;
        }

        public HitContextBuilder Attacker(Entity attacker)
        {
            _attacker = attacker;
            return this;
        }

        public HitContextBuilder Target(Entity target)
        {
            _target = target;
            return this;
        }

        public HitContextBuilder Damage(float damage)
        {
            _baseDamage = damage;
            return this;
        }

        public HitContextBuilder Hits(int count)
        {
            _hitCount = count;
            return this;
        }

        public HitContextBuilder Stealth(bool isStealth = true)
        {
            _isStealthAttack = isStealth;
            return this;
        }

        public HitContext Build()
        {
            return new HitContext(_attacker!, _target!, _baseDamage, _hitCount, _isStealthAttack);
        }
    }
}
