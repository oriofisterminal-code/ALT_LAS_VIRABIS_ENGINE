using Xunit;
using Virabis.Core.Tests.TestSystem;

namespace Virabis.Core.Tests;

/// <summary>
/// Damage system tests using the new TestSystem.
///
/// COMPARISON:
/// OLD: ~200 lines with repetitive Arrange-Act-Assert patterns
/// NEW: ~80 lines with fluent test system
///
/// "Kesip atmakta ustayım, fazlalık kalmaz" - Dr. Mehmet Ameliyat (Berber)
/// </summary>
public class DamageSystemTests
{
    // =========================================================================
    // BASIC DAMAGE TESTS (One-liners)
    // =========================================================================

    [Fact, Trait("Category", "Smoke")]
    public void BasicDamage() => Test.Hit(10).ShouldDeal(10);

    [Fact, Trait("Category", "Smoke")]
    public void BasicDamage_LeavesCorrectHealth() =>
        Test.Hit(10).ShouldLeaveHealth(90);

    [Fact, Trait("Category", "Unit")]
    public void MultiHit_ScalesLinearly() =>
        Test.MultiHit(damagePerHit: 5, hitCount: 8).ShouldDeal(40);

    [Fact, Trait("Category", "Unit")]
    public void MultiHit_LeavesCorrectHealth() =>
        Test.MultiHit(5, 8).ShouldLeaveHealth(60);

    // =========================================================================
    // STEALTH ATTACK TESTS
    // =========================================================================

    [Fact, Trait("Category", "Unit")]
    public void StealthAttack_DoublesDamage() =>
        Test.Stealth(10).ShouldDeal(20);

    [Fact, Trait("Category", "Unit")]
    public void StealthAttack_AwareTarget_NoBonus()
    {
        var awareEnemy = TestData.AwareEnemy();
        Test.Hit(TestData.Player(), awareEnemy, 10).AsStealth().ShouldDeal(10);
    }

    // =========================================================================
    // CRITICAL HIT TESTS
    // =========================================================================

    [Fact, Trait("Category", "Unit")]
    public void Crit_DoublesDamage() =>
        Test.Crit(10, multiplier: 2f).ShouldDeal(20);

    [Fact, Trait("Category", "Unit")]
    public void Crit_TripleMultiplier() =>
        Test.Crit(10, multiplier: 3f).ShouldDeal(30);

    [Fact, Trait("Category", "Unit")]
    public void NoCrit_WhenChanceZero()
    {
        var player = TestData.Player(critChance: 0f, critMultiplier: 3f);
        Test.Hit(player, TestData.Enemy(), 10).ShouldDeal(10);
    }

    // =========================================================================
    // FRIENDLY FIRE TESTS
    // =========================================================================

    [Fact, Trait("Category", "Unit")]
    public void FriendlyFire_DealsZeroDamage() =>
        Test.FriendlyFire(50).ShouldDealZero();

    [Fact, Trait("Category", "Unit")]
    public void FriendlyFire_TargetUnharmed() =>
        Test.FriendlyFire(50).ShouldLeaveHealth(100);

    // =========================================================================
    // FLUENT BUILDER TESTS (More complex scenarios)
    // =========================================================================

    [Fact, Trait("Category", "Unit")]
    public void Fluent_PlayerHitsEnemy_WithCrit()
    {
        var player = TestData.PlayerWithCrit(critMultiplier: 2.5f);
        var enemy = TestData.Enemy(health: 50);

        Test.Damage()
            .AttackerIs(player)
            .TargetIs(enemy)
            .WithDamage(10)
            .WithCrit(multiplier: 2.5f)
            .ShouldDeal(25);
    }

    [Fact, Trait("Category", "Unit")]
    public void Fluent_StealthCrit_Combined()
    {
        // Stealth (2x) + Crit (3x) = 15 * 2 * 3 = 90
        Test.Damage()
            .WithDamage(15)
            .AsStealth()
            .WithCrit(multiplier: 3f)
            .ShouldDeal(90);
    }

    [Fact, Trait("Category", "Integration")]
    public void Integration_BossEncounter_MultipleHits()
    {
        var player = TestData.Player();
        var boss = TestData.Boss(health: 500);

        // Hit boss 5 times with 25 damage each = 125 total
        float totalDamage = 0;
        for (int i = 0; i < 5; i++)
        {
            var scenario = Test.Hit(player, boss, 25);
            totalDamage += scenario.Execute().DamageDealt;
        }

        Assert.Equal(125f, totalDamage, precision: 2);
        Assert.Equal(375f, boss.Health.CurrentHealth, precision: 2);
        Assert.True(boss.Health.IsAlive);
        Assert.True(boss.Tags.HasTag("boss"));
    }

    // =========================================================================
    // DEATH & OVERKILL TESTS
    // =========================================================================

    [Fact, Trait("Category", "Unit")]
    public void Overkill_HealthNotNegative()
    {
        var weakEnemy = TestData.Enemy(health: 50);
        Test.Hit(TestData.Player(), weakEnemy, 200).ShouldKill();
        Assert.Equal(0f, weakEnemy.Health.CurrentHealth);
    }

    [Fact, Trait("Category", "Unit")]
    public void ExactKill_HealthZero()
    {
        var enemy = TestData.Enemy(health: 100);
        Test.Hit(TestData.Player(), enemy, 100).ShouldKill();
        Assert.Equal(0f, enemy.Health.CurrentHealth);
    }

    // =========================================================================
    // PROPERTY-BASED TESTS (Using TestSystem)
    // =========================================================================

    [Fact, Trait("Category", "Property")]
    public void Property_DamageNeverNegative() =>
        Property.Damage_NeverNegative();

    [Fact, Trait("Category", "Property")]
    public void Property_HealthNeverBelowZero() =>
        Property.Health_NeverBelowZero();

    [Fact, Trait("Category", "Property")]
    public void Property_FriendlyFireAlwaysZero() =>
        Property.FriendlyFire_AlwaysZero();

    [Fact, Trait("Category", "Property")]
    public void Property_MultiHitScalesLinearly() =>
        Property.MultiHit_ScalesLinearly();

    [Fact, Trait("Category", "Property")]
    public void Property_CritIncreasesDamage() =>
        Property.Crit_IncreasesDamage();

    // =========================================================================
    // COMPONENT TESTS (Kept from original - still relevant)
    // =========================================================================

    [Fact, Trait("Category", "Unit")]
    public void TagComponent_CaseInsensitive()
    {
        var tags = new TagComponent();
        tags.AddTag("Aware");

        Assert.True(tags.HasTag("aware"));
        Assert.True(tags.HasTag("AWARE"));
        Assert.True(tags.HasTag("AwArE"));
    }

    [Fact, Trait("Category", "Unit")]
    public void HealthComponent_InvalidMaxHealth_Throws()
    {
        Assert.Throws<ArgumentException>(() => new HealthComponent(0));
        Assert.Throws<ArgumentException>(() => new HealthComponent(-10));
    }

    [Fact, Trait("Category", "Unit")]
    public void TeamId_Equality()
    {
        var team1 = new TeamId(1);
        var team2 = new TeamId(1);
        var team3 = new TeamId(2);

        Assert.Equal(team1, team2);
        Assert.NotEqual(team1, team3);
        Assert.Equal(TeamId.Player, new TeamId(0));
    }
}
