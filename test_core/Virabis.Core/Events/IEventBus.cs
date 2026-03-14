namespace Virabis.Core.Events;

/// <summary>
/// Marker interface for all events.
/// All events must implement this interface.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Optional source that raised this event.
    /// </summary>
    object? Source { get; }
}

/// <summary>
/// Base class for all events with common properties.
/// </summary>
public abstract class BaseEvent : IEvent
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public object? Source { get; init; }

    protected BaseEvent(object? source = null)
    {
        Source = source;
    }
}

/// <summary>
/// Represents an event subscription handle for unsubscribe.
/// </summary>
public readonly struct EventSubscription : IDisposable
{
    private readonly Action? _unsubscribe;

    public Guid Id { get; }
    public bool IsActive => _unsubscribe != null;

    internal EventSubscription(Guid id, Action unsubscribe)
    {
        Id = id;
        _unsubscribe = unsubscribe;
    }

    public void Dispose()
    {
        _unsubscribe?.Invoke();
    }

    public static EventSubscription None => new(Guid.Empty, null!);
}

/// <summary>
/// Event handler delegate.
/// </summary>
public delegate void EventHandler<in TEvent>(TEvent e) where TEvent : IEvent;

/// <summary>
/// Filter predicate for event handlers.
/// </summary>
public delegate bool EventFilter<in TEvent>(TEvent e) where TEvent : IEvent;

/// <summary>
/// Interface for the event bus system.
/// Enables decoupled communication between systems.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribes to an event type.
    /// </summary>
    EventSubscription Subscribe<TEvent>(EventHandler<TEvent> handler)
        where TEvent : IEvent;

    /// <summary>
    /// Subscribes to an event type with a filter.
    /// </summary>
    EventSubscription Subscribe<TEvent>(EventHandler<TEvent> handler, EventFilter<TEvent> filter)
        where TEvent : IEvent;

    /// <summary>
    /// Subscribes to an event type with priority.
    /// Higher priority handlers are called first.
    /// </summary>
    EventSubscription Subscribe<TEvent>(EventHandler<TEvent> handler, int priority)
        where TEvent : IEvent;

    /// <summary>
    /// Unsubscribes from an event type.
    /// </summary>
    void Unsubscribe<TEvent>(EventHandler<TEvent> handler)
        where TEvent : IEvent;

    /// <summary>
    /// Unsubscribes using subscription handle.
    /// </summary>
    void Unsubscribe(EventSubscription subscription);

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    void Publish<TEvent>(TEvent e) where TEvent : IEvent;

    /// <summary>
    /// Publishes an event and returns true if any handler processed it.
    /// </summary>
    bool PublishAndCheck<TEvent>(TEvent e) where TEvent : IEvent;

    /// <summary>
    /// Clears all subscriptions for an event type.
    /// </summary>
    void ClearSubscriptions<TEvent>() where TEvent : IEvent;

    /// <summary>
    /// Clears all subscriptions.
    /// </summary>
    void ClearAllSubscriptions();

    /// <summary>
    /// Gets the number of subscribers for an event type.
    /// </summary>
    int GetSubscriberCount<TEvent>() where TEvent : IEvent;
}
