using Xunit;
using Virabis.Core.Nexus.Events;

namespace Virabis.Core.Nexus;

/// <summary>
/// Base class for all Nexus tests providing test isolation.
/// Automatically clears static EventChannel state before and after each test.
/// </summary>
/// <remarks>
/// v3.0: Added to solve static state test isolation issues.
///
/// Problem: EventChannel uses static subscriber lists that persist between tests.
/// Solution: Clear all event channels before and after each test.
///
/// Usage:
/// <code>
/// public class MyTests : TestBase
/// {
///     [Fact]
///     public void MyTest()
///     {
///         // EventChannel state is guaranteed clean
///     }
/// }
/// </code>
///
/// Note: If you add new event types, add them to ClearAllEventChannels().
/// </remarks>
public abstract class TestBase : IDisposable
{
    protected TestBase()
    {
        ClearAllEventChannels();
    }

    public void Dispose()
    {
        ClearAllEventChannels();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Clears all known event channel types.
    /// Add new event types here when they are created.
    /// </summary>
    protected virtual void ClearAllEventChannels()
    {
        // Core Nexus events
        EventChannel<FeatureRegisteredEvent>.ClearForTest();
        EventChannel<FeatureInitializedEvent>.ClearForTest();
        EventChannel<FeatureShutdownEvent>.ClearForTest();
        EventChannel<FeatureEnabledEvent>.ClearForTest();
        EventChannel<FeatureDisabledEvent>.ClearForTest();
        EventChannel<HealthStatusChangedEvent>.ClearForTest();
    }

    /// <summary>
    /// Clears a specific event channel type.
    /// Use this when you only need to clear one type.
    /// </summary>
    protected static void ClearChannel<TEvent>() where TEvent : struct
    {
        EventChannel<TEvent>.ClearForTest();
    }

    /// <summary>
    /// Asserts that no event channel subscribers exist.
    /// Useful for detecting subscriber leaks in tests.
    /// </summary>
    protected static void AssertNoSubscribers<TEvent>() where TEvent : struct
    {
        Assert.Equal(0, EventChannel<TEvent>.SubscriberCount);
    }

    /// <summary>
    /// Asserts the expected number of subscribers for an event type.
    /// </summary>
    protected static void AssertSubscriberCount<TEvent>(int expected) where TEvent : struct
    {
        Assert.Equal(expected, EventChannel<TEvent>.SubscriberCount);
    }
}
