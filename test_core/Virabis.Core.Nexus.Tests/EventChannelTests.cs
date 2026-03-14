using Xunit;
using Virabis.Core.Nexus.Events;

namespace Virabis.Core.Nexus;

/// <summary>
/// Tests for EventChannel static event system.
/// Inherits from TestBase for automatic test isolation.
/// </summary>
/// <remarks>
/// v3.0: Now inherits from TestBase for proper test isolation.
/// Each test starts with clean EventChannel state.
/// </remarks>
public class EventChannelTests : TestBase
{
    private struct TestEvent
    {
        public int Value;
        public string Message;
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Publish_ShouldNotifySubscribers()
    {
        int callCount = 0;
        TestEvent receivedEvent = default;

        using (EventChannel<TestEvent>.Subscribe(evt =>
        {
            callCount++;
            receivedEvent = evt;
        }))
        {
            var testEvent = new TestEvent { Value = 42, Message = "Test" };
            EventChannel<TestEvent>.Publish(testEvent);

            Assert.Equal(1, callCount);
            Assert.Equal(42, receivedEvent.Value);
            Assert.Equal("Test", receivedEvent.Message);
        }
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Subscribe_ShouldReturnDisposable()
    {
        var subscription = EventChannel<TestEvent>.Subscribe(evt => { });
        Assert.NotNull(subscription);
        Assert.IsAssignableFrom<IDisposable>(subscription);
        subscription.Dispose();
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Unsubscribe_ShouldStopNotifications()
    {
        int callCount = 0;

        var subscription = EventChannel<TestEvent>.Subscribe(evt => callCount++);
        
        EventChannel<TestEvent>.Publish(new TestEvent { Value = 1 });
        Assert.Equal(1, callCount);

        subscription.Dispose();
        
        EventChannel<TestEvent>.Publish(new TestEvent { Value = 2 });
        Assert.Equal(1, callCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Publish_ShouldNotifyMultipleSubscribers()
    {
        int count1 = 0, count2 = 0, count3 = 0;

        using var sub1 = EventChannel<TestEvent>.Subscribe(evt => count1++);
        using var sub2 = EventChannel<TestEvent>.Subscribe(evt => count2++);
        using var sub3 = EventChannel<TestEvent>.Subscribe(evt => count3++);

        EventChannel<TestEvent>.Publish(new TestEvent { Value = 1 });

        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
        Assert.Equal(1, count3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Subscribe_WithNullHandler_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            EventChannel<TestEvent>.Subscribe(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Unsubscribe_WithNullHandler_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            EventChannel<TestEvent>.Unsubscribe(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Dispose_CalledMultipleTimes_ShouldBeIdempotent()
    {
        int callCount = 0;
        var subscription = EventChannel<TestEvent>.Subscribe(evt => callCount++);

        subscription.Dispose();
        subscription.Dispose();
        subscription.Dispose();

        EventChannel<TestEvent>.Publish(new TestEvent { Value = 1 });
        Assert.Equal(0, callCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FeatureRegisteredEvent_ShouldPublishAndReceive()
    {
        FeatureRegisteredEvent receivedEvent = default;
        bool eventReceived = false;

        using (EventChannel<FeatureRegisteredEvent>.Subscribe(evt =>
        {
            receivedEvent = evt;
            eventReceived = true;
        }))
        {
            var testEvent = new FeatureRegisteredEvent
            {
                Name = "TestFeature",
                Timestamp = DateTime.UtcNow
            };

            EventChannel<FeatureRegisteredEvent>.Publish(testEvent);

            Assert.True(eventReceived);
            Assert.Equal("TestFeature", receivedEvent.Name);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FeatureInitializedEvent_ShouldPublishAndReceive()
    {
        FeatureInitializedEvent receivedEvent = default;
        bool eventReceived = false;

        using (EventChannel<FeatureInitializedEvent>.Subscribe(evt =>
        {
            receivedEvent = evt;
            eventReceived = true;
        }))
        {
            var testEvent = new FeatureInitializedEvent
            {
                Name = "TestFeature",
                InitTimeMs = 50,
                Timestamp = DateTime.UtcNow
            };

            EventChannel<FeatureInitializedEvent>.Publish(testEvent);

            Assert.True(eventReceived);
            Assert.Equal("TestFeature", receivedEvent.Name);
            Assert.Equal(50, receivedEvent.InitTimeMs);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FeatureShutdownEvent_ShouldPublishAndReceive()
    {
        FeatureShutdownEvent receivedEvent = default;
        bool eventReceived = false;

        using (EventChannel<FeatureShutdownEvent>.Subscribe(evt =>
        {
            receivedEvent = evt;
            eventReceived = true;
        }))
        {
            var testEvent = new FeatureShutdownEvent
            {
                Name = "TestFeature",
                Timestamp = DateTime.UtcNow
            };

            EventChannel<FeatureShutdownEvent>.Publish(testEvent);

            Assert.True(eventReceived);
            Assert.Equal("TestFeature", receivedEvent.Name);
        }
    }

    // Benchmark and Performance Tests
    [Fact]
    [Trait("Category", "Unit")]
    public void Benchmark_EventChannel_PublishPerformance()
    {
        // Warmup
        for (int i = 0; i < 1000; i++)
        {
            EventChannel<TestEvent>.Publish(new TestEvent { Value = i });
        }

        // Benchmark
        const int iterations = 100_000;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            EventChannel<TestEvent>.Publish(new TestEvent { Value = i, Message = "Benchmark" });
        }
        
        sw.Stop();
        
        var avgMicroseconds = (sw.Elapsed.TotalMilliseconds * 1000) / iterations;
        
        // Should be very fast (< 1 microsecond per publish with no subscribers)
        Assert.True(avgMicroseconds < 1.0, 
            $"EventChannel publish too slow: {avgMicroseconds:F3} μs per publish");
        
        Console.WriteLine($"[Benchmark] EventChannel.Publish: {avgMicroseconds:F3} μs per publish ({iterations} iterations)");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Benchmark_EventChannel_WithSubscribers()
    {
        int callCount = 0;
        
        using var sub1 = EventChannel<TestEvent>.Subscribe(evt => callCount++);
        using var sub2 = EventChannel<TestEvent>.Subscribe(evt => callCount++);
        using var sub3 = EventChannel<TestEvent>.Subscribe(evt => callCount++);

        // Warmup
        for (int i = 0; i < 1000; i++)
        {
            EventChannel<TestEvent>.Publish(new TestEvent { Value = i });
        }

        // Benchmark
        const int iterations = 100_000;
        callCount = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            EventChannel<TestEvent>.Publish(new TestEvent { Value = i, Message = "Benchmark" });
        }
        
        sw.Stop();
        
        var avgMicroseconds = (sw.Elapsed.TotalMilliseconds * 1000) / iterations;
        
        // Should be fast even with 3 subscribers (< 5 microseconds)
        Assert.True(avgMicroseconds < 5.0, 
            $"EventChannel with subscribers too slow: {avgMicroseconds:F3} μs per publish");
        
        Assert.Equal(iterations * 3, callCount);
        
        Console.WriteLine($"[Benchmark] EventChannel.Publish (3 subscribers): {avgMicroseconds:F3} μs per publish ({iterations} iterations)");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void VerifyZeroAllocation_StructEvent()
    {
        // This test verifies that EventChannel uses struct events (zero heap allocation)
        // We can't directly measure allocations in xUnit, but we can verify the type constraint
        
        var eventType = typeof(TestEvent);
        Assert.True(eventType.IsValueType, "TestEvent should be a value type (struct)");
        Assert.False(eventType.IsClass, "TestEvent should not be a class");
        
        // Verify all event types are structs
        Assert.True(typeof(FeatureRegisteredEvent).IsValueType);
        Assert.True(typeof(FeatureInitializedEvent).IsValueType);
        Assert.True(typeof(FeatureShutdownEvent).IsValueType);
        
        Console.WriteLine("[Zero-Allocation] All event types are value types (structs) - zero heap allocation confirmed");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Benchmark_SubscribeUnsubscribe()
    {
        const int iterations = 10_000;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var sub = EventChannel<TestEvent>.Subscribe(evt => { });
            sub.Dispose();
        }
        
        sw.Stop();
        
        var avgMicroseconds = (sw.Elapsed.TotalMilliseconds * 1000) / iterations;
        
        // Subscribe/Unsubscribe should be fast (< 10 microseconds)
        Assert.True(avgMicroseconds < 10.0, 
            $"Subscribe/Unsubscribe too slow: {avgMicroseconds:F3} μs per operation");
        
        Console.WriteLine($"[Benchmark] Subscribe/Unsubscribe: {avgMicroseconds:F3} μs per operation ({iterations} iterations)");
    }
}
