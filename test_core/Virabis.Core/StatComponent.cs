namespace Virabis.Core;

/// <summary>
/// Stores combat statistics for crit calculation.
/// </summary>
public class StatComponent
{
    /// <summary>
    /// Chance to land a critical hit (0.0 to 1.0).
    /// </summary>
    public float CritChance { get; set; } = 0.0f;

    /// <summary>
    /// Damage multiplier on critical hits (default 2.0x).
    /// </summary>
    public float CritMultiplier { get; set; } = 2.0f;

    /// <summary>
    /// Base damage multiplier.
    /// </summary>
    public float DamageMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Movement speed units per second.
    /// </summary>
    public float MoveSpeed { get; set; } = 5.0f;

    /// <summary>
    /// Attack range in units.
    /// </summary>
    public float AttackRange { get; set; } = 1.5f;

    /// <summary>
    /// Attacks per second.
    /// </summary>
    public float AttackSpeed { get; set; } = 1.0f;

    public StatComponent() { }

    public StatComponent(float critChance, float critMultiplier)
    {
        CritChance = Math.Clamp(critChance, 0f, 1f);
        CritMultiplier = Math.Max(1f, critMultiplier);
    }

    /// <summary>
    /// Creates a copy of this stat component.
    /// </summary>
    public StatComponent Clone()
    {
        return new StatComponent
        {
            CritChance = CritChance,
            CritMultiplier = CritMultiplier,
            DamageMultiplier = DamageMultiplier,
            MoveSpeed = MoveSpeed,
            AttackRange = AttackRange,
            AttackSpeed = AttackSpeed
        };
    }
}
