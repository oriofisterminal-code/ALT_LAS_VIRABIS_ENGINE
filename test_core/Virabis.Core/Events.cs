using Virabis.Core.Events;

namespace Virabis.Core;

/// <summary>
/// Static event system for Virabis Core.
/// Provides global access to the Event Bus.
/// </summary>
public static class Events
{
    private static IEventBus? _instance;

    /// <summary>
    /// Gets or sets the global event bus instance.
    /// </summary>
    public static IEventBus Bus => _instance ??= new EventBus();

    /// <summary>
    /// Sets a custom event bus instance.
    /// </summary>
    public static void Initialize(IEventBus bus)
    {
        _instance = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    /// <summary>
    /// Resets the event bus (clears all subscriptions).
    /// </summary>
    public static void Reset()
    {
        _instance?.ClearAllSubscriptions();
        _instance = null;
    }

    // ========================================
    // CONVENIENCE METHODS
    // ========================================

    /// <summary>
    /// Subscribes to an event type.
    /// </summary>
    public static EventSubscription Subscribe<TEvent>(EventHandler<TEvent> handler)
        where TEvent : IEvent => Bus.Subscribe(handler);

    /// <summary>
    /// Subscribes to an event type with a filter.
    /// </summary>
    public static EventSubscription Subscribe<TEvent>(EventHandler<TEvent> handler, EventFilter<TEvent> filter)
        where TEvent : IEvent => Bus.Subscribe(handler, filter);

    /// <summary>
    /// Subscribes to an event type with priority.
    /// </summary>
    public static EventSubscription Subscribe<TEvent>(EventHandler<TEvent> handler, int priority)
        where TEvent : IEvent => Bus.Subscribe(handler, priority);

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    public static void Publish<TEvent>(TEvent e)
        where TEvent : IEvent => Bus.Publish(e);

    /// <summary>
    /// Publishes an event and returns true if any handler processed it.
    /// </summary>
    public static bool PublishAndCheck<TEvent>(TEvent e)
        where TEvent : IEvent => Bus.PublishAndCheck(e);

    /// <summary>
    /// Unsubscribes using subscription handle.
    /// </summary>
    public static void Unsubscribe(EventSubscription subscription)
        => Bus.Unsubscribe(subscription);

    /// <summary>
    /// Gets the number of subscribers for an event type.
    /// </summary>
    public static int GetSubscriberCount<TEvent>()
        where TEvent : IEvent => Bus.GetSubscriberCount<TEvent>();

    /// <summary>
    /// Clears all subscriptions for an event type.
    /// </summary>
    public static void ClearSubscriptions<TEvent>()
        where TEvent : IEvent => Bus.ClearSubscriptions<TEvent>();
}

/// <summary>
/// Extension methods for event handling.
/// </summary>
public static class EventExtensions
{
    /// <summary>
    /// Subscribes and returns the subscription (using pattern).
    /// </summary>
    public static EventSubscription On<TEvent>(this IEventBus bus, EventHandler<TEvent> handler)
        where TEvent : IEvent
        => bus.Subscribe(handler);

    /// <summary>
    /// Subscribes with filter (only called when filter returns true).
    /// </summary>
    public static EventSubscription On<TEvent>(this IEventBus bus, EventHandler<TEvent> handler, Func<TEvent, bool> filter)
        where TEvent : IEvent
        => bus.Subscribe(handler, e => filter(e));

    /// <summary>
    /// Subscribes once (automatically unsubscribes after first call).
    /// </summary>
    public static EventSubscription Once<TEvent>(this IEventBus bus, EventHandler<TEvent> handler)
        where TEvent : IEvent
    {
        EventSubscription subscription = default;
        EventHandler<TEvent> wrapper = e =>
        {
            bus.Unsubscribe(subscription);
            handler(e);
        };
        subscription = bus.Subscribe(wrapper);
        return subscription;
    }
}
