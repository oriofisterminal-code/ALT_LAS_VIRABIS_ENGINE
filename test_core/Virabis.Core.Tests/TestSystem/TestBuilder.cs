using Virabis.Core;

namespace Virabis.Core.Tests.TestSystem;

/// <summary>
/// Fluent builder for creating damage test scenarios.
/// "Kısa yoldan git" - Uzm. Ali Beyin (Taksi Şoförü)
///
/// Usage:
/// <code>
/// // Simple
/// Test.Hit(10).ShouldDeal(10);
///
/// // Fluent
/// Test.Damage()
///     .Attacker(player)
///     .Target(enemy)
///     .BaseDamage(10)
///     .ShouldDeal(10);
///
/// // With crit
/// Test.Damage()
///     .WithCrit(multiplier: 3f)
///     .BaseDamage(10)
///     .ShouldDeal(30);
/// </code>
/// </summary>
public static class Test
{
    /// <summary>
    /// Creates a damage test scenario with default setup.
    /// </summary>
    public static DamageScenario Damage() => new();

    /// <summary>
    /// Creates a hit test with specified base damage.
    /// Quick one-liner: Test.Hit(10).ShouldDeal(10);
    /// </summary>
    public static DamageScenario Hit(float baseDamage) => new() { BaseDamage = baseDamage };

    /// <summary>
    /// Creates a hit test with attacker and target.
    /// </summary>
    public static DamageScenario Hit(Entity attacker, Entity target, float baseDamage = 10f)
        => new() { Attacker = attacker, Target = target, BaseDamage = baseDamage };

    /// <summary>
    /// Creates a stealth attack test.
    /// </summary>
    public static DamageScenario Stealth(float baseDamage = 10f)
        => new() { BaseDamage = baseDamage, IsStealth = true };

    /// <summary>
    /// Creates a multi-hit test (e.g., shotgun pellets).
    /// </summary>
    public static DamageScenario MultiHit(float damagePerHit, int hitCount)
        => new() { BaseDamage = damagePerHit, HitCount = hitCount };

    /// <summary>
    /// Creates a friendly fire test.
    /// </summary>
    public static DamageScenario FriendlyFire(float damage = 50f)
        => new() { BaseDamage = damage, IsFriendlyFire = true };

    /// <summary>
    /// Creates a crit test with guaranteed crit.
    /// </summary>
    public static DamageScenario Crit(float baseDamage = 10f, float multiplier = 2f)
        => new() { BaseDamage = baseDamage, CritChance = 1f, CritMultiplier = multiplier };
}

/// <summary>
/// Fluent damage test scenario builder.
/// </summary>
public class DamageScenario
{
    // Internal state
    internal Entity? _attacker;
    internal Entity? _target;
    internal float _baseDamage = 10f;
    internal int _hitCount = 1;
    internal bool _isStealth;
    internal bool _isFriendlyFire;
    internal float _critChance;
    internal float _critMultiplier = 2f;
    internal double _randomValue = 0.5; // Default: no crit

    // Properties for configuration
    public Entity Attacker { get => _attacker ??= TestData.Player(); set => _attacker = value; }
    public Entity Target { get => _target ??= TestData.Enemy(); set => _target = value; }
    public float BaseDamage { get => _baseDamage; set => _baseDamage = value; }
    public int HitCount { get => _hitCount; set => _hitCount = value; }
    public bool IsStealth { get => _isStealth; set => _isStealth = value; }
    public float CritChance { get => _critChance; set => _critChance = value; }
    public float CritMultiplier { get => _critMultiplier; set => _critMultiplier = value; }

    // =========================================================================
    // FLUENT CONFIGURATION
    // =========================================================================

    /// <summary>
    /// Sets the attacker entity.
    /// </summary>
    public DamageScenario AttackerIs(Entity attacker)
    {
        _attacker = attacker;
        return this;
    }

    /// <summary>
    /// Sets the target entity.
    /// </summary>
    public DamageScenario TargetIs(Entity target)
    {
        _target = target;
        return this;
    }

    /// <summary>
    /// Sets both attacker and target.
    /// </summary>
    public DamageScenario Between(Entity attacker, Entity target)
    {
        _attacker = attacker;
        _target = target;
        return this;
    }

    /// <summary>
    /// Sets base damage.
    /// </summary>
    public DamageScenario WithDamage(float damage)
    {
        _baseDamage = damage;
        return this;
    }

    /// <summary>
    /// Sets multi-hit count (e.g., shotgun pellets).
    /// </summary>
    public DamageScenario WithHitCount(int count)
    {
        _hitCount = count;
        return this;
    }

    /// <summary>
    /// Makes this a stealth attack.
    /// </summary>
    public DamageScenario AsStealth()
    {
        _isStealth = true;
        return this;
    }

    /// <summary>
    /// Makes this a friendly fire scenario (Player vs Player).
    /// </summary>
    public DamageScenario AsFriendlyFire()
    {
        _isFriendlyFire = true;
        _attacker = TestData.Player();
        _target = TestData.Player();
        return this;
    }

    /// <summary>
    /// Enables guaranteed crit (100% chance).
    /// </summary>
    public DamageScenario WithCrit(float multiplier = 2f)
    {
        _critChance = 1f;
        _critMultiplier = multiplier;
        _randomValue = 0.99; // Force crit
        return this;
    }

    /// <summary>
    /// Sets crit chance (0.0 to 1.0).
    /// </summary>
    public DamageScenario WithCritChance(float chance)
    {
        _critChance = chance;
        return this;
    }

    /// <summary>
    /// Sets random value for deterministic testing.
    /// Use 0.99+ to force crit, less than crit chance to avoid crit.
    /// </summary>
    public DamageScenario WithRandom(double value)
    {
        _randomValue = value;
        return this;
    }

    // =========================================================================
    // EXECUTION & ASSERTION
    // =========================================================================

    /// <summary>
    /// Executes the damage scenario and returns the result.
    /// </summary>
    public DamageResult Execute()
    {
        // Setup attacker and target
        if (_isFriendlyFire)
        {
            _attacker ??= TestData.Player();
            _target ??= TestData.Player();
        }
        else
        {
            _attacker ??= TestData.Player();
            _target ??= TestData.Enemy();
        }

        // Apply crit settings to attacker
        if (_critChance > 0)
        {
            _attacker.Stats.CritChance = _critChance;
            _attacker.Stats.CritMultiplier = _critMultiplier;
        }

        // Create system and hit context
        var system = TestData.DamageSystem(_randomValue);
        var hit = new HitContext(_attacker, _target, _baseDamage, _hitCount, _isStealth);

        // Execute
        float damage = system.ProcessHit(hit);
        float targetHealthAfter = _target.Health.CurrentHealth;

        return new DamageResult(damage, targetHealthAfter, _target.Health.MaxHealth);
    }

    /// <summary>
    /// Asserts the expected damage was dealt.
    /// </summary>
    public void ShouldDeal(float expectedDamage)
    {
        var result = Execute();
        Xunit.Assert.Equal(expectedDamage, result.DamageDealt, precision: 2);
    }

    /// <summary>
    /// Asserts the expected damage with tolerance.
    /// </summary>
    public void ShouldDealApprox(float expectedDamage, float tolerance = 0.01f)
    {
        var result = Execute();
        Xunit.Assert.True(
            Math.Abs(result.DamageDealt - expectedDamage) <= tolerance,
            $"Expected ~{expectedDamage} damage, got {result.DamageDealt}");
    }

    /// <summary>
    /// Asserts damage is within expected range.
    /// </summary>
    public void ShouldDealBetween(float min, float max)
    {
        var result = Execute();
        Xunit.Assert.True(result.DamageDealt >= min && result.DamageDealt <= max,
            $"Expected damage between {min} and {max}, got {result.DamageDealt}");
    }

    /// <summary>
    /// Asserts the target's health after damage.
    /// </summary>
    public void ShouldLeaveHealth(float expectedHealth)
    {
        var result = Execute();
        Xunit.Assert.Equal(expectedHealth, result.TargetHealthAfter, precision: 2);
    }

    /// <summary>
    /// Asserts zero damage (e.g., friendly fire).
    /// </summary>
    public void ShouldDealZero()
    {
        var result = Execute();
        Xunit.Assert.Equal(0f, result.DamageDealt);
    }

    /// <summary>
    /// Asserts damage is greater than zero.
    /// </summary>
    public void ShouldDealSomeDamage()
    {
        var result = Execute();
        Xunit.Assert.True(result.DamageDealt > 0, "Expected some damage to be dealt");
    }

    /// <summary>
    /// Asserts the target is dead after the hit.
    /// </summary>
    public void ShouldKill()
    {
        Execute();
        Xunit.Assert.False(_target!.Health.IsAlive, "Expected target to be dead");
    }

    /// <summary>
    /// Asserts the target survives the hit.
    /// </summary>
    public void ShouldNotKill()
    {
        Execute();
        Xunit.Assert.True(_target!.Health.IsAlive, "Expected target to survive");
    }
}

/// <summary>
/// Result of a damage test execution.
/// </summary>
public readonly record struct DamageResult(
    float DamageDealt,
    float TargetHealthAfter,
    float TargetMaxHealth
);
