# Attribute Registration System

## Overview

Attribute Registration, Virabis Nexus v2.0'da feature'ların otomatik olarak keşfedilmesini ve kaydedilmesini sağlayan sistemdir. `[VirabisFeature]` attribute'u kullanarak feature'larınızı işaretleyin, Nexus otomatik olarak bulsun ve yüklesin.

## Purpose

- **Auto-Discovery**: Assembly'lerdeki feature'ları otomatik bulun
- **Declarative Registration**: Attribute ile feature metadata tanımlayın
- **Zero-Configuration**: Manuel registration koduna gerek yok
- **Metadata Extraction**: Manifest ve capability bilgilerini otomatik çekin

## Quick Start

### 1. Mark Your Feature

```csharp
using Virabis.Core.Nexus;

[VirabisFeature(
    ManifestPath = "Features/Combat/combat_manifest.json",
    Capabilities = new[] { "damage_calculation", "combat_events" },
    Priority = 100,
    LazyLoad = false
)]
public class CombatFeature : IFeature
{
    public string Name => "Combat";
    
    public void Initialize()
    {
        // Feature initialization
    }
    
    public void Shutdown()
    {
        // Feature cleanup
    }
}
```

### 2. Discover Features

```csharp
using Virabis.Core.Nexus;

var assembly = Assembly.GetExecutingAssembly();
var features = FeatureDiscovery.DiscoverFeatures(assembly);

foreach (var metadata in features)
{
    Console.WriteLine($"Found: {metadata.Type.Name}");
    Console.WriteLine($"Capabilities: {string.Join(", ", metadata.Attribute.Capabilities)}");
}
```

## VirabisFeatureAttribute

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| ManifestPath | string | No | Manifest JSON dosyasının yolu |
| Capabilities | string[] | No | Feature'ın sunduğu yetenekler |
| InitTimeMs | int | No | Maksimum başlatma süresi (ms) |
| LazyLoad | bool | No | Geç yükleme aktif mi? (default: false) |
| Priority | int | No | Başlatma önceliği (0-1000, default: 50) |

### Example Usage

```csharp
// Minimal usage
[VirabisFeature]
public class SimpleFeature : IFeature { }

// With manifest
[VirabisFeature(ManifestPath = "Features/test_manifest.json")]
public class TestFeature : IFeature { }

// Full configuration
[VirabisFeature(
    ManifestPath = "Features/mcp_manifest.json",
    Capabilities = new[] { "mcp_server", "godot_interaction" },
    InitTimeMs = 200,
    LazyLoad = true,
    Priority = 75
)]
public class MCPFeature : IFeature { }
```

## FeatureDiscovery

### Methods

#### DiscoverFeatures(Assembly)

Assembly'deki tüm `[VirabisFeature]` işaretli sınıfları bulur.

```csharp
var assembly = typeof(CombatFeature).Assembly;
var features = FeatureDiscovery.DiscoverFeatures(assembly);

Console.WriteLine($"Found {features.Count} features");
```

#### GetMetadata(Type)

Belirli bir feature type'ı için metadata döndürür.

```csharp
var metadata = FeatureDiscovery.GetMetadata(typeof(CombatFeature));

if (metadata != null)
{
    Console.WriteLine($"Name: {metadata.Type.Name}");
    Console.WriteLine($"Priority: {metadata.Attribute.Priority}");
    
    if (metadata.Manifest != null)
    {
        Console.WriteLine($"Version: {metadata.Manifest.Version}");
    }
}
```

## FeatureMetadata

Discovery sonucu dönen metadata nesnesi:

```csharp
public class FeatureMetadata
{
    public Type Type { get; set; }              // Feature class type
    public VirabisFeatureAttribute Attribute { get; set; }  // Attribute instance
    public FeatureManifest? Manifest { get; set; }          // Loaded manifest (if path provided)
}
```

### Usage

```csharp
var metadata = FeatureDiscovery.GetMetadata(typeof(CombatFeature));

// Access type info
Console.WriteLine($"Type: {metadata.Type.FullName}");

// Access attribute properties
Console.WriteLine($"Priority: {metadata.Attribute.Priority}");
Console.WriteLine($"Lazy Load: {metadata.Attribute.LazyLoad}");

// Access manifest (if loaded)
if (metadata.Manifest != null)
{
    Console.WriteLine($"Manifest Version: {metadata.Manifest.Version}");
    Console.WriteLine($"Capabilities: {string.Join(", ", metadata.Manifest.Capabilities)}");
}
```

## Auto-Registration Flow

```
1. Application starts
2. FeatureDiscovery scans assemblies
3. Finds classes with [VirabisFeature]
4. Loads manifests (if ManifestPath provided)
5. Creates FeatureMetadata objects
6. Sorts by Priority
7. Instantiates features
8. Registers with FeatureRegistry
9. Initializes features (respecting LazyLoad)
```

## Examples

### Combat Feature

```csharp
[VirabisFeature(
    ManifestPath = "Features/Combat/combat_manifest.json",
    Capabilities = new[] { "damage_calculation", "combat_events" },
    Priority = 100,
    LazyLoad = false
)]
public class CombatFeature : IFeature
{
    private DamageSystem _damageSystem;
    
    public string Name => "Combat";
    
    public void Initialize()
    {
        _damageSystem = new DamageSystem();
        EventChannel<FeatureInitializedEvent>.Publish(new FeatureInitializedEvent 
        { 
            Name = Name 
        });
    }
    
    public void Shutdown()
    {
        _damageSystem = null;
    }
    
    [RequiresCapability("damage_calculation")]
    public HitResult ProcessHit(HitContext context)
    {
        return _damageSystem.ProcessHit(context);
    }
}
```

### Test Feature

```csharp
[VirabisFeature(
    ManifestPath = "Features/Test/test_manifest.json",
    Capabilities = new[] { "test_infrastructure" },
    Priority = 50,
    LazyLoad = true
)]
public class TestFeature : IFeature
{
    public string Name => "Test";
    
    public void Initialize()
    {
        Console.WriteLine("Test feature initialized");
    }
    
    public void Shutdown()
    {
        Console.WriteLine("Test feature shutdown");
    }
    
    [RequiresCapability("test_infrastructure")]
    public bool IsInitialized()
    {
        return true;
    }
}
```

### MCP Feature

```csharp
[VirabisFeature(
    ManifestPath = "Features/MCP/mcp_manifest.json",
    Capabilities = new[] { "mcp_server", "godot_interaction" },
    InitTimeMs = 200,
    LazyLoad = true,
    Priority = 75
)]
public class MCPFeature : IFeature
{
    private int _serverPort = 9999;
    private bool _initialized = false;
    
    public string Name => "MCP";
    
    public void Initialize()
    {
        _initialized = true;
    }
    
    public void Shutdown()
    {
        _initialized = false;
    }
    
    [RequiresCapability("mcp_server")]
    [RequiresCapability("godot_interaction")]
    public int GetServerPort()
    {
        return _serverPort;
    }
}
```

## Testing

### Unit Tests

```csharp
[Test]
public void DiscoverFeatures_FindsAllFeatures()
{
    var assembly = typeof(CombatFeature).Assembly;
    var features = FeatureDiscovery.DiscoverFeatures(assembly);
    
    Assert.That(features.Count, Is.GreaterThan(0));
    Assert.That(features.Any(f => f.Type == typeof(CombatFeature)), Is.True);
}

[Test]
public void GetMetadata_ReturnsCorrectMetadata()
{
    var metadata = FeatureDiscovery.GetMetadata(typeof(CombatFeature));
    
    Assert.That(metadata, Is.Not.Null);
    Assert.That(metadata.Type, Is.EqualTo(typeof(CombatFeature)));
    Assert.That(metadata.Attribute.Priority, Is.EqualTo(100));
}

[Test]
public void GetMetadata_LoadsManifest_WhenPathProvided()
{
    var metadata = FeatureDiscovery.GetMetadata(typeof(CombatFeature));
    
    Assert.That(metadata.Manifest, Is.Not.Null);
    Assert.That(metadata.Manifest.Name, Is.EqualTo("combat"));
}
```

### Test Lab

Godot Test Lab'de "Test Attribute Registration" butonuna tıklayarak attribute registration sistemini test edebilirsiniz.

## Best Practices

1. **Always Use Attribute**: Her feature'a `[VirabisFeature]` ekleyin
2. **Provide Manifest**: Manifest path belirterek tam metadata sağlayın
3. **Set Priority**: Kritik feature'lara yüksek priority verin
4. **Use Lazy Load**: Sık kullanılmayan feature'lar için lazy load aktif edin
5. **Document Capabilities**: Capability listesini güncel tutun
6. **Test Discovery**: Unit testlerde discovery'nin çalıştığını doğrulayın

## Common Patterns

### Plugin Architecture

```csharp
// Core feature - high priority, always loaded
[VirabisFeature(Priority = 100, LazyLoad = false)]
public class CoreFeature : IFeature { }

// Optional plugin - low priority, lazy loaded
[VirabisFeature(Priority = 25, LazyLoad = true)]
public class PluginFeature : IFeature { }
```

### Feature Dependencies

```csharp
// Base feature - loaded first
[VirabisFeature(Priority = 100)]
public class BaseFeature : IFeature { }

// Dependent feature - loaded after
[VirabisFeature(Priority = 50)]
public class DependentFeature : IFeature
{
    private BaseFeature _base;
    
    public void Initialize()
    {
        _base = FeatureRegistry.Instance.GetFeature<BaseFeature>();
    }
}
```

## Error Handling

### Missing Manifest

Manifest path belirtilmişse ama dosya bulunamazsa:

```csharp
try
{
    var metadata = FeatureDiscovery.GetMetadata(typeof(MyFeature));
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Manifest not found: {ex.Message}");
}
```

### Invalid Attribute

Attribute yanlış kullanılmışsa:

```csharp
// ❌ Wrong - not implementing IFeature
[VirabisFeature]
public class NotAFeature { }

// ✅ Correct
[VirabisFeature]
public class ValidFeature : IFeature { }
```

## Integration

Attribute Registration diğer Nexus v2.0 bileşenleriyle entegre çalışır:

- **FeatureManifest**: Manifest path'ten manifest yükler
- **FeatureRegistry**: Keşfedilen feature'ları kaydeder
- **CapabilityRegistry**: Capabilities listesini kullanır
- **EventChannel**: Feature lifecycle eventlerini yayınlar

## Related Files

- `VirabisFeatureAttribute.cs` - Attribute tanımı
- `FeatureDiscovery.cs` - Discovery logic
- `FeatureMetadata.cs` - Metadata model
- `FeatureRegistry.cs` - Feature registration
- `ManifestLoader.cs` - Manifest loading

## See Also

- [FeatureManifest System](Manifest/README.md)
- [Capability System](README_CAPABILITY.md)
- [FeatureRegistry](README_REGISTRY.md)
