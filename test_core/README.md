# Virabis Core

Clean architecture game engine core for cyberpunk roguelite games.

## Features

- **DamageSystem** - Configurable damage with crit, stealth, multi-hit
- **Entity System** - Team-based entities with health, stats, tags
- **Nexus** - Feature management with event-driven architecture
- **EventChannel** - Zero-allocation struct-based events
- **TestSystem** - Fluent API for easy test writing

## Quick Start

### Damage Example

```csharp
var player = new Entity(TeamId.Player, maxHealth: 100);
var enemy = new Entity(TeamId.Enemy, maxHealth: 100);
var damageSystem = new DamageSystem(new SystemRandomProvider());

var hit = new HitContext(player, enemy, baseDamage: 10);
float damage = damageSystem.ProcessHit(hit);
```

### Test Example

```csharp
// Simple one-liner
[Fact] public void BasicDamage() => Test.Hit(10).ShouldDeal(10);

// Fluent API
Test.Damage()
    .WithDamage(10)
    .WithCrit(multiplier: 2f)
    .ShouldDeal(20);

// Property test
[Fact] public void Property() => Property.Damage_NeverNegative();
```

## Project Structure

```
├── Virabis.Core/              # Core systems (Entity, Damage, AI)
├── Virabis.Core.Nexus/        # Feature management, events
├── Virabis.Core.Tests/        # Unit tests with TestSystem
├── Virabis.Core.Nexus.Tests/  # Nexus tests
├── Virabis.Facade/            # Facade pattern API
├── Virabis.Features.*/        # Optional features (Combat, MCP, etc.)
└── Virabis.Movement.Tests/    # Movement system tests
```

## Tech Stack

- **.NET 8.0** with C# 12
- **Godot 4.6** compatible
- **xUnit** + **CsCheck** for testing
- **3-Layer Architecture**: Core → Bridge → Gameplay

## License

MIT
