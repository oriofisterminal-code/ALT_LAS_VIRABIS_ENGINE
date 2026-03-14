using Xunit;
using Virabis.Core.Events;

namespace Virabis.Core.Tests;

/// <summary>
/// Tests for Event Bus System.
/// "Herkes duymalı, hiçbir şey kaçmamalı" - Can Hızlı (Taksi Şoförü)
/// </summary>
public class EventBusTests
{
    public EventBusTests()
    {
        Events.Reset();
    }

    // ========================================
    // BASIC SUBSCRIBE/PUBLISH
    // ========================================

    [Fact, Trait("Category", "Smoke")]
    public void EventBus_Subscribe_Publish_ReceivesEvent()
    {
        var bus = new EventBus();
        var received = false;

        bus.Subscribe<TestEvent>(e => received = true);
        bus.Publish(new TestEvent());

        Assert.True(received);
    }

    [Fact, Trait("Category", "Unit")]
    public void EventBus_MultipleSubscribers_AllReceiveEvent()
    {
        var bus = new EventBus();
        var count = 0;

        bus.Subscribe<TestEvent>(e => count++);
        bus.Subscribe<TestEvent>(e => count++);
        bus.Subscribe<TestEvent>(e => count++);

        bus.Publish(new TestEvent());

        Assert.Equal(3, count);
    }

    [Fact, Trait("Category", "Unit")]
    public void EventBus_PublishAndCheck_ReturnsTrueWhenHandled()
    {
        var bus = new EventBus();
        bus.Subscribe<TestEvent>(e => { });

        var result = bus.PublishAndCheck(new TestEvent());

        Assert.True(result);
    }

    [Fact, Trait("Category", "Unit")]
    public void EventBus_PublishAndCheck_ReturnsFalseWhenNoHandlers()
    {
        var bus = new EventBus();

        var result = bus.PublishAndCheck(new TestEvent());

        Assert.False(result);
    }

    // ========================================
    // UNSUBSCRIBE
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void EventBus_Unsubscribe_StopsReceiving()
    {
        var bus = new EventBus();
        var count = 0;

        var subscription = bus.Subscribe<TestEvent>(e => count++);
        bus.Publish(new TestEvent());
        Assert.Equal(1, count);

        bus.Unsubscribe(subscription);
        bus.Publish(new TestEvent());

        Assert.Equal(1, count); // Still 1, not 2
    }

    [Fact, Trait("Category", "Unit")]
    public void EventSubscription_Dispose_Unsubscribes()
    {
        var bus = new EventBus();
        var count = 0;

        using (bus.Subscribe<TestEvent>(e => count++))
        {
            bus.Publish(new TestEvent());
        }

        bus.Publish(new TestEvent());

        Assert.Equal(1, count);
    }

    // ========================================
    // FILTERING
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void EventBus_Filter_OnlyReceivesMatchingEvents()
    {
        var bus = new EventBus();
        var received = new List<int>();

        bus.Subscribe<ValueEvent>(e => received.Add(e.Value), e => e.Value > 5);

        bus.Publish(new ValueEvent(3));
        bus.Publish(new ValueEvent(7));
        bus.Publish(new ValueEvent(2));
        bus.Publish(new ValueEvent(10));

        Assert.Equal(new[] { 7, 10 }, received);
    }

    // ========================================
    // PRIORITY
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void EventBus_Priority_HigherPriorityCalledFirst()
    {
        var bus = new EventBus();
        var order = new List<int>();

        bus.Subscribe<ValueEvent>(e => order.Add(1), priority: 0);
        bus.Subscribe<ValueEvent>(e => order.Add(2), priority: 10);
        bus.Subscribe<ValueEvent>(e => order.Add(3), priority: 5);

        bus.Publish(new ValueEvent(1));

        Assert.Equal(new[] { 2, 3, 1 }, order); // Priority 10, 5, 0
    }

    // ========================================
    // CLEAR
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void EventBus_ClearSubscriptions_RemovesAllForType()
    {
        var bus = new EventBus();
        var count = 0;

        bus.Subscribe<TestEvent>(e => count++);
        bus.Subscribe<TestEvent>(e => count++);

        bus.ClearSubscriptions<TestEvent>();
        bus.Publish(new TestEvent());

        Assert.Equal(0, count);
    }

    [Fact, Trait("Category", "Unit")]
    public void EventBus_ClearAllSubscriptions_RemovesAll()
    {
        var bus = new EventBus();
        var count = 0;

        bus.Subscribe<TestEvent>(e => count++);
        bus.Subscribe<ValueEvent>(e => count++);

        bus.ClearAllSubscriptions();
        bus.Publish(new TestEvent());
        bus.Publish(new ValueEvent(1));

        Assert.Equal(0, count);
    }

    // ========================================
    // GLOBAL EVENTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Events_Global_PublishAndSubscribe()
    {
        var received = false;

        using (Events.Subscribe<TestEvent>(e => received = true))
        {
            Events.Publish(new TestEvent());
        }

        Assert.True(received);
    }

    // ========================================
    // TEST EVENTS
    // ========================================

    private class TestEvent : BaseEvent
    {
        public TestEvent(object? source = null) : base(source) { }
    }

    private class ValueEvent : BaseEvent
    {
        public int Value { get; }

        public ValueEvent(int value, object? source = null) : base(source)
        {
            Value = value;
        }
    }
}

/// <summary>
/// Integration tests for Event Bus with game events.
/// </summary>
public class GameEventTests
{
    public GameEventTests()
    {
        Events.Reset();
    }

    [Fact, Trait("Category", "Integration")]
    public void DamageTakenEvent_Published_OnApplyDamage()
    {
        var entity = new Entity(TeamId.Enemy, 100f);
        DamageTakenEvent? received = null;

        using (Events.Subscribe<DamageTakenEvent>(e => received = e))
        {
            entity.Health.ApplyDamage(25f);
        }

        Assert.NotNull(received);
        Assert.Equal(entity, received!.Target);
        Assert.Equal(25f, received.Damage);
        Assert.Equal(75f, received.CurrentHealth);
    }

    [Fact, Trait("Category", "Integration")]
    public void DeathEvent_Published_WhenHealthReachesZero()
    {
        var entity = new Entity(TeamId.Enemy, 100f);
        DeathEvent? received = null;

        using (Events.Subscribe<DeathEvent>(e => received = e))
        {
            entity.Health.ApplyDamage(100f);
        }

        Assert.NotNull(received);
        Assert.Equal(entity, received!.Entity);
    }

    [Fact, Trait("Category", "Integration")]
    public void HealedEvent_Published_OnHeal()
    {
        var entity = new Entity(TeamId.Player, 100f);
        entity.Health.ApplyDamage(50f); // Down to 50
        HealedEvent? received = null;

        using (Events.Subscribe<HealedEvent>(e => received = e))
        {
            entity.Health.Heal(25f);
        }

        Assert.NotNull(received);
        Assert.Equal(25f, received!.Amount);
        Assert.Equal(75f, received.CurrentHealth);
    }

    [Fact, Trait("Category", "Integration")]
    public void StateChangedEvent_Published_OnTransition()
    {
        var entity = new Entity(TeamId.Player, 100f);
        entity.InitializeCombatantStates();
        StateChangedEvent? received = null;

        using (Events.Subscribe<StateChangedEvent>(e => received = e))
        {
            entity.TryChangeState(EntityStates.Attacking);
        }

        Assert.NotNull(received);
        Assert.Equal("Idle", received!.PreviousState);
        Assert.Equal("Attacking", received.NewState);
    }

    [Fact, Trait("Category", "Integration")]
    public void MultipleSystems_CanReactToSameEvent()
    {
        var entity = new Entity(TeamId.Enemy, 100f);
        var achievementFired = false;
        var statsUpdated = false;
        var uiUpdated = false;

        using (Events.Subscribe<DamageTakenEvent>(e => achievementFired = true))
        using (Events.Subscribe<DamageTakenEvent>(e => statsUpdated = true))
        using (Events.Subscribe<DamageTakenEvent>(e => uiUpdated = true))
        {
            entity.Health.ApplyDamage(10f);
        }

        Assert.True(achievementFired);
        Assert.True(statsUpdated);
        Assert.True(uiUpdated);
    }
}
