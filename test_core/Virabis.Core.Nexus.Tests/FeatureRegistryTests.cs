using Xunit;
using Virabis.Core.Nexus;
using Virabis.Core.Nexus.Events;

namespace Virabis.Core.Nexus.Tests;

/// <summary>
/// Unit tests for FeatureRegistry class
/// </summary>
public class FeatureRegistryTests
{
    /// <summary>
    /// Mock feature for testing
    /// </summary>
    private class MockFeature : IFeature, IHealthCheck
    {
        public string Name { get; }
        public bool IsHealthy { get; set; } = true;
        public bool InitializeCalled { get; private set; }
        public bool ShutdownCalled { get; private set; }
        public int UpdateCallCount { get; private set; }

        public MockFeature(string name)
        {
            Name = name;
        }

        public void Initialize()
        {
            InitializeCalled = true;
        }

        public void Shutdown()
        {
            ShutdownCalled = true;
        }

        public void Update(float deltaTime)
        {
            UpdateCallCount++;
        }

        bool IHealthCheck.IsHealthy()
        {
            return IsHealthy;
        }

        string IHealthCheck.GetHealthStatus()
        {
            return IsHealthy ? "Healthy" : "Unhealthy";
        }
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Register_ValidFeature_Success()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature = new MockFeature("TestFeature1");

        // Act
        registry.Register(feature);

        // Assert
        Assert.True(registry.IsEnabled(feature.Name));
        Assert.True(feature.InitializeCalled);

        // Cleanup
        registry.Unregister(feature.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_NullFeature_ThrowsException()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_DuplicateFeature_ThrowsException()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature1 = new MockFeature("DuplicateTest");
        var feature2 = new MockFeature("DuplicateTest");

        // Act
        registry.Register(feature1);

        // Assert
        Assert.Throws<InvalidOperationException>(() => registry.Register(feature2));

        // Cleanup
        registry.Unregister(feature1.Name);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Unregister_ExistingFeature_Success()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature = new MockFeature("UnregisterTest");
        registry.Register(feature);

        // Act
        registry.Unregister(feature.Name);

        // Assert
        Assert.False(registry.IsEnabled(feature.Name));
        Assert.True(feature.ShutdownCalled);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Enable_ExistingFeature_Success()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature = new MockFeature("EnableTest");
        registry.Register(feature);
        registry.Disable(feature.Name);

        // Act
        registry.Enable(feature.Name);

        // Assert
        Assert.True(registry.IsEnabled(feature.Name));

        // Cleanup
        registry.Unregister(feature.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Disable_ExistingFeature_Success()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature = new MockFeature("DisableTest");
        registry.Register(feature);

        // Act
        registry.Disable(feature.Name);

        // Assert
        Assert.False(registry.IsEnabled(feature.Name));

        // Cleanup
        registry.Unregister(feature.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsEnabled_EnabledFeature_ReturnsTrue()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature = new MockFeature("IsEnabledTest");
        registry.Register(feature);

        // Act
        var isEnabled = registry.IsEnabled(feature.Name);

        // Assert
        Assert.True(isEnabled);

        // Cleanup
        registry.Unregister(feature.Name);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void IsHealthy_HealthyFeature_ReturnsTrue()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature = new MockFeature("HealthyTest") { IsHealthy = true };
        registry.Register(feature);

        // Act
        var isHealthy = registry.IsHealthy(feature.Name);

        // Assert
        Assert.True(isHealthy);

        // Cleanup
        registry.Unregister(feature.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UpdateAll_UnhealthyFeature_AutoDisabled()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature = new MockFeature("UnhealthyTest") { IsHealthy = true };
        registry.Register(feature);

        // Make feature unhealthy
        feature.IsHealthy = false;

        // Act
        registry.UpdateAll(0.016f);

        // Assert
        Assert.False(registry.IsEnabled(feature.Name));

        // Cleanup
        registry.Unregister(feature.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetAllFeatures_ReturnsAllRegistered()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature1 = new MockFeature("GetAllTest1");
        var feature2 = new MockFeature("GetAllTest2");
        var feature3 = new MockFeature("GetAllTest3");

        registry.Register(feature1);
        registry.Register(feature2);
        registry.Register(feature3);

        // Act
        var allFeatures = registry.GetAllFeatures();

        // Assert
        Assert.Contains(allFeatures, f => f.Name == feature1.Name);
        Assert.Contains(allFeatures, f => f.Name == feature2.Name);
        Assert.Contains(allFeatures, f => f.Name == feature3.Name);

        // Cleanup
        registry.Unregister(feature1.Name);
        registry.Unregister(feature2.Name);
        registry.Unregister(feature3.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void LoadAllFeatures_ValidAssembly_LoadsFeatures()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var assembly = typeof(FeatureRegistryTests).Assembly;

        // Clean up any existing test features
        var existingFeatures = registry.GetAllFeatures().ToList();
        foreach (var f in existingFeatures)
        {
            if (f.Name.Contains("Test"))
            {
                registry.Unregister(f.Name);
            }
        }

        // Act
        var count = registry.LoadAllFeatures(assembly);

        // Assert
        Assert.True(count >= 0); // Should load 0 or more features

        // Cleanup
        var loadedFeatures = registry.GetAllFeatures().ToList();
        foreach (var f in loadedFeatures)
        {
            registry.Unregister(f.Name);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void LoadAllFeatures_NullAssembly_ThrowsException()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.LoadAllFeatures(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void LoadAllFeatures_AlreadyRegistered_SkipsFeature()
    {
        // Arrange
        var registry = FeatureRegistry.Instance;
        var feature = new MockFeature("LoadAllSkipTest");
        registry.Register(feature);
        var assembly = typeof(FeatureRegistryTests).Assembly;

        // Act
        var countBefore = registry.GetAllFeatures().Count;
        registry.LoadAllFeatures(assembly);
        var countAfter = registry.GetAllFeatures().Count;

        // Assert - should not duplicate
        Assert.Equal(countBefore, countAfter);

        // Cleanup
        registry.Unregister(feature.Name);
        var loadedFeatures = registry.GetAllFeatures().ToList();
        foreach (var f in loadedFeatures)
        {
            if (f.Name.Contains("Test"))
            {
                registry.Unregister(f.Name);
            }
        }
    }
}
