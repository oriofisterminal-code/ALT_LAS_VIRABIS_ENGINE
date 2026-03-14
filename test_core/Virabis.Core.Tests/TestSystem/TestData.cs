using Virabis.Core;

namespace Virabis.Core.Tests.TestSystem;

/// <summary>
/// Factory for creating test data with sensible defaults.
/// "Tarif kadar basit olmalı" - Uzm. Elif Reçete (Aşçı)
///
/// Usage:
/// <code>
/// var player = TestData.Player();
/// var enemy = TestData.Enemy(health: 50);
/// var boss = TestData.Boss();
/// var hit = TestData.Hit(player, enemy, damage: 10);
/// </code>
/// </summary>
public static class TestData
{
    // =========================================================================
    // ENTITY FACTORIES
    // =========================================================================

    /// <summary>
    /// Creates a Player entity with default health 100.
    /// </summary>
    public static Entity Player(float health = 100f, float critChance = 0f, float critMultiplier = 2f)
    {
        var entity = new Entity(TeamId.Player, maxHealth: health);
        entity.Stats.CritChance = critChance;
        entity.Stats.CritMultiplier = critMultiplier;
        return entity;
    }

    /// <summary>
    /// Creates a Player with guaranteed crit (100% chance).
    /// </summary>
    public static Entity PlayerWithCrit(float health = 100f, float critMultiplier = 2f)
        => Player(health, critChance: 1f, critMultiplier);

    /// <summary>
    /// Creates an Enemy entity with default health 100.
    /// </summary>
    public static Entity Enemy(float health = 100f)
        => new(TeamId.Enemy, maxHealth: health);

    /// <summary>
    /// Creates an Enemy that is "aware" (no stealth bonus against).
    /// </summary>
    public static Entity AwareEnemy(float health = 100f)
    {
        var enemy = new Entity(TeamId.Enemy, maxHealth: health);
        enemy.Tags.AddTag("aware");
        return enemy;
    }

    /// <summary>
    /// Creates a Boss enemy with high health and always aware.
    /// </summary>
    public static Entity Boss(float health = 500f)
    {
        var boss = new Entity(TeamId.Enemy, maxHealth: health);
        boss.Tags.AddTag("boss");
        boss.Tags.AddTag("aware");
        return boss;
    }

    /// <summary>
    /// Creates an Entity with custom team and health.
    /// </summary>
    public static Entity Entity(TeamId team, float health = 100f)
        => new(team, maxHealth: health);

    // =========================================================================
    // HIT CONTEXT FACTORIES
    // =========================================================================

    /// <summary>
    /// Creates a basic HitContext. Defaults: Player attacks Enemy, base damage 10.
    /// </summary>
    public static HitContext Hit(
        Entity? attacker = null,
        Entity? target = null,
        float damage = 10f,
        int hitCount = 1,
        bool stealth = false)
    {
        attacker ??= Player();
        target ??= Enemy();
        return new HitContext(attacker, target, damage, hitCount, stealth);
    }

    /// <summary>
    /// Creates a HitContext for Player attacking Enemy.
    /// </summary>
    public static HitContext PlayerHitsEnemy(float damage = 10f, int hitCount = 1)
        => Hit(attacker: Player(), target: Enemy(), damage, hitCount);

    /// <summary>
    /// Creates a HitContext for stealth attack (Player vs unaware Enemy).
    /// </summary>
    public static HitContext StealthHit(float damage = 10f)
        => Hit(attacker: Player(), target: Enemy(), damage, stealth: true);

    /// <summary>
    /// Creates a HitContext for friendly fire (Player vs Player).
    /// </summary>
    public static HitContext FriendlyFire(float damage = 50f)
        => Hit(attacker: Player(), target: Player(), damage);

    /// <summary>
    /// Creates a HitContext for multi-hit attack (e.g., shotgun pellets).
    /// </summary>
    public static HitContext MultiHit(float damagePerHit = 5f, int hitCount = 8)
        => Hit(attacker: Player(), target: Enemy(), damagePerHit, hitCount);

    // =========================================================================
    // DAMAGE SYSTEM FACTORIES
    // =========================================================================

    /// <summary>
    /// Creates a DamageSystem with deterministic random (no crit by default).
    /// </summary>
    public static DamageSystem DamageSystem(double randomValue = 0.5)
        => new(new DeterministicRandom(randomValue));

    /// <summary>
    /// Creates a DamageSystem with guaranteed crit.
    /// </summary>
    public static DamageSystem DamageSystemWithCrit()
        => new(new DeterministicRandom(0.99)); // High value triggers crit

    /// <summary>
    /// Creates a DamageSystem with system random (non-deterministic).
    /// </summary>
    public static DamageSystem DamageSystemRandom()
        => new(new SystemRandomProvider());

    // =========================================================================
    // SCENARIO BUILDERS (Quick one-liners)
    // =========================================================================

    /// <summary>
    /// Quick damage test: Player hits Enemy with base damage.
    /// Returns the actual damage dealt.
    /// </summary>
    public static float QuickDamage(float baseDamage = 10f)
    {
        var system = DamageSystem();
        var hit = PlayerHitsEnemy(baseDamage);
        return system.ProcessHit(hit);
    }

    /// <summary>
    /// Quick crit test: Player with 100% crit hits Enemy.
    /// </summary>
    public static float QuickCritDamage(float baseDamage = 10f, float critMultiplier = 2f)
    {
        var system = DamageSystemWithCrit();
        var player = PlayerWithCrit(critMultiplier: critMultiplier);
        var hit = Hit(attacker: player, target: Enemy(), damage: baseDamage);
        return system.ProcessHit(hit);
    }

    /// <summary>
    /// Quick stealth test: Player stealth attacks unaware Enemy.
    /// </summary>
    public static float QuickStealthDamage(float baseDamage = 10f)
    {
        var system = DamageSystem();
        var hit = StealthHit(baseDamage);
        return system.ProcessHit(hit);
    }

    /// <summary>
    /// Quick friendly fire test: Player attacks Player.
    /// </summary>
    public static float QuickFriendlyFire(float baseDamage = 50f)
    {
        var system = DamageSystem();
        var hit = FriendlyFire(baseDamage);
        return system.ProcessHit(hit);
    }
}
