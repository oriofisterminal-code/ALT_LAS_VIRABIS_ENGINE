# Virabis.Facade

Simplified static API for Virabis Core Nexus system.

## Overview

The `VirabisCore` facade provides a clean, intuitive static API for managing features in the Virabis Core Nexus system. It wraps the underlying `FeatureRegistry` and `EventBus` singletons, making it easier for developers to interact with the system without understanding the internal architecture.

## Features

- ✅ Enable/disable features at runtime
- ✅ Check feature health status
- ✅ Query all registered features
- ✅ Subscribe to feature lifecycle events
- ✅ Get diagnostic information
- ✅ Comprehensive XML documentation
- ✅ Simple, intuitive API

## Quick Start

### Basic Usage

```csharp
using Virabis.Facade;

// Enable a feature
VirabisCore.EnableFeature("Combat");

// Disable a feature
VirabisCore.DisableFeature("Movement");

// Check if feature is enabled
if (VirabisCore.IsFeatureEnabled("Combat"))
{
    // Use combat system
}

// Check if feature is healthy
if (VirabisCore.IsFeatureHealthy("Combat"))
{
    // Combat system is operating normally
}
```

### Event Subscriptions

```csharp
// Subscribe to feature enabled events
VirabisCore.OnFeatureEnabled(featureName =>
{
    Console.WriteLine($"Feature '{featureName}' enabled");
});

// Subscribe to feature disabled events
VirabisCore.OnFeatureDisabled(featureName =>
{
    Console.WriteLine($"Feature '{featureName}' disabled");
});

// Subscribe to health status changes
VirabisCore.OnHealthStatusChanged((featureName, isHealthy) =>
{
    if (isHealthy)
    {
        Console.WriteLine($"Feature '{featureName}' recovered");
    }
    else
    {
        Console.WriteLine($"Feature '{featureName}' became unhealthy!");
    }
});

// Subscribe to feature registration
VirabisCore.OnFeatureRegistered(featureName =>
{
    Console.WriteLine($"Feature '{featureName}' registered");
});

// Subscribe to feature unregistration
VirabisCore.OnFeatureUnregistered(featureName =>
{
    Console.WriteLine($"Feature '{featureName}' unregistered");
});
```

### Query Features

```csharp
// Get all registered features
foreach (var feature in VirabisCore.GetAllFeatures())
{
    Console.WriteLine($"Feature: {feature.Name}");
    Console.WriteLine($"  Enabled: {VirabisCore.IsFeatureEnabled(feature.Name)}");
    Console.WriteLine($"  Healthy: {VirabisCore.IsFeatureHealthy(feature.Name)}");
}

// Get diagnostic information
var diagnostics = VirabisCore.GetDiagnostics();
foreach (var kvp in diagnostics)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

## Complete Example

```csharp
using Virabis.Core.Nexus;
using Virabis.Facade;

// Register features (typically done at startup)
var combatFeature = new CombatFeature();
var movementFeature = new MovementFeature();

FeatureRegistry.Instance.Register(combatFeature);
FeatureRegistry.Instance.Register(movementFeature);

// Subscribe to events
VirabisCore.OnFeatureEnabled(name => 
    Console.WriteLine($"[System] {name} enabled"));

VirabisCore.OnHealthStatusChanged((name, isHealthy) =>
{
    if (!isHealthy)
    {
        Console.WriteLine($"[Alert] {name} is unhealthy!");
    }
});

// Use features
if (VirabisCore.IsFeatureEnabled("Combat") && 
    VirabisCore.IsFeatureHealthy("Combat"))
{
    // Safe to use combat system
    ProcessCombat();
}

// Temporarily disable a feature for testing
VirabisCore.DisableFeature("Movement");
RunCombatTests();
VirabisCore.EnableFeature("Movement");

// Display all features
Console.WriteLine("=== Registered Features ===");
foreach (var feature in VirabisCore.GetAllFeatures())
{
    var enabled = VirabisCore.IsFeatureEnabled(feature.Name);
    var healthy = VirabisCore.IsFeatureHealthy(feature.Name);
    var status = enabled && healthy ? "✓" : "✗";
    
    Console.WriteLine($"{status} {feature.Name}");
}
```

## API Reference

### Feature Management

| Method | Description |
|--------|-------------|
| `EnableFeature(string name)` | Enable a feature by name |
| `DisableFeature(string name)` | Disable a feature by name |
| `IsFeatureEnabled(string name)` | Check if feature is enabled |
| `IsFeatureHealthy(string name)` | Check if feature is healthy |

### Feature Query

| Method | Description |
|--------|-------------|
| `GetAllFeatures()` | Get all registered features |
| `GetDiagnostics()` | Get diagnostic information for all features |

### Event Subscriptions

| Method | Description |
|--------|-------------|
| `OnFeatureEnabled(Action<string>)` | Subscribe to feature enabled events |
| `OnFeatureDisabled(Action<string>)` | Subscribe to feature disabled events |
| `OnHealthStatusChanged(Action<string, bool>)` | Subscribe to health status changes |
| `OnFeatureRegistered(Action<string>)` | Subscribe to feature registration |
| `OnFeatureUnregistered(Action<string>)` | Subscribe to feature unregistration |

## Best Practices

### 1. Check Both Enabled and Healthy

Always check both enabled and healthy status before using a feature:

```csharp
if (VirabisCore.IsFeatureEnabled("Combat") && 
    VirabisCore.IsFeatureHealthy("Combat"))
{
    // Safe to use combat system
}
```

### 2. Subscribe to Health Changes

Monitor health status changes to handle degradation gracefully:

```csharp
VirabisCore.OnHealthStatusChanged((name, isHealthy) =>
{
    if (!isHealthy && name == "Combat")
    {
        // Show warning to player
        ShowWarning("Combat system experiencing issues");
    }
});
```

### 3. Use Diagnostics for Debugging

Display diagnostics in debug panels:

```csharp
var diagnostics = VirabisCore.GetDiagnostics();
foreach (var kvp in diagnostics)
{
    DebugPanel.AddLine($"{kvp.Key}: {kvp.Value}");
}
```

### 4. Keep Event Handlers Alive

The EventBus uses weak references, so keep a strong reference to your handlers:

```csharp
// Good - handler is a field
private Action<string> _featureEnabledHandler;

public void Initialize()
{
    _featureEnabledHandler = name => Console.WriteLine($"Enabled: {name}");
    VirabisCore.OnFeatureEnabled(_featureEnabledHandler);
}

// Bad - handler may be garbage collected
public void Initialize()
{
    VirabisCore.OnFeatureEnabled(name => Console.WriteLine($"Enabled: {name}"));
}
```

## Testing

The facade includes comprehensive unit tests covering all functionality:

```bash
dotnet test core/Virabis.Facade.Tests/
```

All 26 tests should pass:
- Feature enable/disable
- Health status checks
- Event subscriptions
- Feature queries
- Error handling
- Edge cases

## Architecture

The facade follows the **Facade Pattern**, providing a simplified interface to the complex Core Nexus subsystem:

```
┌─────────────────────────────────────┐
│      VirabisCore (Facade)           │
│  - Simple static API                │
│  - No state                         │
│  - Delegates to singletons          │
└─────────────────┬───────────────────┘
                  │
        ┌─────────┴─────────┐
        │                   │
┌───────▼────────┐  ┌───────▼────────┐
│ FeatureRegistry│  │   EventBus     │
│  (Singleton)   │  │  (Singleton)   │
└────────────────┘  └────────────────┘
```

## Dependencies

- `Virabis.Core.Nexus` - Foundation layer with FeatureRegistry and EventBus
- `.NET 8.0` - Target framework

## License

Part of the Virabis project.

## See Also

- [Virabis.Core.Nexus](../Virabis.Core.Nexus/README.md) - Foundation layer
- [ARCHITECTURE.md](../../ARCHITECTURE.md) - System architecture
- [Design Document](.kiro/specs/virabis-core-nexus/design.md) - Detailed design
