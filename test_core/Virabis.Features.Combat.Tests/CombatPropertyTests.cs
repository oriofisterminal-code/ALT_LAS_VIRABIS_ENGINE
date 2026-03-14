using Xunit;
using CsCheck;
using Virabis.Core;
using Virabis.Features.Combat;

namespace Virabis.Features.Combat.Tests;

/// <summary>
/// Property-based tests for Combat system using CsCheck
/// Validates combat invariants across the input space
/// </summary>
public class CombatPropertyTests
{
    /// <summary>
    /// Mock random provider for deterministic testing
    /// </summary>
    private class MockRandomProvider : IRandomProvider
    {
        private readonly double _value;

        public MockRandomProvider(double value = 0.0)
        {
            _value = value;
        }

        public double NextDouble() => _value;
    }

    /// <summary>
    /// **Validates: Requirements 2.6, 2.7, 2.8**
    /// Property: Damage calculation always produces non-negative values
    /// For any valid base damage and reduction percentage, final damage must be >= 0
    /// </summary>
    [Fact]
    public void CombatSystem_DamageCalculation_AlwaysNonNegative()
    {
        // Use tuple generator for efficiency (avoids nested Sample calls)
        Gen.Select(Gen.Float[0f, 1000f], Gen.Int[1, 10], Gen.Float[0f, 1f], Gen.Float[1f, 5f])
            .Sample(t =>
            {
                var (baseDamage, hitCount, critChance, critMultiplier) = t;
                
                // Test with no crit (deterministic)
                var damageSystem = new DamageSystem(new MockRandomProvider(0.0));
                var attacker = new Entity(TeamId.Player, maxHealth: 100);
                attacker.Stats.CritChance = critChance;
                attacker.Stats.CritMultiplier = critMultiplier;

                var target = new Entity(TeamId.Enemy, maxHealth: 100);
                var context = new HitContext(attacker, target, baseDamage, hitCount);

                // Act
                float damage = damageSystem.ProcessHit(context);

                // Invariant: Damage must always be non-negative
                Assert.True(damage >= 0f,
                    $"Damage {damage} is negative for baseDamage={baseDamage}, hitCount={hitCount}");

                // Additional invariant: Target health should never be negative
                Assert.True(target.Health.CurrentHealth >= 0f,
                    $"Target health {target.Health.CurrentHealth} is negative");

                // Test with guaranteed crit
                var damageSystemCrit = new DamageSystem(new MockRandomProvider(0.0));
                var attackerCrit = new Entity(TeamId.Player, maxHealth: 100);
                attackerCrit.Stats.CritChance = 1.0f; // Guaranteed crit
                attackerCrit.Stats.CritMultiplier = critMultiplier;

                var targetCrit = new Entity(TeamId.Enemy, maxHealth: 100);
                var contextCrit = new HitContext(attackerCrit, targetCrit, baseDamage, hitCount);

                float damageCrit = damageSystemCrit.ProcessHit(contextCrit);

                // Invariant: Crit damage must also be non-negative
                Assert.True(damageCrit >= 0f,
                    $"Crit damage {damageCrit} is negative");
            }, iter: 10); // Fast execution for development
    }

    /// <summary>
    /// **Validates: Requirements 2.6, 2.7, 2.8**
    /// Property: Damage amplification (crit multiplier) and probability (crit chance) are always in valid ranges
    /// CritChance should be clamped to [0, 1] and CritMultiplier should be >= 1
    /// This validates the "damage modification bounds" invariant
    /// </summary>
    [Fact]
    public void CombatSystem_DamageModificationBounds_AlwaysInValidRange()
    {
        // Use tuple generator for efficiency
        Gen.Select(Gen.Float[-1f, 2f], Gen.Float[-2f, 10f])
            .Sample(t =>
            {
                var (rawCritChance, rawCritMultiplier) = t;
                
                // Create StatComponent which should clamp values
                var stats = new StatComponent(rawCritChance, rawCritMultiplier);

                // Invariant: CritChance must be in [0, 1] range
                Assert.True(stats.CritChance >= 0f && stats.CritChance <= 1f,
                    $"CritChance {stats.CritChance} is outside valid range [0, 1] for input {rawCritChance}");

                // Invariant: CritMultiplier must be >= 1 (can't reduce damage via crit)
                Assert.True(stats.CritMultiplier >= 1f,
                    $"CritMultiplier {stats.CritMultiplier} is less than 1 for input {rawCritMultiplier}");

                // Test in actual combat scenario
                var damageSystem = new DamageSystem(new MockRandomProvider(0.0));
                var attacker = new Entity(TeamId.Player, maxHealth: 100);
                attacker.Stats.CritChance = stats.CritChance;
                attacker.Stats.CritMultiplier = stats.CritMultiplier;

                var target = new Entity(TeamId.Enemy, maxHealth: 100);
                var context = new HitContext(attacker, target, baseDamage: 10f);

                float damage = damageSystem.ProcessHit(context);

                // Invariant: Damage with valid stats should be reasonable
                Assert.True(damage >= 0f && damage <= 10f * stats.CritMultiplier,
                    $"Damage {damage} is outside expected range for baseDamage=10, critMultiplier={stats.CritMultiplier}");
            }, iter: 10);
    }

    /// <summary>
    /// **Validates: Requirements 2.6, 2.7, 2.8**
    /// Property: Health never exceeds maximum after healing
    /// For any current health, healing amount, and max health, the result must be <= maxHealth
    /// </summary>
    [Fact]
    public void CombatSystem_HealthAfterHealing_NeverExceedsMaximum()
    {
        // Use tuple generator for efficiency
        Gen.Select(Gen.Float[50f, 200f], Gen.Float[0f, 1f], Gen.Float[0f, 100f])
            .Sample(t =>
            {
                var (maxHealth, currentHealthPercent, healingAmount) = t;
                
                // Create entity with specific health state
                var entity = new Entity(TeamId.Player, maxHealth);
                
                // Set current health to a percentage of max
                float targetCurrentHealth = maxHealth * currentHealthPercent;
                float damageToApply = maxHealth - targetCurrentHealth;
                if (damageToApply > 0)
                {
                    entity.Health.ApplyDamage(damageToApply);
                }

                // Verify setup
                float actualCurrentHealth = entity.Health.CurrentHealth;
                Assert.True(actualCurrentHealth >= 0f && actualCurrentHealth <= maxHealth,
                    $"Setup failed: current health {actualCurrentHealth} outside [0, {maxHealth}]");

                // Act: Apply healing
                entity.Health.Heal(healingAmount);

                // Invariant: Health must never exceed maximum
                Assert.True(entity.Health.CurrentHealth <= maxHealth,
                    $"Health {entity.Health.CurrentHealth} exceeds maximum {maxHealth} after healing {healingAmount}");

                // Invariant: Health must remain non-negative
                Assert.True(entity.Health.CurrentHealth >= 0f,
                    $"Health {entity.Health.CurrentHealth} is negative after healing");

                // Invariant: Health should increase or stay the same (healing never damages)
                Assert.True(entity.Health.CurrentHealth >= actualCurrentHealth,
                    $"Health decreased from {actualCurrentHealth} to {entity.Health.CurrentHealth} after healing");
            }, iter: 10);
    }
}
