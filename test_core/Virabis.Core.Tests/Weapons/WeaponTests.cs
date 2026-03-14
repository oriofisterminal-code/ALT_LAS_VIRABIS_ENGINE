using System.Collections.Generic;
using Virabis.Core.Weapons;
using Xunit;

namespace Virabis.Core.Tests.Weapons;

public class WeaponTests
{
    [Fact]
    public void CanFire_WithinCooldown_ReturnsFalse()
    {
        // Arrange
        var state = new WeaponState(fireRate: 2f);  // 2 shots per second = 0.5s cooldown
        float currentTime = 0f;

        // Act
        state.RecordShot(currentTime);
        bool canFire = state.CanFire(currentTime + 0.2f);  // 0.2s later

        // Assert
        Assert.False(canFire);
    }

    [Fact]
    public void CanFire_AfterCooldown_ReturnsTrue()
    {
        // Arrange
        var state = new WeaponState(fireRate: 2f);  // 0.5s cooldown
        float currentTime = 0f;

        // Act
        state.RecordShot(currentTime);
        bool canFire = state.CanFire(currentTime + 0.6f);  // 0.6s later

        // Assert
        Assert.True(canFire);
    }

    [Fact]
    public void Fire_WithinCooldown_ReturnsFalse()
    {
        // Arrange
        var config = new WeaponConfig
        {
            Damage = 10f,
            FireRate = 2f,
            Range = 100f,
            HitCount = 1
        };
        var damageSystem = new DamageSystem(new TestRandomProvider());
        var weapon = new Weapon(config, damageSystem);

        var attacker = new Entity(new TeamId(1), maxHealth: 100f);
        var target = new Entity(new TeamId(2), maxHealth: 50f);
        
        var targets = new List<Entity> { target };

        // Act
        float firstShot = weapon.Fire(0.016f, attacker, default, default, targets);
        float secondShot = weapon.Fire(0.1f, attacker, default, default, targets);  // 0.116s total

        // Assert
        Assert.True(firstShot > 0);  // First shot deals damage
        Assert.Equal(0f, secondShot);  // Still in cooldown
    }

    [Fact]
    public void Fire_AfterCooldown_ReturnsTrue()
    {
        // Arrange
        var config = new WeaponConfig
        {
            Damage = 10f,
            FireRate = 2f,
            Range = 100f,
            HitCount = 1
        };
        var damageSystem = new DamageSystem(new TestRandomProvider());
        var weapon = new Weapon(config, damageSystem);

        var attacker = new Entity(new TeamId(1), maxHealth: 100f);
        var target = new Entity(new TeamId(2), maxHealth: 50f);
        
        var targets = new List<Entity> { target };

        // Act
        float firstShot = weapon.Fire(0.016f, attacker, default, default, targets);
        float secondShot = weapon.Fire(0.6f, attacker, default, default, targets);  // 0.616s total

        // Assert
        Assert.True(firstShot > 0);  // First shot deals damage
        Assert.True(secondShot > 0);  // Cooldown expired, second shot deals damage
    }

    [Fact]
    public void Fire_RespectHitCount_LimitsDamageTargets()
    {
        // Arrange
        var config = new WeaponConfig
        {
            Damage = 10f,
            FireRate = 2f,
            Range = 100f,
            HitCount = 2  // Max 2 targets
        };
        var damageSystem = new DamageSystem(new TestRandomProvider());
        var weapon = new Weapon(config, damageSystem);

        var attacker = new Entity(new TeamId(1), maxHealth: 100f);
        var target1 = new Entity(new TeamId(2), maxHealth: 50f);
        var target2 = new Entity(new TeamId(2), maxHealth: 50f);
        var target3 = new Entity(new TeamId(2), maxHealth: 50f);
        
        var targets = new List<Entity> { target1, target2, target3 };

        // Act
        float totalDamage = weapon.Fire(0.016f, attacker, default, default, targets);

        // Assert
        Assert.True(totalDamage > 0);  // Damage applied
        // Only first 2 targets should take damage (HitCount = 2)
        Assert.True(target1.Health.CurrentHealth < 50f);
        Assert.True(target2.Health.CurrentHealth < 50f);
        Assert.Equal(50f, target3.Health.CurrentHealth);  // Not hit
    }
}

/// <summary>
/// Test random provider for deterministic testing
/// </summary>
public class TestRandomProvider : IRandomProvider
{
    public double NextDouble() => 0.0;  // Always return 0 (no crit)
}
