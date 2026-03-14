using Xunit;
using Virabis.Core.Tests.TestSystem;

namespace Virabis.Core.Tests;

/// <summary>
/// Integration tests using the new TestSystem.
/// More realistic combat scenarios with cleaner code.
///
/// "Büyük resmi gör, detaylarda boğulma" - Dr. Fatma Göz (Fotoğrafçı)
/// </summary>
public class IntegrationTests
{
    // =========================================================================
    // COMBAT SCENARIOS
    // =========================================================================

    [Fact, Trait("Category", "Integration")]
    public void Combat_ShotgunCritKill()
    {
        // Player with 80% crit, 2.5x multiplier
        var player = TestData.Player(health: 100, critChance: 0.8f, critMultiplier: 2.5f);
        var enemy = TestData.Enemy(health: 50);

        // Shotgun: 8 pellets * 5 damage each
        var result = Test.Damage()
            .AttackerIs(player)
            .TargetIs(enemy)
            .WithDamage(5)
            .WithHitCount(8)
            .WithRandom(0.5) // May or may not crit
            .Execute();

        // Base: 40, With crit: 100
        Assert.True(result.DamageDealt >= 40f);
        Assert.True(enemy.Health.CurrentHealth < 50f);
    }

    [Fact, Trait("Category", "Integration")]
    public void Combat_StealthAssassination()
    {
        // Assassin with 100% crit, 3x multiplier
        var assassin = TestData.PlayerWithCrit(health: 80, critMultiplier: 3f);
        var guard = TestData.Enemy(health: 100);

        // Stealth + Crit: 15 * 2 * 3 = 90
        Test.Damage()
            .AttackerIs(assassin)
            .TargetIs(guard)
            .WithDamage(15)
            .AsStealth()
            .WithCrit(multiplier: 3f)
            .ShouldDeal(90);

        Assert.Equal(10f, guard.Health.CurrentHealth, precision: 2);
        Assert.True(guard.Health.IsAlive, "Guard barely survives");
    }

    [Fact, Trait("Category", "Integration")]
    public void Combat_AlertedGuard_NoStealthBonus()
    {
        var player = TestData.Player();
        var alertGuard = TestData.AwareEnemy(health: 100);

        // Stealth fails on aware target
        Test.Hit(player, alertGuard, 20).AsStealth().ShouldDeal(20);
        Assert.Equal(80f, alertGuard.Health.CurrentHealth, precision: 2);
    }

    [Fact, Trait("Category", "Integration")]
    public void Combat_TeamFight_NoFriendlyFire()
    {
        var player1 = TestData.Player();
        var player2 = TestData.Player();
        var enemy1 = TestData.Enemy();
        var enemy2 = TestData.Enemy();

        // Friendly fire: 0 damage
        Test.Hit(player1, player2, 50).ShouldDealZero();
        Assert.Equal(100f, player2.Health.CurrentHealth);

        // Valid hit: normal damage
        Test.Hit(player1, enemy1, 50).ShouldDeal(50);
        Assert.Equal(50f, enemy1.Health.CurrentHealth, precision: 2);
    }

    [Fact, Trait("Category", "Integration")]
    public void Combat_BossEncounter()
    {
        var player = TestData.Player();
        var boss = TestData.Boss(health: 500);

        // 5 hits of 25 damage = 125 total
        float totalDamage = 0;
        for (int i = 0; i < 5; i++)
        {
            totalDamage += Test.Hit(player, boss, 25).Execute().DamageDealt;
        }

        Assert.Equal(125f, totalDamage, precision: 2);
        Assert.Equal(375f, boss.Health.CurrentHealth, precision: 2);
        Assert.True(boss.Health.IsAlive);
        Assert.True(boss.Tags.HasTag("boss"));
    }

    [Fact, Trait("Category", "Integration")]
    public void Combat_HealthValidation()
    {
        var entity = TestData.Player(health: 100);

        entity.Health.ApplyDamage(50f);
        entity.Health.Heal(100f); // Try overheal

        Assert.Equal(100f, entity.Health.CurrentHealth); // Clamped to max
    }

    [Fact, Trait("Category", "Integration")]
    public void Combat_Death_NoNegativeHealth()
    {
        var player = TestData.Player(health: 50);
        var enemy = TestData.Enemy();

        // Overkill: 200 damage to 50 HP
        Test.Hit(enemy, player, 200).Execute();

        Assert.Equal(0f, player.Health.CurrentHealth);
        Assert.False(player.Health.IsAlive);
    }

    // =========================================================================
    // COMPLEX SCENARIOS (Using TestSystem for cleaner code)
    // =========================================================================

    [Fact, Trait("Category", "Integration")]
    public void Complex_MultiRoundCombat()
    {
        var player = TestData.Player(health: 100);
        var enemy = TestData.Enemy(health: 150);

        // Round 1: Normal hit
        Test.Hit(player, enemy, 30).Execute();
        Assert.Equal(120f, enemy.Health.CurrentHealth, precision: 2);

        // Round 2: Critical hit
        Test.Damage()
            .AttackerIs(player)
            .TargetIs(enemy)
            .WithDamage(40)
            .WithCrit(multiplier: 2f)
            .Execute();
        Assert.Equal(40f, enemy.Health.CurrentHealth, precision: 2);

        // Round 3: Finish off
        Test.Hit(player, enemy, 50).Execute();
        Assert.False(enemy.Health.IsAlive);
    }

    [Fact, Trait("Category", "Integration")]
    public void Complex_ShotgunVsBoss()
    {
        var player = TestData.PlayerWithCrit(critMultiplier: 2f);
        var boss = TestData.Boss(health: 1000);

        // 10 shotgun blasts (8 pellets each, 5 damage per pellet)
        for (int blast = 0; blast < 10; blast++)
        {
            var result = Test.MultiHit(5, 8).Execute();
            // Each blast: 40 damage (or 80 with crit)
        }

        // Boss should be damaged but alive
        Assert.True(boss.Health.CurrentHealth < 1000f);
        Assert.True(boss.Health.IsAlive);
        Assert.True(boss.Tags.HasTag("boss"));
    }

    [Fact, Trait("Category", "Integration")]
    public void Complex_StealthChain()
    {
        // Chain of stealth kills
        var assassin = TestData.PlayerWithCrit(critMultiplier: 3f);
        var guards = new[]
        {
            TestData.Enemy(health: 90),  // 15 * 2 * 3 = 90 (one-shot)
            TestData.Enemy(health: 90),
            TestData.Enemy(health: 90)
        };

        foreach (var guard in guards)
        {
            Test.Damage()
                .AttackerIs(assassin)
                .TargetIs(guard)
                .WithDamage(15)
                .AsStealth()
                .WithCrit(multiplier: 3f)
                .ShouldKill();
        }

        // All guards dead
        Assert.All(guards, g => Assert.False(g.Health.IsAlive));
    }
}
