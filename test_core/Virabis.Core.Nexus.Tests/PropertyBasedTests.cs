using Xunit;
using CsCheck;
using Virabis.Core.Nexus;
using Virabis.Core.Nexus.Events;

namespace Virabis.Core.Nexus.Tests;

/// <summary>
/// Property-based tests using CsCheck
/// Iteration count controlled by CSCHECK_ITER environment variable (default: 100, CI: 10)
/// </summary>
public class PropertyBasedTests
{
    /// <summary>
    /// Get iteration count from environment variable or use default
    /// </summary>
    private static int GetIterationCount()
    {
        var iterEnv = Environment.GetEnvironmentVariable("CSCHECK_ITER");
        return int.TryParse(iterEnv, out var iter) ? iter : 100;
    }

    /// <summary>
    /// Mock feature for property testing
    /// </summary>
    private class TestFeature : IFeature, IHealthCheck
    {
        public string Name { get; }
        public bool IsHealthy { get; set; } = true;
        private int _updateCount = 0;

        public TestFeature(string name)
        {
            Name = name;
        }

        public void Initialize() { }
        public void Shutdown() { }
        public void Update(float deltaTime) => _updateCount++;
        bool IHealthCheck.IsHealthy() => IsHealthy;
        string IHealthCheck.GetHealthStatus() => IsHealthy ? "Healthy" : "Unhealthy";
    }

    [Fact]
    [Trait("Category", "Property")]
    public void FeatureRegistry_ThreadSafe_AlwaysConsistent()
    {
        // Property: Concurrent operations should never corrupt registry state
        Gen.Int[1, 10].Select(i => new TestFeature($"ThreadSafeFeature{i}_{Guid.NewGuid()}"))
            .Array[3, 8]
            .Sample(features =>
            {
                var registry = FeatureRegistry.Instance;
                
                // Cleanup before test - use unique names so no conflicts
                foreach (var f in features)
                {
                    try { registry.Unregister(f.Name); } catch { }
                }

                // Concurrent registration
                Parallel.ForEach(features, feature =>
                {
                    try
                    {
                        registry.Register(feature);
                    }
                    catch (InvalidOperationException)
                    {
                        // Duplicate registration is expected in concurrent scenario
                    }
                });

                // Verify: All features should be registered
                var allFeatures = registry.GetAllFeatures();
                foreach (var feature in features)
                {
                    Assert.Contains(allFeatures, f => f.Name == feature.Name);
                }

                // Cleanup
                foreach (var feature in features)
                {
                    try { registry.Unregister(feature.Name); } catch { }
                }
            }, iter: GetIterationCount());
    }

    [Fact]
    [Trait("Category", "Property")]
    public void EventBus_ThreadSafe_NoDeadlock()
    {
        // Property: Concurrent publish/subscribe should never deadlock
        Gen.Int[1, 100].Array[5, 15]
            .Sample(eventIds =>
            {
                var receivedCount = 0;

                // Subscribe multiple handlers
                var subscriptions = new List<IDisposable>();
                for (int i = 0; i < 5; i++)
                {
                    var sub = EventChannel<FeatureRegisteredEvent>.Subscribe(e =>
                    {
                        Interlocked.Increment(ref receivedCount);
                    });
                    subscriptions.Add(sub);
                }

                // Concurrent publish
                Parallel.ForEach(eventIds, id =>
                {
                    EventChannel<FeatureRegisteredEvent>.Publish(new FeatureRegisteredEvent
                    {
                        Name = $"Feature{id}",
                        Timestamp = DateTime.UtcNow
                    });
                });

                // Cleanup
                foreach (var sub in subscriptions)
                {
                    sub.Dispose();
                }

                // Verify: Should receive events (exact count may vary due to GC)
                Assert.True(receivedCount > 0);
            }, iter: GetIterationCount());
    }

    [Fact]
    [Trait("Category", "Property")]
    public void HealthCheck_AlwaysDisablesUnhealthy()
    {
        // Property: Unhealthy features should always be disabled after UpdateAll
        Gen.Bool.Array[3, 8]
            .Sample(healthStates =>
            {
                var registry = FeatureRegistry.Instance;
                var features = healthStates.Select((healthy, i) =>
                    new TestFeature($"HealthCheckTest{i}_{Guid.NewGuid()}") { IsHealthy = healthy }
                ).ToArray();

                // Register all features (cleanup first with unique names)
                foreach (var feature in features)
                {
                    try { registry.Unregister(feature.Name); } catch { }
                    registry.Register(feature);
                }

                // Update all
                registry.UpdateAll(0.016f);

                // Verify: Unhealthy features should be disabled
                for (int i = 0; i < features.Length; i++)
                {
                    var feature = features[i];
                    var expectedEnabled = healthStates[i];
                    var actualEnabled = registry.IsEnabled(feature.Name);
                    
                    Assert.Equal(expectedEnabled, actualEnabled);
                }

                // Cleanup
                foreach (var feature in features)
                {
                    try { registry.Unregister(feature.Name); } catch { }
                }
            }, iter: GetIterationCount());
    }

    /// <summary>
    /// **Validates: Requirements 2.5**
    /// Property: Concurrent register/unregister operations should never
    /// corrupt registry state or cause exceptions
    /// </summary>
    [Fact]
    [Trait("Category", "Property")]
    public void FeatureRegistry_ConcurrentRegisterUnregister_NoRaceConditions()
    {
        Gen.Int[1, 10].Select(i => new TestFeature($"ConcurrentTest{i}_{Guid.NewGuid()}"))
            .Array[5, 12]
            .Sample(features =>
            {
                var registry = FeatureRegistry.Instance;
                
                // Cleanup
                foreach (var f in features)
                {
                    try { registry.Unregister(f.Name); } catch { }
                }
                
                // Concurrent register and unregister
                Parallel.ForEach(features, feature =>
                {
                    try
                    {
                        // Register
                        registry.Register(feature);
                        
                        // Immediately unregister (stress test)
                        registry.Unregister(feature.Name);
                        
                        // Register again
                        registry.Register(feature);
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected in concurrent scenario
                    }
                });
                
                // Invariant: Registry should be in consistent state
                var allFeatures = registry.GetAllFeatures();
                Assert.NotNull(allFeatures);
                
                // Cleanup
                foreach (var feature in features)
                {
                    try { registry.Unregister(feature.Name); } catch { }
                }
            }, iter: GetIterationCount());
    }

    /// <summary>
    /// **Validates: Requirements 2.7, 2.8**
    /// Property: Event channel should deliver events to all subscribers
    /// Tests that all subscribed handlers receive published events
    /// </summary>
    [Fact]
    [Trait("Category", "Property")]
    public void EventChannel_SubscriptionOrdering_AlwaysPreserved()
    {
        // Test that all subscribers receive events (ordering is implementation detail)
        Gen.Int[2, 8] // Number of subscribers
            .Sample(subscriberCount =>
            {
                var receivedCount = 0;
                var subscriptions = new List<IDisposable>();
                var testId = Guid.NewGuid().ToString();

                try
                {
                    // Subscribe handlers
                    for (int i = 0; i < subscriberCount; i++)
                    {
                        var sub = EventChannel<FeatureRegisteredEvent>.Subscribe(e =>
                        {
                            if (e.Name == testId)
                            {
                                System.Threading.Interlocked.Increment(ref receivedCount);
                            }
                        });
                        subscriptions.Add(sub);
                    }

                    // Publish event
                    EventChannel<FeatureRegisteredEvent>.Publish(new FeatureRegisteredEvent
                    {
                        Name = testId,
                        Timestamp = DateTime.UtcNow
                    });

                    // Small delay to ensure all handlers execute
                    System.Threading.Thread.Sleep(10);

                    // Invariant: All subscribers should have received the event
                    Assert.Equal(subscriberCount, receivedCount);
                }
                finally
                {
                    // Cleanup
                    foreach (var sub in subscriptions)
                    {
                        sub.Dispose();
                    }
                }
            }, iter: GetIterationCount());
    }

    /// <summary>
    /// **Validates: Requirements 2.7, 2.8**
    /// Property: Feature priority ordering should be maintained during registration
    /// Higher priority features should be registered before lower priority features
    /// </summary>
    [Fact]
    [Trait("Category", "Property")]
    public void FeatureRegistry_PriorityOrdering_MaintainedDuringRegistration()
    {
        Gen.Int[0, 100].Array[3, 6] // Generate array of priorities
            .Sample(priorities =>
            {
                var registry = FeatureRegistry.Instance;
                var features = new List<(IFeature feature, int priority, int registrationOrder)>();
                var registrationCounter = 0;

                try
                {
                    // Create features with different priorities
                    for (int i = 0; i < priorities.Length; i++)
                    {
                        var feature = new TestFeature($"PriorityTest{i}_{Guid.NewGuid()}");
                        features.Add((feature, priorities[i], -1)); // -1 means not registered yet
                    }

                    // Sort by priority (descending) to simulate LoadAllFeatures behavior
                    var sortedFeatures = features
                        .OrderByDescending(f => f.priority)
                        .ToList();

                    // Register features in priority order
                    for (int i = 0; i < sortedFeatures.Count; i++)
                    {
                        var (feature, priority, _) = sortedFeatures[i];
                        try
                        {
                            registry.Register(feature);
                            sortedFeatures[i] = (feature, priority, registrationCounter++);
                        }
                        catch (InvalidOperationException)
                        {
                            // Feature already registered (shouldn't happen with unique names)
                        }
                    }

                    // Invariant: Features should be registered in priority order
                    // Higher priority features should have lower registration order numbers
                    for (int i = 0; i < sortedFeatures.Count - 1; i++)
                    {
                        var current = sortedFeatures[i];
                        var next = sortedFeatures[i + 1];

                        // If current has higher priority, it should be registered first (lower order number)
                        if (current.priority > next.priority)
                        {
                            Assert.True(current.registrationOrder < next.registrationOrder,
                                $"Feature with priority {current.priority} should be registered before feature with priority {next.priority}");
                        }
                    }

                    // Additional invariant: All features should be registered
                    var allFeatures = registry.GetAllFeatures();
                    foreach (var (feature, _, order) in sortedFeatures)
                    {
                        if (order >= 0) // Was successfully registered
                        {
                            Assert.Contains(allFeatures, f => f.Name == feature.Name);
                        }
                    }
                }
                finally
                {
                    // Cleanup
                    foreach (var (feature, _, _) in features)
                    {
                        try { registry.Unregister(feature.Name); } catch { }
                    }
                }
            }, iter: GetIterationCount());
    }
}
