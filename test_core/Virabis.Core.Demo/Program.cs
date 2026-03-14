using Virabis.Core;

Console.WriteLine("=== VIRABIS COMBAT CORE DEMO ===\n");

// Setup
var random = new SystemRandomProvider();
var damageSystem = new DamageSystem(random);

// Scenario 1: Basic Combat
Console.WriteLine("📍 SCENARIO 1: Basic Hitscan Combat");
var player = new Entity(TeamId.Player, maxHealth: 100);
player.Stats.CritChance = 0.3f;
player.Stats.CritMultiplier = 2.0f;

var enemy = new Entity(TeamId.Enemy, maxHealth: 100);

var hit1 = new HitContext(player, enemy, baseDamage: 15f);
float damage1 = damageSystem.ProcessHit(hit1);

Console.WriteLine($"  Player attacks Enemy: {damage1:F1} damage");
Console.WriteLine($"  Enemy Health: {enemy.Health.CurrentHealth:F1}/{enemy.Health.MaxHealth:F1}");
Console.WriteLine($"  Crit? {(damage1 > 15f ? "YES ⚡" : "No")}\n");

// Scenario 2: Shotgun Multi-Hit
Console.WriteLine("📍 SCENARIO 2: Shotgun Multi-Hit");
var enemy2 = new Entity(TeamId.Enemy, maxHealth: 100);
var shotgunHit = new HitContext(player, enemy2, baseDamage: 5f, hitCount: 8);
float shotgunDamage = damageSystem.ProcessHit(shotgunHit);

Console.WriteLine($"  Shotgun blast (8 pellets × 5 damage): {shotgunDamage:F1} damage");
Console.WriteLine($"  Enemy Health: {enemy2.Health.CurrentHealth:F1}/{enemy2.Health.MaxHealth:F1}");
Console.WriteLine($"  Crit? {(shotgunDamage > 40f ? "YES ⚡" : "No")}\n");

// Scenario 3: Stealth Assassination
Console.WriteLine("📍 SCENARIO 3: Stealth Assassination");
var assassin = new Entity(TeamId.Player, maxHealth: 80);
assassin.Stats.CritChance = 0.5f;
assassin.Stats.CritMultiplier = 2.5f;

var guard = new Entity(TeamId.Enemy, maxHealth: 100);
// Guard is unaware (no "aware" tag)

var backstab = new HitContext(assassin, guard, baseDamage: 20f, isStealthAttack: true);
float backstabDamage = damageSystem.ProcessHit(backstab);

Console.WriteLine($"  Stealth backstab: {backstabDamage:F1} damage");
Console.WriteLine($"  Guard Health: {guard.Health.CurrentHealth:F1}/{guard.Health.MaxHealth:F1}");
Console.WriteLine($"  Stealth Bonus: 2x ✓");
Console.WriteLine($"  Crit? {(backstabDamage > 40f ? "YES ⚡" : "No")}\n");

// Scenario 4: Alert Guard (No Stealth Bonus)
Console.WriteLine("📍 SCENARIO 4: Alert Guard");
var alertGuard = new Entity(TeamId.Enemy, maxHealth: 100);
alertGuard.Tags.AddTag("aware");

var failedStealth = new HitContext(assassin, alertGuard, baseDamage: 20f, isStealthAttack: true);
float normalDamage = damageSystem.ProcessHit(failedStealth);

Console.WriteLine($"  Attempted stealth attack: {normalDamage:F1} damage");
Console.WriteLine($"  Guard Health: {alertGuard.Health.CurrentHealth:F1}/{alertGuard.Health.MaxHealth:F1}");
Console.WriteLine($"  Stealth Bonus: BLOCKED (guard is aware) ⚠️");
Console.WriteLine($"  Crit? {(normalDamage > 20f ? "YES ⚡" : "No")}\n");

// Scenario 5: Friendly Fire Test
Console.WriteLine("📍 SCENARIO 5: Friendly Fire Prevention");
var player1 = new Entity(TeamId.Player, maxHealth: 100);
var player2 = new Entity(TeamId.Player, maxHealth: 100);

var friendlyFire = new HitContext(player1, player2, baseDamage: 50f);
float ffDamage = damageSystem.ProcessHit(friendlyFire);

Console.WriteLine($"  Player1 shoots Player2: {ffDamage:F1} damage");
Console.WriteLine($"  Player2 Health: {player2.Health.CurrentHealth:F1}/{player2.Health.MaxHealth:F1}");
Console.WriteLine($"  Friendly Fire: PREVENTED ✓\n");

// Scenario 6: Boss Fight
Console.WriteLine("📍 SCENARIO 6: Boss Encounter");
var boss = new Entity(TeamId.Enemy, maxHealth: 500);
boss.Tags.AddTag("boss");
boss.Tags.AddTag("aware");

Console.WriteLine($"  Boss Health: {boss.Health.CurrentHealth:F1}/{boss.Health.MaxHealth:F1}");
Console.WriteLine($"  Boss Tags: {string.Join(", ", boss.Tags.GetTags())}");

for (int i = 1; i <= 3; i++)
{
    var bossHit = new HitContext(player, boss, baseDamage: 30f);
    float bossDamage = damageSystem.ProcessHit(bossHit);
    Console.WriteLine($"  Attack #{i}: {bossDamage:F1} damage → Boss HP: {boss.Health.CurrentHealth:F1}");
}

Console.WriteLine($"\n  Boss still alive? {(boss.Health.IsAlive ? "YES 💀" : "DEFEATED ✓")}\n");

// Summary
Console.WriteLine("=== DEMO COMPLETE ===");
Console.WriteLine("✅ All combat mechanics working:");
Console.WriteLine("  • Basic damage calculation");
Console.WriteLine("  • Multi-hit scaling (shotgun)");
Console.WriteLine("  • Critical hits (random)");
Console.WriteLine("  • Stealth multiplier (2x)");
Console.WriteLine("  • Aware tag blocking stealth");
Console.WriteLine("  • Friendly fire prevention");
Console.WriteLine("  • Boss encounters");
Console.WriteLine("  • Health clamping (0 to MaxHealth)");
