# Capability System

## Overview

Capability System, Virabis Nexus v2.0'da method-level security ve access control sağlayan sistemdir. Feature'ların hangi yeteneklere sahip olduğunu tanımlayın ve method'ları `[RequiresCapability]` attribute'u ile koruyun.

## Purpose

- **Method-Level Security**: Her method için gerekli capability'leri tanımlayın
- **Compile-Time Validation**: CapabilityValidator ile capability eksikliklerini bulun
- **Runtime Enforcement**: CapabilityRegistry ile runtime'da access control yapın
- **Clear Error Messages**: Hangi capability eksikse açıkça belirtin

## Quick Start

### 1. Define Feature Capabilities

```csharp
[VirabisFeature(
    Capabilities = new[] { "damage_calculation", "combat_events" }
)]
public class CombatFeature : IFeature
{
    // ...
}
```

### 2. Protect Methods

```csharp
[RequiresCapability("damage_calculation")]
public HitResult ProcessHit(HitContext context)
{
    return _damageSystem.ProcessHit(context);
}
```

### 3. Register Capabilities

```csharp
var registry = CapabilityRegistry.Instance;
registry.Register("Combat", new[] { "damage_calculation", "combat_events" });
```

### 4. Enforce at Runtime

```csharp
// Throws UnauthorizedAccessException if capability missing
registry.EnforceCapability("Combat", "damage_calculation");
```

## Components

### RequiresCapabilityAttribute

Method'ları capability ile korur.

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequiresCapabilityAttribute : Attribute
{
    public string Capability { get; }
    
    public RequiresCapabilityAttribute(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            throw new ArgumentException("Capability cannot be null or empty");
        
        Capability = capability;
    }
}
```

#### Usage

```csharp
// Single capability
[RequiresCapability("damage_calculation")]
public void Method1() { }

// Multiple capabilities
[RequiresCapability("mcp_server")]
[RequiresCapability("godot_interaction")]
public void Method2() { }
```

### CapabilityValidator

Compile-time validation için feature'ları analiz eder.

```csharp
public class CapabilityValidator
{
    public ValidationResult ValidateFeature(Type featureType);
    public HashSet<string> GetRequiredCapabilities(Type featureType);
}
```

#### Usage

```csharp
var validator = new CapabilityValidator();
var result = validator.ValidateFeature(typeof(CombatFeature));

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### CapabilityRegistry

Runtime'da capability yönetimi ve enforcement.

```csharp
public class CapabilityRegistry
{
    public static CapabilityRegistry Instance { get; }
    
    public void Register(string featureName, IEnumerable<string> capabilities);
    public bool HasCapability(string featureName, string capability);
    public void EnforceCapability(string featureName, string capability);
    public IReadOnlySet<string> GetCapabilities(string featureName);
    public void Unregister(string featureName);
    public void ClearAll();
}
```

#### Usage

```csharp
var registry = CapabilityRegistry.Instance;

// Register
registry.Register("Combat", new[] { "damage_calculation", "combat_events" });

// Check
if (registry.HasCapability("Combat", "damage_calculation"))
{
    // Safe to call
}

// Enforce (throws if missing)
registry.EnforceCapability("Combat", "damage_calculation");

// Get all
var caps = registry.GetCapabilities("Combat");
Console.WriteLine($"Capabilities: {string.Join(", ", caps)}");
```

## Usage Examples

### Combat Feature

```csharp
[VirabisFeature(
    ManifestPath = "Features/Combat/combat_manifest.json",
    Capabilities = new[] { "damage_calculation", "combat_events" }
)]
public class CombatFeature : IFeature
{
    private DamageSystem _damageSystem;
    
    public string Name => "Combat";
    
    public void Initialize()
    {
        _damageSystem = new DamageSystem();
        
        // Register capabilities
        CapabilityRegistry.Instance.Register(Name, new[] 
        { 
            "damage_calculation", 
            "combat_events" 
        });
    }
    
    [RequiresCapability("damage_calculation")]
    public HitResult ProcessHit(HitContext context)
    {
        // Enforce at runtime
        CapabilityRegistry.Instance.EnforceCapability(Name, "damage_calculation");
        
        return _damageSystem.ProcessHit(context);
    }
    
    [RequiresCapability("damage_calculation")]
    public DamageSystem GetDamageSystem()
    {
        CapabilityRegistry.Instance.EnforceCapability(Name, "damage_calculation");
        return _damageSystem;
    }
    
    [RequiresCapability("combat_events")]
    public void PublishCombatEvent(string eventData)
    {
        CapabilityRegistry.Instance.EnforceCapability(Name, "combat_events");
        // Publish event...
    }
}
```

### Test Feature

```csharp
[VirabisFeature(
    ManifestPath = "Features/Test/test_manifest.json",
    Capabilities = new[] { "test_infrastructure" }
)]
public class TestFeature : IFeature
{
    public string Name => "Test";
    
    public void Initialize()
    {
        CapabilityRegistry.Instance.Register(Name, new[] { "test_infrastructure" });
    }
    
    [RequiresCapability("test_infrastructure")]
    public bool IsInitialized()
    {
        CapabilityRegistry.Instance.EnforceCapability(Name, "test_infrastructure");
        return true;
    }
}
```

### MCP Feature

```csharp
[VirabisFeature(
    ManifestPath = "Features/MCP/mcp_manifest.json",
    Capabilities = new[] { "mcp_server", "godot_interaction" }
)]
public class MCPFeature : IFeature
{
    private int _serverPort = 9999;
    
    public string Name => "MCP";
    
    public void Initialize()
    {
        CapabilityRegistry.Instance.Register(Name, new[] 
        { 
            "mcp_server", 
            "godot_interaction" 
        });
    }
    
    [RequiresCapability("mcp_server")]
    [RequiresCapability("godot_interaction")]
    public int GetServerPort()
    {
        CapabilityRegistry.Instance.EnforceCapability(Name, "mcp_server");
        CapabilityRegistry.Instance.EnforceCapability(Name, "godot_interaction");
        return _serverPort;
    }
    
    [RequiresCapability("mcp_server")]
    public bool IsInitialized()
    {
        CapabilityRegistry.Instance.EnforceCapability(Name, "mcp_server");
        return true;
    }
}
```

## Validation

### Compile-Time Validation

```csharp
var validator = new CapabilityValidator();

// Validate feature
var result = validator.ValidateFeature(typeof(CombatFeature));

if (!result.IsValid)
{
    Console.WriteLine("Validation failed:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}

// Get required capabilities
var required = validator.GetRequiredCapabilities(typeof(CombatFeature));
Console.WriteLine($"Required: {string.Join(", ", required)}");
```

### Runtime Enforcement

```csharp
try
{
    // This will throw if capability missing
    registry.EnforceCapability("Combat", "damage_calculation");
    
    // Safe to proceed
    var result = combatFeature.ProcessHit(context);
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"Access denied: {ex.Message}");
}
```

## Validation Result

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

### Example

```csharp
var result = validator.ValidateFeature(typeof(MyFeature));

if (result.IsValid)
{
    Console.WriteLine("✓ All capabilities declared");
}
else
{
    Console.WriteLine("✗ Missing capabilities:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  {error}");
    }
}
```

## Error Messages

### Missing Capability

```
Feature 'Combat' does not have capability 'damage_calculation'
```

### Method Requires Capability

```
Method 'ProcessHit' requires capability 'damage_calculation' but feature 'Combat' does not declare it
```

### Null/Empty Capability

```
Capability cannot be null or empty
```

## Testing

### Unit Tests

```csharp
[Test]
public void Register_StoresCapabilities()
{
    var registry = CapabilityRegistry.Instance;
    registry.ClearAll();
    
    registry.Register("Test", new[] { "cap1", "cap2" });
    
    Assert.That(registry.HasCapability("Test", "cap1"), Is.True);
    Assert.That(registry.HasCapability("Test", "cap2"), Is.True);
}

[Test]
public void EnforceCapability_ThrowsWhenMissing()
{
    var registry = CapabilityRegistry.Instance;
    registry.ClearAll();
    
    registry.Register("Test", new[] { "cap1" });
    
    Assert.Throws<UnauthorizedAccessException>(() =>
    {
        registry.EnforceCapability("Test", "cap2");
    });
}

[Test]
public void ValidateFeature_DetectsMissingCapabilities()
{
    var validator = new CapabilityValidator();
    
    // Feature with method requiring undeclared capability
    var result = validator.ValidateFeature(typeof(InvalidFeature));
    
    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors, Has.Count.GreaterThan(0));
}
```

### Test Lab

Godot Test Lab'de "Test Capability System" butonuna tıklayarak capability sistemini test edebilirsiniz.

## Best Practices

1. **Declare All Capabilities**: Feature attribute'unda tüm capability'leri listeleyin
2. **Use Descriptive Names**: Capability adları açıklayıcı olsun (örn: "damage_calculation")
3. **Validate Early**: Compile-time validation ile hataları erken yakalayın
4. **Enforce at Runtime**: Kritik method'larda EnforceCapability kullanın
5. **Document Capabilities**: Her capability'nin ne yaptığını dokümante edin
6. **Test Validation**: Unit test'lerde validation'ı test edin

## Common Patterns

### Capability Groups

```csharp
// Core capabilities
public static class CoreCapabilities
{
    public const string DamageCalculation = "damage_calculation";
    public const string CombatEvents = "combat_events";
    public const string HealthManagement = "health_management";
}

// Usage
[RequiresCapability(CoreCapabilities.DamageCalculation)]
public void Method() { }
```

### Capability Inheritance

```csharp
// Base feature
[VirabisFeature(Capabilities = new[] { "base_capability" })]
public class BaseFeature : IFeature { }

// Derived feature adds more
[VirabisFeature(Capabilities = new[] { "base_capability", "derived_capability" })]
public class DerivedFeature : BaseFeature { }
```

### Conditional Capabilities

```csharp
public void Initialize()
{
    var capabilities = new List<string> { "core" };
    
    if (IsDebugMode)
    {
        capabilities.Add("debug_tools");
    }
    
    CapabilityRegistry.Instance.Register(Name, capabilities);
}
```

## Advanced Usage

### Custom Validation

```csharp
public class CustomValidator : CapabilityValidator
{
    public override ValidationResult ValidateFeature(Type featureType)
    {
        var result = base.ValidateFeature(featureType);
        
        // Add custom validation
        var required = GetRequiredCapabilities(featureType);
        if (required.Contains("admin") && !required.Contains("audit"))
        {
            result.IsValid = false;
            result.Errors.Add("Admin capability requires audit capability");
        }
        
        return result;
    }
}
```

### Capability Policies

```csharp
public class CapabilityPolicy
{
    public bool AllowFeature(string featureName, string capability)
    {
        // Custom policy logic
        if (featureName == "Combat" && capability == "god_mode")
        {
            return IsDebugBuild();
        }
        
        return true;
    }
}
```

### Audit Logging

```csharp
public class AuditedCapabilityRegistry : CapabilityRegistry
{
    public override void EnforceCapability(string featureName, string capability)
    {
        Log($"Capability check: {featureName}.{capability}");
        
        try
        {
            base.EnforceCapability(featureName, capability);
            Log($"✓ Access granted");
        }
        catch (UnauthorizedAccessException)
        {
            Log($"✗ Access denied");
            throw;
        }
    }
}
```

## Security Considerations

1. **Don't Trust Client**: Always enforce capabilities server-side
2. **Validate Input**: Check capability names for injection attacks
3. **Audit Access**: Log capability checks for security audits
4. **Principle of Least Privilege**: Only grant necessary capabilities
5. **Regular Review**: Periodically review capability assignments

## Performance

Capability checks are fast:
- Registry lookup: O(1) hash table lookup
- Enforcement: ~10 ns overhead
- Validation: One-time at startup

## Limitations

1. **No Dynamic Capabilities**: Capabilities must be known at compile-time
2. **No Capability Hierarchy**: No parent/child capability relationships
3. **No Expiration**: Capabilities don't expire automatically
4. **No Delegation**: Can't delegate capabilities to other features

## Future Enhancements

- **Runtime Interception**: Castle.DynamicProxy for automatic enforcement
- **Capability Hierarchy**: Parent capabilities include children
- **Capability Expiration**: Time-based capability revocation
- **Capability Delegation**: Features can delegate capabilities
- **Policy Engine**: Complex capability policies

## Related Files

- `RequiresCapabilityAttribute.cs` - Attribute definition
- `CapabilityValidator.cs` - Compile-time validation
- `CapabilityRegistry.cs` - Runtime registry
- `CapabilitySystemTests.cs` - Unit tests

## See Also

- [Attribute Registration](README_ATTRIBUTE_REGISTRATION.md)
- [FeatureManifest System](Manifest/README.md)
- [Security Best Practices](../../docs/SECURITY.md)
