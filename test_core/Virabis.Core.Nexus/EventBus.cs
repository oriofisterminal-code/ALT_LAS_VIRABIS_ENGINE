namespace Virabis.Core.Nexus;

/// <summary>
/// Simple event bus for feature communication.
/// Singleton pattern - use EventBus.Instance.
/// Thread-safe implementation with weak references to prevent memory leaks.
/// </summary>
public sealed class EventBus
{
    private static readonly Lazy<EventBus> _instance = 
        new(() => new EventBus());

    /// <summary>
    /// Gets the singleton instance of the EventBus.
    /// </summary>
    public static EventBus Instance => _instance.Value;

    private readonly Dictionary<Type, List<WeakReference>> _subscribers;
    private readonly object _lock = new();

    private EventBus()
    {
        _subscribers = new Dictionary<Type, List<WeakReference>>();
    }

    /// <summary>
    /// Subscribe to an event type.
    /// Uses weak reference to prevent memory leaks - subscribers can be
    /// garbage collected even if still subscribed.
    /// </summary>
    /// <typeparam name="T">Event type to subscribe to</typeparam>
    /// <param name="handler">Event handler delegate</param>
    /// <exception cref="ArgumentNullException">If handler is null</exception>
    /// <example>
    /// EventBus.Instance.Subscribe&lt;FeatureEnabledEvent&gt;(e => 
    ///     Console.WriteLine($"Feature {e.FeatureName} enabled"));
    /// </example>
    public void Subscribe<T>(Action<T> handler) where T : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<WeakReference>();
            }

            _subscribers[eventType].Add(new WeakReference(handler));
        }
    }

    /// <summary>
    /// Publish an event to all subscribers.
    /// Dead subscribers (garbage collected) are automatically cleaned up.
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="event">Event instance to publish</param>
    /// <exception cref="ArgumentNullException">If event is null</exception>
    /// <example>
    /// EventBus.Instance.Publish(new FeatureEnabledEvent("Combat"));
    /// </example>
    public void Publish<T>(T @event) where T : class
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_subscribers.ContainsKey(eventType))
                return;

            var deadReferences = new List<WeakReference>();

            foreach (var weakRef in _subscribers[eventType])
            {
                if (!weakRef.IsAlive)
                {
                    deadReferences.Add(weakRef);
                    continue;
                }

                var handler = weakRef.Target as Action<T>;
                try
                {
                    handler?.Invoke(@event);
                }
                catch (Exception ex)
                {
                    // Log but don't fail - one bad subscriber shouldn't break others
                    Console.WriteLine($"[EventBus] Handler exception for {eventType.Name}: {ex.Message}");
                }
            }

            // Clean up dead references
            foreach (var deadRef in deadReferences)
            {
                _subscribers[eventType].Remove(deadRef);
            }

            // Remove empty subscriber lists
            if (_subscribers[eventType].Count == 0)
            {
                _subscribers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// Unsubscribe from an event type.
    /// Note: Due to weak references, explicit unsubscribe is optional.
    /// Subscribers will be cleaned up automatically when garbage collected.
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="handler">Handler to unsubscribe</param>
    public void Unsubscribe<T>(Action<T> handler) where T : class
    {
        if (handler == null)
            return;

        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_subscribers.ContainsKey(eventType))
                return;

            _subscribers[eventType].RemoveAll(wr => 
                !wr.IsAlive || ReferenceEquals(wr.Target, handler));

            if (_subscribers[eventType].Count == 0)
            {
                _subscribers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// Clear all subscribers for all event types.
    /// Useful for testing or complete system reset.
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }
    }

    /// <summary>
    /// Get subscriber count for an event type.
    /// Useful for debugging and monitoring.
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <returns>Number of active subscribers</returns>
    public int GetSubscriberCount<T>() where T : class
    {
        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_subscribers.ContainsKey(eventType))
                return 0;

            // Count only alive references
            return _subscribers[eventType].Count(wr => wr.IsAlive);
        }
    }
}
