using System;
using System.Collections.Generic;

namespace Virabis.Core.Nexus;

/// <summary>
/// Zero-allocation, type-safe event channel for struct-based events.
/// Thread-safe implementation with test isolation support.
/// </summary>
/// <typeparam name="TEvent">Event type (must be struct)</typeparam>
/// <remarks>
/// v3.0 Improvements:
/// - ClearForTest() for test isolation
/// - SubscriberCount for diagnostics
/// - TrySubscribe for safe subscription
/// </remarks>
public static class EventChannel<TEvent> where TEvent : struct
{
    private static readonly List<Action<TEvent>> _subscribers = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the current number of subscribers. Useful for diagnostics.
    /// </summary>
    public static int SubscriberCount
    {
        get
        {
            lock (_lock)
            {
                return _subscribers.Count;
            }
        }
    }

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <param name="evt">Event to publish</param>
    public static void Publish(TEvent evt)
    {
        lock (_lock)
        {
            // Create a copy to avoid modification during iteration
            var subscribersCopy = _subscribers.ToArray();
            foreach (var subscriber in subscribersCopy)
            {
                subscriber(evt);
            }
        }
    }

    /// <summary>
    /// Subscribes to events of this type.
    /// </summary>
    /// <param name="handler">Handler to invoke on event</param>
    /// <returns>IDisposable for automatic unsubscription</returns>
    public static IDisposable Subscribe(Action<TEvent> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            _subscribers.Add(handler);
        }
        return new EventSubscription<TEvent>(handler);
    }

    /// <summary>
    /// Tries to subscribe. Returns null if handler is null.
    /// Useful for conditional subscriptions.
    /// </summary>
    public static IDisposable? TrySubscribe(Action<TEvent>? handler)
    {
        if (handler == null)
            return null;
        return Subscribe(handler);
    }

    /// <summary>
    /// Unsubscribes a handler from events.
    /// </summary>
    /// <param name="handler">Handler to unsubscribe</param>
    public static void Unsubscribe(Action<TEvent> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        lock (_lock)
        {
            _subscribers.Remove(handler);
        }
    }

    /// <summary>
    /// Clears all subscribers. USE ONLY IN TESTS!
    /// This method exists for test isolation.
    /// </summary>
    /// <remarks>
    /// WARNING: This will break production code if called outside tests!
    /// Always use [SetUp] and [TearDown] to ensure clean state.
    /// </remarks>
    public static void ClearForTest()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }
    }
}

public class EventSubscription<TEvent> : IDisposable where TEvent : struct
{
    private readonly Action<TEvent> _handler;
    private bool _disposed;
    
    public EventSubscription(Action<TEvent> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            EventChannel<TEvent>.Unsubscribe(_handler);
            _disposed = true;
        }
    }
}
