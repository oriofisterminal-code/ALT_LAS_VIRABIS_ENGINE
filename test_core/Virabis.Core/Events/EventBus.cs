using System.Collections.Concurrent;

namespace Virabis.Core.Events;

/// <summary>
/// Thread-safe event bus implementation.
/// "Haberler hızlı yayılsın, herkes duysun" - Can Hızlı (Taksi Şoförü)
/// </summary>
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, SubscriptionList> _subscriptions = new();
    private readonly ConcurrentDictionary<Guid, (Type EventType, Guid SubscriptionId)> _subscriptionIndex = new();
    private readonly bool _enableLogging;
    private int _subscriptionCounter;

    /// <summary>
    /// Creates a new EventBus instance.
    /// </summary>
    public EventBus(bool enableLogging = false)
    {
        _enableLogging = enableLogging;
    }

    // ========================================
    // SUBSCRIBE
    // ========================================

    public EventSubscription Subscribe<TEvent>(EventHandler<TEvent> handler)
        where TEvent : IEvent
    {
        return SubscribeInternal(handler, 0, null);
    }

    public EventSubscription Subscribe<TEvent>(EventHandler<TEvent> handler, EventFilter<TEvent> filter)
        where TEvent : IEvent
    {
        return SubscribeInternal(handler, 0, filter);
    }

    public EventSubscription Subscribe<TEvent>(EventHandler<TEvent> handler, int priority)
        where TEvent : IEvent
    {
        return SubscribeInternal(handler, priority, null);
    }

    private EventSubscription SubscribeInternal<TEvent>(
        EventHandler<TEvent> handler,
        int priority,
        EventFilter<TEvent>? filter)
        where TEvent : IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var subscriptionId = Guid.NewGuid();
        var eventType = typeof(TEvent);
        var list = _subscriptions.GetOrAdd(eventType, _ => new SubscriptionList());

        var subscription = new Subscription
        {
            Id = subscriptionId,
            Handler = e => handler((TEvent)e),
            Filter = filter != null ? e => filter((TEvent)e) : null,
            Priority = priority
        };

        list.Add(subscription);
        _subscriptionIndex[subscriptionId] = (eventType, subscriptionId);

        Log($"Subscribed to {eventType.Name} (Id: {subscriptionId})");

        return new EventSubscription(subscriptionId, () => UnsubscribeById(subscriptionId));
    }

    // ========================================
    // UNSUBSCRIBE
    // ========================================

    public void Unsubscribe<TEvent>(EventHandler<TEvent> handler)
        where TEvent : IEvent
    {
        if (handler == null) return;

        var eventType = typeof(TEvent);
        if (_subscriptions.TryGetValue(eventType, out var list))
        {
            list.Remove(handler);
            Log($"Unsubscribed handler from {eventType.Name}");
        }
    }

    public void Unsubscribe(EventSubscription subscription)
    {
        if (!subscription.IsActive)
            return;

        UnsubscribeById(subscription.Id);
    }

    private void UnsubscribeById(Guid subscriptionId)
    {
        if (_subscriptionIndex.TryRemove(subscriptionId, out var info))
        {
            if (_subscriptions.TryGetValue(info.EventType, out var list))
            {
                list.Remove(subscriptionId);
            }
            Log($"Unsubscribed by handle (Id: {subscriptionId})");
        }
    }

    // ========================================
    // PUBLISH
    // ========================================

    public void Publish<TEvent>(TEvent e)
        where TEvent : IEvent
    {
        PublishAndCheck(e);
    }

    public bool PublishAndCheck<TEvent>(TEvent e)
        where TEvent : IEvent
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        var eventType = typeof(TEvent);
        var hasHandlers = false;

        if (_subscriptions.TryGetValue(eventType, out var list))
        {
            var handlers = list.GetHandlers();
            foreach (var subscription in handlers)
            {
                try
                {
                    if (subscription.TryHandle(e))
                    {
                        hasHandlers = true;
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error in handler for {eventType.Name}: {ex.Message}");
#if DEBUG
                    throw;
#endif
                }
            }
        }

        Log($"Published {eventType.Name} (handlers: {hasHandlers})");
        return hasHandlers;
    }

    // ========================================
    // CLEAR
    // ========================================

    public void ClearSubscriptions<TEvent>()
        where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        if (_subscriptions.TryRemove(eventType, out var list))
        {
            foreach (var sub in list.GetHandlers())
            {
                _subscriptionIndex.TryRemove(sub.Id, out _);
            }
        }
        Log($"Cleared subscriptions for {eventType.Name}");
    }

    public void ClearAllSubscriptions()
    {
        _subscriptions.Clear();
        _subscriptionIndex.Clear();
        Log("Cleared all subscriptions");
    }

    // ========================================
    // QUERY
    // ========================================

    public int GetSubscriberCount<TEvent>()
        where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        return _subscriptions.TryGetValue(eventType, out var list) ? list.Count : 0;
    }

    // ========================================
    // INTERNAL METHODS
    // ========================================

    internal void ClearAllSubscriptions()
    {
        _subscriptions.Clear();
        _subscriptionIndex.Clear();
    }

    // ========================================
    // LOGGING
    // ========================================

    private void Log(string message)
    {
        if (_enableLogging)
        {
            Console.WriteLine($"[EventBus] {DateTime.Now:HH:mm:ss.fff} {message}");
        }
    }

    // ========================================
    // INNER CLASSES
    // ========================================

    private class SubscriptionList
    {
        private readonly List<Subscription> _subscriptions = new();
        private readonly object _lock = new();
        private bool _needsSort;

        public int Count
        {
            get
            {
                lock (_lock) return _subscriptions.Count;
            }
        }

        public void Add(Subscription subscription)
        {
            lock (_lock)
            {
                _subscriptions.Add(subscription);
                _needsSort = true;
            }
        }

        public void Remove(Guid id)
        {
            lock (_lock)
            {
                _subscriptions.RemoveAll(s => s.Id == id);
            }
        }

        public void Remove(Delegate handler)
        {
            lock (_lock)
            {
                _subscriptions.RemoveAll(s => s.Handler == handler);
            }
        }

        public IReadOnlyList<Subscription> GetHandlers()
        {
            lock (_lock)
            {
                if (_needsSort)
                {
                    _subscriptions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                    _needsSort = false;
                }
                return _subscriptions.ToList();
            }
        }
    }

    private class Subscription
    {
        public Guid Id { get; init; }
        public Action<IEvent>? Handler { get; init; }
        public Func<IEvent, bool>? Filter { get; init; }
        public int Priority { get; init; }

        public bool TryHandle(IEvent e)
        {
            if (Handler == null) return false;

            // Check filter first
            if (Filter != null && !Filter(e))
                return false;

            Handler(e);
            return true;
        }
    }
}
