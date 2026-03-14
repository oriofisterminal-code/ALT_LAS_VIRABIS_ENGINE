# FeatureTestBase

## Overview

FeatureTestBase, feature test'lerinde tekrarlanan boilerplate kodunu ortadan kaldıran generic bir base class'tır. Her feature için aynı setup/teardown ve temel test'leri yazmak yerine, bu class'tan inherit ederek %70 daha az kod yazarsınız.

## Purpose

- **Reduce Boilerplate**: Setup/teardown kodunu tek yerde tanımlayın
- **Standard Tests**: Her feature için ortak test'leri otomatik sağlayın
- **Type Safety**: Generic constraint ile IFeature implementasyonunu garanti edin
- **Consistent Testing**: Tüm feature'lar için tutarlı test yapısı

## Quick Start

### Before (Without FeatureTestBase)

```csharp
[TestFixture]
public class CombatFeatureTests
{
    private CombatFeature _feature;
    private FeatureRegistry _registry;
    
    [SetUp]
    public void SetUp()
    {
        _registry = FeatureRegistry.Instance;
        _registry.ClearAll();
        _feature = new CombatFeature();
        _registry.Register(_feature);
        _feature.Initialize();
    }
    
    [TearDown]
    public void TearDown()
    {
        _feature?.Shutdown();
        _registry?.ClearAll();
    }
    
    [Test]
    public void Feature_ShouldInitializeSuccessfully()
    {
        Assert.DoesNotThrow(() => _feature.Initialize());
    }
    
    [Test]
    public void Feature_ShouldShutdownSuccessfully()
    {
        Assert.DoesNotThrow(() => _feature.Shutdown());
    }
    
    // Your custom tests...
}
```

### After (With FeatureTestBase)

```csharp
[TestFixture]
public class CombatFeatureTests : FeatureTestBase<CombatFeature>
{
    // Setup/teardown and base tests are inherited!
    
    [Test]
    public void ProcessHit_ShouldCalculateDamage()
    {
        // Use Feature property from base class
        var context = new HitContext { BaseDamage = 100 };
        var result = Feature.ProcessHit(context);
        
        Assert.That(result.FinalDamage, Is.GreaterThan(0));
    }
}
```

**Result**: 70% less code, same functionality!

## Generic Constraint

```csharp
public abstract class FeatureTestBase<T> where T : IFeature, new()
```

- **T**: Feature type being tested
- **IFeature**: Ensures T implements IFeature interface
- **new()**: Ensures T has parameterless constructor

## Inherited Properties

### Feature

```csharp
protected T Feature { get; private set; }
```

Your feature instance, ready to use in tests.

```csharp
[Test]
public void MyTest()
{
    var result = Feature.SomeMethod();
    Assert.That(result, Is.Not.Null);
}
```

### Registry

```csharp
protected FeatureRegistry Registry { get; private set; }
```

FeatureRegistry instance for advanced scenarios.

```csharp
[Test]
public void MyTest()
{
    var otherFeature = Registry.GetFeature<OtherFeature>();
    Assert.That(otherFeature, Is.Not.Null);
}
```

### EventBus

```csharp
protected EventBus EventBus { get; private set; }
```

EventBus instance for event testing.

```csharp
[Test]
public void MyTest()
{
    bool eventReceived = false;
    EventBus.Subscribe<MyEvent>(e => eventReceived = true);
    
    Feature.DoSomething();
    
    Assert.That(eventReceived, Is.True);
}
```

## Inherited Methods

### BaseSetUp()

Automatically called before each test:

```csharp
[SetUp]
public void BaseSetUp()
{
    Registry = FeatureRegistry.Instance;
    Registry.ClearAll();
    EventBus = EventBus.Instance;
    Feature = new T();
    Registry.Register(Feature);
    Feature.Initialize();
}
```

### BaseTearDown()

Automatically called after each test:

```csharp
[TearDown]
public void BaseTearDown()
{
    Feature?.Shutdown();
    Registry?.ClearAll();
}
```

## Inherited Tests

### Feature_ShouldInitializeSuccessfully

```csharp
[Test]
public void Feature_ShouldInitializeSuccessfully()
{
    Assert.DoesNotThrow(() => Feature.Initialize());
}
```

Verifies feature can initialize without throwing exceptions.

### Feature_ShouldShutdownSuccessfully

```csharp
[Test]
public void Feature_ShouldShutdownSuccessfully()
{
    Assert.DoesNotThrow(() => Feature.Shutdown());
}
```

Verifies feature can shutdown cleanly.

### Feature_ShouldRegisterWithNexus

```csharp
[Test]
public void Feature_ShouldRegisterWithNexus()
{
    var registered = Registry.GetFeature<T>();
    Assert.That(registered, Is.Not.Null);
    Assert.That(registered, Is.SameAs(Feature));
}
```

Verifies feature is properly registered.

### Feature_ShouldPassHealthCheck

```csharp
[Test]
public void Feature_ShouldPassHealthCheck()
{
    if (Feature is IHealthCheck healthCheck)
    {
        var status = healthCheck.GetHealthStatus();
        Assert.That(status, Does.Contain("Healthy").Or.Contain("OK"));
    }
}
```

Verifies health check (if feature implements IHealthCheck).

## Usage Examples

### Combat Feature Tests

```csharp
[TestFixture]
public class CombatFeatureTests : FeatureTestBase<CombatFeature>
{
    [Test]
    public void ProcessHit_WithValidContext_CalculatesDamage()
    {
        var context = new HitContext
        {
            Attacker = new Entity { Id = 1 },
            Target = new Entity { Id = 2 },
            BaseDamage = 100
        };
        
        var result = Feature.ProcessHit(context);
        
        Assert.That(result.FinalDamage, Is.GreaterThan(0));
        Assert.That(result.WasHit, Is.True);
    }
    
    [Test]
    public void GetDamageSystem_ReturnsValidSystem()
    {
        var system = Feature.GetDamageSystem();
        
        Assert.That(system, Is.Not.Null);
    }
}
```

### Test Feature Tests

```csharp
[TestFixture]
public class TestFeatureTests : FeatureTestBase<TestFeature>
{
    [Test]
    public void IsInitialized_AfterSetup_ReturnsTrue()
    {
        var result = Feature.IsInitialized();
        
        Assert.That(result, Is.True);
    }
}
```

### MCP Feature Tests

```csharp
[TestFixture]
public class MCPFeatureTests : FeatureTestBase<MCPFeature>
{
    [Test]
    public void GetServerPort_ReturnsValidPort()
    {
        var port = Feature.GetServerPort();
        
        Assert.That(port, Is.GreaterThan(0));
        Assert.That(port, Is.LessThan(65536));
    }
    
    [Test]
    public void IsInitialized_AfterSetup_ReturnsTrue()
    {
        var result = Feature.IsInitialized();
        
        Assert.That(result, Is.True);
    }
}
```

## Advanced Usage

### Custom Setup

Override BaseSetUp for additional setup:

```csharp
[TestFixture]
public class MyFeatureTests : FeatureTestBase<MyFeature>
{
    private MyDependency _dependency;
    
    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp(); // Call parent setup
        
        // Your custom setup
        _dependency = new MyDependency();
        Feature.SetDependency(_dependency);
    }
    
    [TearDown]
    public new void BaseTearDown()
    {
        // Your custom teardown
        _dependency?.Dispose();
        
        base.BaseTearDown(); // Call parent teardown
    }
}
```

### Event Testing

```csharp
[TestFixture]
public class MyFeatureTests : FeatureTestBase<MyFeature>
{
    [Test]
    public void DoSomething_PublishesEvent()
    {
        bool eventReceived = false;
        MyEvent receivedEvent = default;
        
        EventChannel<MyEvent>.Subscribe(e =>
        {
            eventReceived = true;
            receivedEvent = e;
        });
        
        Feature.DoSomething();
        
        Assert.That(eventReceived, Is.True);
        Assert.That(receivedEvent.Data, Is.Not.Null);
    }
}
```

### Multi-Feature Testing

```csharp
[TestFixture]
public class IntegrationTests : FeatureTestBase<CombatFeature>
{
    private TestFeature _testFeature;
    
    [SetUp]
    public new void BaseSetUp()
    {
        base.BaseSetUp();
        
        _testFeature = new TestFeature();
        Registry.Register(_testFeature);
        _testFeature.Initialize();
    }
    
    [Test]
    public void Features_ShouldWorkTogether()
    {
        // Test interaction between Combat and Test features
        var combatResult = Feature.ProcessHit(new HitContext());
        var testResult = _testFeature.IsInitialized();
        
        Assert.That(combatResult, Is.Not.Null);
        Assert.That(testResult, Is.True);
    }
}
```

## Benefits

### Code Reduction

| Metric | Before | After | Reduction |
|--------|--------|-------|-----------|
| Lines of code | ~40 | ~12 | 70% |
| Setup/teardown | Manual | Automatic | 100% |
| Base tests | Manual | Inherited | 100% |
| Boilerplate | High | Minimal | 90% |

### Consistency

All feature tests have:
- Same setup/teardown pattern
- Same base test coverage
- Same property names
- Same test structure

### Maintainability

Changes to base testing logic only need to be made once in FeatureTestBase, not in every feature test class.

## Testing FeatureTestBase

### Meta Tests

```csharp
[TestFixture]
public class FeatureTestBaseTests
{
    [Test]
    public void BaseSetUp_InitializesFeature()
    {
        var tests = new CombatFeatureTests();
        tests.BaseSetUp();
        
        Assert.That(tests.Feature, Is.Not.Null);
    }
    
    [Test]
    public void BaseTearDown_CleansUpResources()
    {
        var tests = new CombatFeatureTests();
        tests.BaseSetUp();
        tests.BaseTearDown();
        
        // Verify cleanup
        Assert.That(FeatureRegistry.Instance.GetFeature<CombatFeature>(), Is.Null);
    }
}
```

### Test Lab

Godot Test Lab'de "Test FeatureTestBase" butonuna tıklayarak FeatureTestBase sistemini test edebilirsiniz.

## Best Practices

1. **Always Inherit**: Tüm feature test'leri FeatureTestBase'den inherit etsin
2. **Use Feature Property**: Feature instance'a erişmek için Feature property kullanın
3. **Call Base Methods**: Custom setup/teardown'da base method'ları çağırın
4. **Keep Tests Simple**: Base class karmaşıklığı gizler, test'lerinizi basit tutun
5. **Test One Thing**: Her test tek bir davranışı test etsin
6. **Use Descriptive Names**: Test method adları açıklayıcı olsun

## Common Patterns

### Arrange-Act-Assert

```csharp
[Test]
public void Method_Scenario_ExpectedResult()
{
    // Arrange
    var input = new Input { Value = 42 };
    
    // Act
    var result = Feature.Method(input);
    
    // Assert
    Assert.That(result, Is.Not.Null);
}
```

### Exception Testing

```csharp
[Test]
public void Method_InvalidInput_ThrowsException()
{
    var invalidInput = new Input { Value = -1 };
    
    Assert.Throws<ArgumentException>(() => Feature.Method(invalidInput));
}
```

### State Verification

```csharp
[Test]
public void Method_ChangesState()
{
    var initialState = Feature.GetState();
    
    Feature.Method();
    
    var finalState = Feature.GetState();
    Assert.That(finalState, Is.Not.EqualTo(initialState));
}
```

## Limitations

1. **Parameterless Constructor**: Feature must have `new()` constraint
2. **Single Feature**: Base class tests only one feature at a time
3. **Standard Setup**: Custom setup requires overriding BaseSetUp

## Migration Guide

### Step 1: Inherit from FeatureTestBase

```csharp
// Before
[TestFixture]
public class MyFeatureTests
{
}

// After
[TestFixture]
public class MyFeatureTests : FeatureTestBase<MyFeature>
{
}
```

### Step 2: Remove Manual Setup/Teardown

```csharp
// Before
[SetUp]
public void SetUp()
{
    _feature = new MyFeature();
    _feature.Initialize();
}

[TearDown]
public void TearDown()
{
    _feature?.Shutdown();
}

// After
// Delete these methods - inherited from base!
```

### Step 3: Update Test Methods

```csharp
// Before
[Test]
public void MyTest()
{
    var result = _feature.Method();
}

// After
[Test]
public void MyTest()
{
    var result = Feature.Method(); // Use Feature property
}
```

### Step 4: Remove Base Tests

```csharp
// Before
[Test]
public void Feature_ShouldInitializeSuccessfully()
{
    Assert.DoesNotThrow(() => _feature.Initialize());
}

// After
// Delete this test - inherited from base!
```

## Related Files

- `FeatureTestBase.cs` - Base class implementation
- `CombatFeatureTests.cs` - Example usage
- `TestFeatureTests.cs` - Example usage
- `MCPFeatureTests.cs` - Example usage

## See Also

- [Testing Best Practices](../../docs/TESTING.md)
- [Feature Development Guide](../../docs/FEATURES.md)
- [NUnit Documentation](https://docs.nunit.org/)
