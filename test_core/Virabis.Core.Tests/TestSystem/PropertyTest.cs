using CsCheck;
using Virabis.Core;

namespace Virabis.Core.Tests.TestSystem;

/// <summary>
/// Property-based testing helper for damage system.
/// "Görünmeyeni görünür kılar" - Uzm. Zeynep Röntgen (Dedektif)
///
/// Usage:
/// <code>
/// // Simple property
/// Property.ForAll<HitScenario>()
///     .Assert(s => s.Damage >= 0);
///
/// // Built-in properties
/// Property.Damage_NeverNegative();
/// Property.Health_NeverBelowZero();
/// Property.FriendlyFire_AlwaysZero();
/// </code>
/// </summary>
public static class Property
{
    /// <summary>
    /// Default iteration count for property tests.
    /// </summary>
    public static int DefaultIterations => GetIterationCount();

    private static int GetIterationCount()
    {
        var iterEnv = Environment.GetEnvironmentVariable("CSCHECK_ITER");
        return int.TryParse(iterEnv, out var iter) ? iter : 100;
    }

    // =========================================================================
    // GENERATORS
    // =========================================================================

    /// <summary>
    /// Generator for damage values (0 to 1000).
    /// </summary>
    public static Gen<float> Damage => Gen.Float[0f, 1000f];

    /// <summary>
    /// Generator for health values (1 to 1000).
    /// </summary>
    public static Gen<float> Health => Gen.Float[1f, 1000f];

    /// <summary>
    /// Generator for crit chance (0 to 1).
    /// </summary>
    public static Gen<float> CritChance => Gen.Float[0f, 1f];

    /// <summary>
    /// Generator for crit multiplier (1.5 to 5.0).
    /// </summary>
    public static Gen<float> CritMultiplier => Gen.Float[1.5f, 5f];

    /// <summary>
    /// Generator for hit count (1 to 10).
    /// </summary>
    public static Gen<int> HitCount => Gen.Int[1, 10];

    /// <summary>
    /// Generator for TeamId.
    /// </summary>
    public static Gen<TeamId> TeamIdGen => Gen.Int[0, 3].Select(i => new TeamId(i));

    /// <summary>
    /// Generator for a complete damage scenario.
    /// </summary>
    public static Gen<DamageScenarioGen> Scenario =>
        from baseDamage in Damage
        from attackerHealth in Health
        from targetHealth in Health
        from critChance in CritChance
        from critMultiplier in CritMultiplier
        from hitCount in HitCount
        from isStealth in Gen.Bool
        from randomValue in Gen.Double[0, 1]
        select new DamageScenarioGen(
            baseDamage,
            attackerHealth,
            targetHealth,
            critChance,
            critMultiplier,
            hitCount,
            isStealth,
            randomValue
        );

    // =========================================================================
    // BUILT-IN PROPERTY TESTS
    // =========================================================================

    /// <summary>
    /// Property: Damage is never negative.
    /// </summary>
    public static void Damage_NeverNegative(int? iterations = null)
    {
        Scenario.Sample(s =>
        {
            var result = s.Execute();
            Xunit.Assert.True(result.DamageDealt >= 0,
                $"Damage should never be negative, got {result.DamageDealt}");
        }, iterations ?? DefaultIterations);
    }

    /// <summary>
    /// Property: Target health never goes below zero.
    /// </summary>
    public static void Health_NeverBelowZero(int? iterations = null)
    {
        Scenario.Sample(s =>
        {
            var result = s.Execute();
            Xunit.Assert.True(result.TargetHealthAfter >= 0,
                $"Health should never be negative, got {result.TargetHealthAfter}");
        }, iterations ?? DefaultIterations);
    }

    /// <summary>
    /// Property: Friendly fire always deals zero damage.
    /// </summary>
    public static void FriendlyFire_AlwaysZero(int? iterations = null)
    {
        Gen.Float[1f, 100f].Sample(damage =>
        {
            var result = Test.FriendlyFire(damage).Execute();
            Xunit.Assert.Equal(0f, result.DamageDealt);
        }, iterations ?? DefaultIterations);
    }

    /// <summary>
    /// Property: Stealth attack on aware target doesn't get bonus.
    /// </summary>
    public static void StealthOnAware_NoBonus(int? iterations = null)
    {
        Gen.Float[1f, 100f].Sample(damage =>
        {
            var player = TestData.Player();
            var awareEnemy = TestData.AwareEnemy();
            var scenario = Test.Hit(player, awareEnemy, damage).AsStealth();
            var result = scenario.Execute();

            // Should be base damage only, no stealth bonus
            Xunit.Assert.Equal(damage, result.DamageDealt, 2);
        }, iterations ?? DefaultIterations);
    }

    /// <summary>
    /// Property: Multi-hit damage scales linearly.
    /// </summary>
    public static void MultiHit_ScalesLinearly(int? iterations = null)
    {
        (from damage in Gen.Float[1f, 10f]
         from hits in Gen.Int[1, 10]
         select (damage, hits))
        .Sample(p =>
        {
            var result = Test.MultiHit(p.damage, p.hits).Execute();
            var expected = p.damage * p.hits;
            Xunit.Assert.Equal(expected, result.DamageDealt, 2);
        }, iterations ?? DefaultIterations);
    }

    /// <summary>
    /// Property: Crit damage is higher than base damage.
    /// </summary>
    public static void Crit_IncreasesDamage(int? iterations = null)
    {
        (from damage in Gen.Float[1f, 100f]
         from multiplier in Gen.Float[1.5f, 5f]
         select (damage, multiplier))
        .Sample(p =>
        {
            var normalResult = Test.Hit(p.damage).Execute();
            var critResult = Test.Crit(p.damage, p.multiplier).Execute();

            // Crit should deal more damage (or equal if multiplier is 1)
            Xunit.Assert.True(critResult.DamageDealt >= normalResult.DamageDealt,
                $"Crit ({critResult.DamageDealt}) should >= normal ({normalResult.DamageDealt})");
        }, iterations ?? DefaultIterations);
    }

    /// <summary>
    /// Property: Overkill doesn't cause negative health.
    /// </summary>
    public static void Overkill_NoNegativeHealth(int? iterations = null)
    {
        (from targetHealth in Gen.Float[1f, 100f]
         from damage in Gen.Float[100f, 1000f] // Damage much higher than health
         select (targetHealth, damage))
        .Sample(p =>
        {
            var target = TestData.Enemy(p.targetHealth);
            var scenario = Test.Hit(TestData.Player(), target, p.damage);
            var result = scenario.Execute();

            Xunit.Assert.Equal(0f, result.TargetHealthAfter);
        }, iterations ?? DefaultIterations);
    }

    /// <summary>
    /// Runs all built-in property tests.
    /// </summary>
    public static void RunAll(int? iterations = null)
    {
        Damage_NeverNegative(iterations);
        Health_NeverBelowZero(iterations);
        FriendlyFire_AlwaysZero(iterations);
        StealthOnAware_NoBonus(iterations);
        MultiHit_ScalesLinearly(iterations);
        Crit_IncreasesDamage(iterations);
        Overkill_NoNegativeHealth(iterations);
    }
}

/// <summary>
/// Generated damage scenario for property testing.
/// </summary>
public readonly record struct DamageScenarioGen(
    float BaseDamage,
    float AttackerHealth,
    float TargetHealth,
    float CritChance,
    float CritMultiplier,
    int HitCount,
    bool IsStealth,
    double RandomValue
)
{
    /// <summary>
    /// Executes this scenario and returns the result.
    /// </summary>
    public DamageResult Execute()
    {
        var attacker = TestData.Player(AttackerHealth, CritChance, CritMultiplier);
        var target = TestData.Enemy(TargetHealth);

        var system = TestData.DamageSystem(RandomValue);
        var hit = new HitContext(attacker, target, BaseDamage, HitCount, IsStealth);

        float damage = system.ProcessHit(hit);
        return new DamageResult(damage, target.Health.CurrentHealth, target.Health.MaxHealth);
    }
}
