# FeatureManifest System

## Overview

FeatureManifest, Virabis Nexus v2.0'ın temel bileşenlerinden biridir. Her feature'ın yeteneklerini, kaynak gereksinimlerini ve yaşam döngüsü ayarlarını JSON formatında tanımlamanıza olanak tanır.

## Purpose

- **Declarative Configuration**: Feature'ları kod yerine JSON ile yapılandırın
- **Resource Management**: CPU, memory ve init time limitlerini belirleyin
- **Capability Declaration**: Feature'ın sunduğu yetenekleri listeleyin
- **Event Configuration**: Feature'ın yayınladığı ve dinlediği eventleri tanımlayın
- **Lifecycle Control**: Lazy loading ve priority ayarları yapın

## File Structure

```json
{
  "name": "combat",
  "version": "1.0.0",
  "capabilities": ["damage_calculation", "combat_events"],
  "resources": {
    "maxCpuPercent": 10.0,
    "maxMemoryMB": 50.0,
    "maxInitTimeMs": 100
  },
  "lifecycle": {
    "lazyLoad": false,
    "priority": 100,
    "shutdownTimeoutMs": 5000
  },
  "events": {
    "publishes": ["CombatEvent", "DamageEvent"],
    "subscribes": ["PlayerSpawnEvent"]
  }
}
```

## Properties

### Core Properties

- **name** (string, required): Feature'ın benzersiz adı (kebab-case)
- **version** (string, required): Semantic versioning (örn: "1.0.0")
- **capabilities** (string[], required): Feature'ın sunduğu yetenekler

### ResourceBudget

- **maxCpuPercent** (double): Maksimum CPU kullanımı (%)
- **maxMemoryMB** (double): Maksimum bellek kullanımı (MB)
- **maxInitTimeMs** (int): Maksimum başlatma süresi (ms)

### LifecycleConfig

- **lazyLoad** (bool): Geç yükleme aktif mi?
- **priority** (int): Başlatma önceliği (yüksek = önce)
- **shutdownTimeoutMs** (int): Kapatma timeout süresi (ms)

### EventConfig

- **publishes** (string[]): Yayınlanan event tipleri
- **subscribes** (string[]): Dinlenen event tipleri

## Usage

### 1. Create Manifest File

Manifest dosyasını feature klasörünüze ekleyin:

```
core/Virabis.Features.Combat/combat_manifest.json
```

### 2. Load Manifest

```csharp
using Virabis.Core.Nexus.Manifest;

var loader = new ManifestLoader();
var manifest = loader.Load("path/to/combat_manifest.json");

Console.WriteLine($"Loaded: {manifest.Name} v{manifest.Version}");
Console.WriteLine($"Capabilities: {string.Join(", ", manifest.Capabilities)}");
```

### 3. Validate Manifest

```csharp
var validator = new ManifestValidator();
var result = validator.Validate(manifest);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

## Validation Rules

ManifestValidator aşağıdaki kuralları kontrol eder:

1. **Name**: Boş olamaz, sadece küçük harf, rakam ve tire içerebilir
2. **Version**: Semantic versioning formatında olmalı (X.Y.Z)
3. **Capabilities**: En az bir capability tanımlanmalı
4. **Resources**: Pozitif değerler olmalı
5. **Lifecycle**: Priority 0-1000 arasında olmalı

## Examples

### Combat Feature Manifest

```json
{
  "name": "combat",
  "version": "1.0.0",
  "capabilities": ["damage_calculation", "combat_events"],
  "resources": {
    "maxCpuPercent": 10.0,
    "maxMemoryMB": 50.0,
    "maxInitTimeMs": 100
  },
  "lifecycle": {
    "lazyLoad": false,
    "priority": 100,
    "shutdownTimeoutMs": 5000
  },
  "events": {
    "publishes": ["CombatEvent", "DamageEvent"],
    "subscribes": ["PlayerSpawnEvent"]
  }
}
```

### Test Feature Manifest

```json
{
  "name": "test",
  "version": "1.0.0",
  "capabilities": ["test_infrastructure"],
  "resources": {
    "maxCpuPercent": 5.0,
    "maxMemoryMB": 20.0,
    "maxInitTimeMs": 50
  },
  "lifecycle": {
    "lazyLoad": true,
    "priority": 50,
    "shutdownTimeoutMs": 2000
  },
  "events": {
    "publishes": ["TestStartedEvent", "TestCompletedEvent"],
    "subscribes": []
  }
}
```

## Error Handling

### InvalidManifestException

Manifest yüklenirken veya validate edilirken hata oluşursa `InvalidManifestException` fırlatılır:

```csharp
try
{
    var manifest = loader.Load("invalid_manifest.json");
}
catch (InvalidManifestException ex)
{
    Console.WriteLine($"Manifest error: {ex.Message}");
}
```

### Common Errors

- **File not found**: Manifest dosyası bulunamadı
- **Invalid JSON**: JSON formatı hatalı
- **Missing required field**: Zorunlu alan eksik
- **Invalid version format**: Version formatı yanlış
- **Empty capabilities**: Capabilities listesi boş

## Testing

### Unit Tests

```csharp
[Test]
public void Load_ValidManifest_ReturnsManifest()
{
    var loader = new ManifestLoader();
    var manifest = loader.Load("TestData/valid_manifest.json");
    
    Assert.That(manifest.Name, Is.EqualTo("test"));
    Assert.That(manifest.Version, Is.EqualTo("1.0.0"));
    Assert.That(manifest.Capabilities, Has.Count.EqualTo(1));
}

[Test]
public void Validate_InvalidName_ReturnsFalse()
{
    var manifest = new FeatureManifest
    {
        Name = "Invalid Name", // Spaces not allowed
        Version = "1.0.0",
        Capabilities = new[] { "test" }
    };
    
    var validator = new ManifestValidator();
    var result = validator.Validate(manifest);
    
    Assert.That(result.IsValid, Is.False);
}
```

### Test Lab

Godot Test Lab'de "Test FeatureManifest" butonuna tıklayarak manifest sistemini test edebilirsiniz.

## Best Practices

1. **Naming**: Feature adlarını kebab-case kullanarak yazın (örn: "combat-system")
2. **Versioning**: Semantic versioning kullanın ve her değişiklikte güncelleyin
3. **Capabilities**: Açıklayıcı ve benzersiz capability adları kullanın
4. **Resources**: Gerçekçi limitler belirleyin, profiling yaparak optimize edin
5. **Priority**: Kritik feature'lara yüksek priority verin (örn: core systems = 100)
6. **Lazy Loading**: Sık kullanılmayan feature'lar için lazy loading aktif edin

## Integration

FeatureManifest diğer Nexus v2.0 bileşenleriyle entegre çalışır:

- **FeatureDiscovery**: Manifest path'i attribute'tan alır
- **CapabilityRegistry**: Capabilities listesini kullanır
- **EventChannel**: Event configuration'ı kullanır
- **FeatureRegistry**: Lifecycle settings'i uygular

## Related Files

- `FeatureManifest.cs` - Ana model sınıfı
- `ManifestLoader.cs` - JSON yükleme
- `ManifestValidator.cs` - Validation logic (planned)
- `ResourceBudget.cs` - Resource limits
- `LifecycleConfig.cs` - Lifecycle settings
- `EventConfig.cs` - Event configuration
- `InvalidManifestException.cs` - Exception type

## See Also

- [Attribute Registration](../README.md#attribute-registration)
- [Capability System](../README.md#capability-system)
- [EventChannel](../README.md#eventchannel)
