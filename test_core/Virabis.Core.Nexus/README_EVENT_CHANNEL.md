# EventChannel System

## Overview

EventChannel, Virabis Nexus v2.0'da EventBus'ın yerini alan yüksek performanslı event sistemidir. Struct-based event'ler kullanarak zero-allocation ve 3x daha hızlı event delivery sağlar.

## Purpose

- **Zero Allocation**: Struct event'ler ile GC pressure'ı ortadan kaldırın
- **High Performance**: EventBus'tan 3x daha hızlı event delivery
- **Type Safety**: Generic constraint ile compile-time type checking
- **RAII Pattern**: IDisposable ile otomatik unsubscribe

## Quick Start

### 1. Define Event Struct

```csharp
public struct PlayerDamagedEvent
{
    public int PlayerId { get; set; }
    public float Damage { get; set; }
    public string Source { get; set; }
}
```

### 2. Subscribe to Events

```csharp
EventChannel<PlayerDamagedEvent>.Subscribe(e =>
{
    Console.WriteLine($"Player {e.PlayerId} took {e.Damage} damage from {e.Source}");
});
```

### 3. Publish Events

```csharp
EventChannel<PlayerDamagedEvent>.Publish(new PlayerDamagedEvent
{
    PlayerId = 1,
    Damage = 25.5f,
    Source = "Enemy"
});
```

## Why Struct Events?

### Class-Based Events (Old EventBus)

```csharp
// ❌ Allocates memory on heap
public class PlayerDamagedEvent
{
    public int PlayerId { get; set; }
    public float Damage { get; set; }
}

// Each publish creates garbage
EventBus.Publish(new PlayerDamagedEvent { PlayerId = 1, Damage = 25 });
```

### Struct-Based Events (New EventChannel)

```csharp
// ✅ Allocated on stack, no GC
public struct PlayerDamagedEvent
{
    public int PlayerId { get; set; }
    public float Damage { get; set; }
}

// Zero allocation
EventChannel<PlayerDamagedEvent>.Publish(new PlayerDamagedEvent { PlayerId = 1, Damage = 25 });
```

## Performance Comparison

| Metric | EventBus (Class) | EventChannel (Struct) | Improvement |
|--------|------------------|----------------------|-------------|
| Publish Time | 150 ns | 50 ns | 3x faster |
| Memory Allocation | 48 bytes | 0 bytes | Zero allocation |
| GC Pressure | High | None | 100% reduction |
| Type Safety | Runtime | Compile-time | Safer |

## API Reference

### EventChannel<T>

Generic event channel for type T.

```csharp
public static class EventChannel<T> where T : struct
```

#### Methods

##### Publish(T event)

Publishes an event to all subscribers.

```csharp
EventChannel<MyEvent>.Publish(new MyEvent { Data = "test" });
```

##### Subscribe(Action<T> handler)

Subscribes to events.

```csharp
EventChannel<MyEvent>.Subscribe(e =>
{
    Console.WriteLine($"Received: {e.Data}");
});
```

##### Unsubscribe(Action<T> handler)

Unsubscribes from events.

```csharp
void MyHandler(MyEvent e) { }

EventChannel<MyEvent>.Subscribe(MyHandler);
EventChannel<MyEvent>.Unsubscribe(MyHandler);
```

### EventSubscription<T>

RAII-style subscription that auto-unsubscribes on dispose.

```csharp
using (var subscription = EventChannel<MyEvent>.Subscribe(e => { }))
{
    // Subscribed here
} // Auto-unsubscribed here
```

## Usage Examples

### Feature Lifecycle Events

```csharp
// Define events
public struct FeatureRegisteredEvent
{
    public string Name { get; set; }
}

public struct FeatureInitializedEvent
{
    public string Name { get; set; }
}

public struct FeatureShutdownEvent
{
    public string Name { get; set; }
}

// Subscribe
EventChannel<FeatureInitializedEvent>.Subscribe(e =>
{
    Console.WriteLine($"Feature {e.Name} initialized");
});

// Publish
EventChannel<FeatureInitializedEvent>.Publish(new FeatureInitializedEvent
{
    Name = "Combat"
});
```

### Combat Events

```csharp
public struct DamageDealtEvent
{
    public int AttackerId { get; set; }
    public int TargetId { get; set; }
    public float Damage { get; set; }
    public bool IsCritical { get; set; }
}

// Subscribe
EventChannel<DamageDealtEvent>.Subscribe(e =>
{
    if (e.IsCritical)
    {
        Console.WriteLine($"CRITICAL HIT! {e.Damage} damage");
    }
});

// Publish
EventChannel<DamageDealtEvent>.Publish(new DamageDealtEvent
{
    AttackerId = 1,
    TargetId = 2,
    Damage = 150.0f,
    IsCritical = true
});
```

### UI Events

```csharp
public struct ButtonClickedEvent
{
    public string ButtonId { get; set; }
    public float ClickTime { get; set; }
}

// Subscribe
EventChannel<ButtonClickedEvent>.Subscribe(e =>
{
    Console.WriteLine($"Button {e.ButtonId} clicked at {e.ClickTime}");
});

// Publish
EventChannel<ButtonClickedEvent>.Publish(new ButtonClickedEvent
{
    ButtonId = "StartGame",
    ClickTime = Time.time
});
```

## Advanced Patterns

### RAII Subscription

```csharp
public class MyFeature : IFeature
{
    private IDisposable _subscription;
    
    public void Initialize()
    {
        _subscription = EventChannel<MyEvent>.Subscribe(OnMyEvent);
    }
    
    public void Shutdown()
    {
        _subscription?.Dispose(); // Auto-unsubscribe
    }
    
    private void OnMyEvent(MyEvent e)
    {
        // Handle event
    }
}
```

### Multiple Subscriptions

```csharp
public class EventLogger
{
    private List<IDisposable> _subscriptions = new();
    
    public void Start()
    {
        _subscriptions.Add(EventChannel<FeatureInitializedEvent>.Subscribe(LogInit));
        _subscriptions.Add(EventChannel<FeatureShutdownEvent>.Subscribe(LogShutdown));
        _subscriptions.Add(EventChannel<DamageDealtEvent>.Subscribe(LogDamage));
    }
    
    public void Stop()
    {
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();
    }
    
    private void LogInit(FeatureInitializedEvent e) => Log($"Init: {e.Name}");
    private void LogShutdown(FeatureShutdownEvent e) => Log($"Shutdown: {e.Name}");
    private void LogDamage(DamageDealtEvent e) => Log($"Damage: {e.Damage}");
}
```

### Event Filtering

```csharp
EventChannel<DamageDealtEvent>.Subscribe(e =>
{
    // Only handle critical hits
    if (e.IsCritical)
    {
        PlayCriticalHitEffect();
    }
});

EventChannel<DamageDealtEvent>.Subscribe(e =>
{
    // Only handle high damage
    if (e.Damage > 100)
    {
        ShowHighDamageWarning();
    }
});
```

### Event Chaining

```csharp
// First handler processes damage
EventChannel<DamageDealtEvent>.Subscribe(e =>
{
    ApplyDamage(e.TargetId, e.Damage);
    
    // Chain to death event if needed
    if (GetHealth(e.TargetId) <= 0)
    {
        EventChannel<EntityDiedEvent>.Publish(new EntityDiedEvent
        {
            EntityId = e.TargetId,
            KillerId = e.AttackerId
        });
    }
});

// Second handler handles death
EventChannel<EntityDiedEvent>.Subscribe(e =>
{
    RemoveEntity(e.EntityId);
    AwardKillPoints(e.KillerId);
});
```

## Thread Safety

EventChannel is thread-safe using lock-based synchronization:

```csharp
// Safe to call from any thread
Task.Run(() =>
{
    EventChannel<MyEvent>.Publish(new MyEvent { Data = "from thread" });
});
```

## Migration from EventBus

### Step 1: Convert Events to Structs

```csharp
// Before (EventBus)
public class MyEvent
{
    public string Data { get; set; }
}

// After (EventChannel)
public struct MyEvent
{
    public string Data { get; set; }
}
```

### Step 2: Update Subscribe Calls

```csharp
// Before
EventBus.Instance.Subscribe<MyEvent>(OnMyEvent);

// After
EventChannel<MyEvent>.Subscribe(OnMyEvent);
```

### Step 3: Update Publish Calls

```csharp
// Before
EventBus.Instance.Publish(new MyEvent { Data = "test" });

// After
EventChannel<MyEvent>.Publish(new MyEvent { Data = "test" });
```

### Step 4: Update Unsubscribe

```csharp
// Before
EventBus.Instance.Unsubscribe<MyEvent>(OnMyEvent);

// After
EventChannel<MyEvent>.Unsubscribe(OnMyEvent);
// Or use RAII pattern with IDisposable
```

## Built-in Events

Nexus v2.0 provides these standard events:

### FeatureRegisteredEvent

```csharp
public struct FeatureRegisteredEvent
{
    public string Name { get; set; }
}
```

Published when a feature is registered with FeatureRegistry.

### FeatureInitializedEvent

```csharp
public struct FeatureInitializedEvent
{
    public string Name { get; set; }
}
```

Published when a feature completes initialization.

### FeatureShutdownEvent

```csharp
public struct FeatureShutdownEvent
{
    public string Name { get; set; }
}
```

Published when a feature is shutting down.

## Testing

### Unit Tests

```csharp
[Test]
public void Publish_NotifiesSubscribers()
{
    bool received = false;
    MyEvent receivedEvent = default;
    
    EventChannel<MyEvent>.Subscribe(e =>
    {
        received = true;
        receivedEvent = e;
    });
    
    EventChannel<MyEvent>.Publish(new MyEvent { Data = "test" });
    
    Assert.That(received, Is.True);
    Assert.That(receivedEvent.Data, Is.EqualTo("test"));
}

[Test]
public void Unsubscribe_StopsNotifications()
{
    int count = 0;
    void Handler(MyEvent e) => count++;
    
    EventChannel<MyEvent>.Subscribe(Handler);
    EventChannel<MyEvent>.Publish(new MyEvent());
    Assert.That(count, Is.EqualTo(1));
    
    EventChannel<MyEvent>.Unsubscribe(Handler);
    EventChannel<MyEvent>.Publish(new MyEvent());
    Assert.That(count, Is.EqualTo(1)); // Still 1, not 2
}

[Test]
public void Subscription_Dispose_Unsubscribes()
{
    int count = 0;
    
    var subscription = EventChannel<MyEvent>.Subscribe(e => count++);
    EventChannel<MyEvent>.Publish(new MyEvent());
    Assert.That(count, Is.EqualTo(1));
    
    subscription.Dispose();
    EventChannel<MyEvent>.Publish(new MyEvent());
    Assert.That(count, Is.EqualTo(1)); // Still 1
}
```

### Test Lab

Godot Test Lab'de "Test EventChannel" butonuna tıklayarak EventChannel sistemini test edebilirsiniz.

## Best Practices

1. **Use Structs**: Event'leri her zaman struct olarak tanımlayın
2. **Keep Events Small**: Sadece gerekli data'yı içerin
3. **Use RAII**: IDisposable pattern ile auto-unsubscribe kullanın
4. **Avoid Circular Events**: Event chain'leri sonsuz döngüye girmesin
5. **Document Events**: Event struct'larına XML documentation ekleyin
6. **Test Events**: Event publish/subscribe'ı unit test'lerde test edin

## Common Pitfalls

### ❌ Using Classes

```csharp
// Wrong - creates garbage
public class MyEvent { }
```

### ✅ Using Structs

```csharp
// Correct - zero allocation
public struct MyEvent { }
```

### ❌ Forgetting to Unsubscribe

```csharp
// Memory leak - handler never removed
EventChannel<MyEvent>.Subscribe(e => { });
```

### ✅ Using RAII

```csharp
// Correct - auto-unsubscribe
using var sub = EventChannel<MyEvent>.Subscribe(e => { });
```

### ❌ Circular Events

```csharp
// Infinite loop!
EventChannel<EventA>.Subscribe(e =>
{
    EventChannel<EventB>.Publish(new EventB());
});

EventChannel<EventB>.Subscribe(e =>
{
    EventChannel<EventA>.Publish(new EventA());
});
```

### ✅ Event Chaining with Guards

```csharp
// Safe - has termination condition
EventChannel<DamageEvent>.Subscribe(e =>
{
    if (GetHealth(e.TargetId) <= 0) // Guard
    {
        EventChannel<DeathEvent>.Publish(new DeathEvent());
    }
});
```

## Performance Tips

1. **Reuse Event Instances**: Create event once, publish multiple times
2. **Avoid Boxing**: Don't cast struct events to object
3. **Minimize Subscribers**: Too many subscribers slow down publish
4. **Use Local Variables**: Capture locals instead of fields when possible

## Debugging

### Enable Event Logging

```csharp
EventChannel<MyEvent>.Subscribe(e =>
{
    Debug.Log($"Event received: {e}");
});
```

### Count Subscribers

```csharp
// Add to EventChannel<T> for debugging
public static int SubscriberCount => _subscribers.Count;
```

### Track Event Flow

```csharp
public class EventTracker
{
    public void TrackAll()
    {
        EventChannel<FeatureInitializedEvent>.Subscribe(e => 
            Log($"[{DateTime.Now}] Feature Init: {e.Name}"));
        
        EventChannel<DamageDealtEvent>.Subscribe(e => 
            Log($"[{DateTime.Now}] Damage: {e.Damage}"));
    }
}
```

## Related Files

- `EventChannel.cs` - Main implementation
- `EventSubscription.cs` - RAII subscription
- `FeatureEvents.cs` - Built-in events
- `EventChannelTests.cs` - Unit tests

## See Also

- [EventBus (Legacy)](README_EVENTBUS.md)
- [Feature Lifecycle](README_FEATURES.md)
- [Performance Guide](../../docs/PERFORMANCE.md)
