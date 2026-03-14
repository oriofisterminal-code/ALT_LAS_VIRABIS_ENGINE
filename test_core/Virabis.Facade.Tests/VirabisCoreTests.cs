using Virabis.Core.Nexus;
using Virabis.Facade;
using Xunit;

namespace Virabis.Facade.Tests;

/// <summary>
/// Tests for the VirabisCore static facade API.
/// </summary>
public class VirabisCoreTests : IDisposable
{
    private readonly TestFeature _testFeature;

    public VirabisCoreTests()
    {
        // Clean up any existing features
        CleanupRegistry();

        // Create and register a test feature
        _testFeature = new TestFeature("TestFeature");
        FeatureRegistry.Instance.Register(_testFeature);
    }

    public void Dispose()
    {
        CleanupRegistry();
    }

    private void CleanupRegistry()
    {
        // Unregister all features
        var features = FeatureRegistry.Instance.GetAllFeatures().ToList();
        foreach (var feature in features)
        {
            FeatureRegistry.Instance.Unregister(feature.Name);
        }

        // Clear event bus
        EventBus.Instance.ClearAll();
    }

    [Fact]
    public void EnableFeature_ValidFeature_Success()
    {
        // Arrange
        FeatureRegistry.Instance.Disable("TestFeature");

        // Act
        VirabisCore.EnableFeature("TestFeature");

        // Assert
        Assert.True(VirabisCore.IsFeatureEnabled("TestFeature"));
    }

    [Fact]
    public void EnableFeature_UnregisteredFeature_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            VirabisCore.EnableFeature("NonExistent"));
    }

    [Fact]
    public void DisableFeature_ValidFeature_Success()
    {
        // Arrange
        VirabisCore.EnableFeature("TestFeature");

        // Act
        VirabisCore.DisableFeature("TestFeature");

        // Assert
        Assert.False(VirabisCore.IsFeatureEnabled("TestFeature"));
    }

    [Fact]
    public void DisableFeature_UnregisteredFeature_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            VirabisCore.DisableFeature("NonExistent"));
    }

    [Fact]
    public void IsFeatureHealthy_HealthyFeature_ReturnsTrue()
    {
        // Arrange
        _testFeature.SetHealthy(true);

        // Act
        var isHealthy = VirabisCore.IsFeatureHealthy("TestFeature");

        // Assert
        Assert.True(isHealthy);
    }

    [Fact]
    public void IsFeatureHealthy_UnhealthyFeature_ReturnsFalse()
    {
        // Arrange
        _testFeature.SetHealthy(false);
        FeatureRegistry.Instance.UpdateAll(0.016f); // Trigger health check

        // Act
        var isHealthy = VirabisCore.IsFeatureHealthy("TestFeature");

        // Assert
        Assert.False(isHealthy);
    }

    [Fact]
    public void IsFeatureHealthy_UnregisteredFeature_ReturnsFalse()
    {
        // Act
        var isHealthy = VirabisCore.IsFeatureHealthy("NonExistent");

        // Assert
        Assert.False(isHealthy);
    }

    [Fact]
    public void IsFeatureEnabled_EnabledFeature_ReturnsTrue()
    {
        // Arrange
        VirabisCore.EnableFeature("TestFeature");

        // Act
        var isEnabled = VirabisCore.IsFeatureEnabled("TestFeature");

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void IsFeatureEnabled_DisabledFeature_ReturnsFalse()
    {
        // Arrange
        VirabisCore.DisableFeature("TestFeature");

        // Act
        var isEnabled = VirabisCore.IsFeatureEnabled("TestFeature");

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsFeatureEnabled_UnregisteredFeature_ReturnsFalse()
    {
        // Act
        var isEnabled = VirabisCore.IsFeatureEnabled("NonExistent");

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void GetAllFeatures_ReturnsAllRegisteredFeatures()
    {
        // Arrange
        var feature2 = new TestFeature("Feature2");
        FeatureRegistry.Instance.Register(feature2);

        // Act
        var features = VirabisCore.GetAllFeatures();

        // Assert
        Assert.Equal(2, features.Count);
        Assert.Contains(features, f => f.Name == "TestFeature");
        Assert.Contains(features, f => f.Name == "Feature2");

        // Cleanup
        FeatureRegistry.Instance.Unregister("Feature2");
    }

    [Fact]
    public void GetAllFeatures_NoFeatures_ReturnsEmptyCollection()
    {
        // Arrange
        CleanupRegistry();

        // Act
        var features = VirabisCore.GetAllFeatures();

        // Assert
        Assert.Empty(features);
    }

    [Fact]
    public void OnFeatureEnabled_FeatureEnabled_HandlerCalled()
    {
        // Arrange
        string? enabledFeatureName = null;
        VirabisCore.OnFeatureEnabled(name => enabledFeatureName = name);
        VirabisCore.DisableFeature("TestFeature");

        // Act
        VirabisCore.EnableFeature("TestFeature");

        // Assert
        Assert.Equal("TestFeature", enabledFeatureName);
    }

    [Fact]
    public void OnFeatureEnabled_NullHandler_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VirabisCore.OnFeatureEnabled(null!));
    }

    [Fact]
    public void OnFeatureDisabled_FeatureDisabled_HandlerCalled()
    {
        // Arrange
        string? disabledFeatureName = null;
        VirabisCore.OnFeatureDisabled(name => disabledFeatureName = name);

        // Act
        VirabisCore.DisableFeature("TestFeature");

        // Assert
        Assert.Equal("TestFeature", disabledFeatureName);
    }

    [Fact]
    public void OnFeatureDisabled_NullHandler_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VirabisCore.OnFeatureDisabled(null!));
    }

    [Fact]
    public void OnHealthStatusChanged_HealthChanges_HandlerCalled()
    {
        // Arrange
        string? changedFeatureName = null;
        bool? newHealthStatus = null;
        VirabisCore.OnHealthStatusChanged((name, isHealthy) =>
        {
            changedFeatureName = name;
            newHealthStatus = isHealthy;
        });

        _testFeature.SetHealthy(false);

        // Act
        FeatureRegistry.Instance.UpdateAll(0.016f); // Trigger health check

        // Assert
        Assert.Equal("TestFeature", changedFeatureName);
        Assert.False(newHealthStatus);
    }

    [Fact]
    public void OnHealthStatusChanged_NullHandler_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VirabisCore.OnHealthStatusChanged(null!));
    }

    [Fact]
    public void OnFeatureRegistered_FeatureRegistered_HandlerCalled()
    {
        // Arrange
        string? registeredFeatureName = null;
        VirabisCore.OnFeatureRegistered(name => registeredFeatureName = name);

        // Act
        var newFeature = new TestFeature("NewFeature");
        FeatureRegistry.Instance.Register(newFeature);

        // Assert
        Assert.Equal("NewFeature", registeredFeatureName);

        // Cleanup
        FeatureRegistry.Instance.Unregister("NewFeature");
    }

    [Fact]
    public void OnFeatureRegistered_NullHandler_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VirabisCore.OnFeatureRegistered(null!));
    }

    [Fact]
    public void OnFeatureUnregistered_FeatureUnregistered_HandlerCalled()
    {
        // Arrange
        string? unregisteredFeatureName = null;
        VirabisCore.OnFeatureUnregistered(name => unregisteredFeatureName = name);

        var tempFeature = new TestFeature("TempFeature");
        FeatureRegistry.Instance.Register(tempFeature);

        // Act
        FeatureRegistry.Instance.Unregister("TempFeature");

        // Assert
        Assert.Equal("TempFeature", unregisteredFeatureName);
    }

    [Fact]
    public void OnFeatureUnregistered_NullHandler_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VirabisCore.OnFeatureUnregistered(null!));
    }

    [Fact]
    public void GetDiagnostics_ReturnsFeatureInformation()
    {
        // Act
        var diagnostics = VirabisCore.GetDiagnostics();

        // Assert
        Assert.NotEmpty(diagnostics);
        Assert.Contains("TestFeature", diagnostics.Keys);
        Assert.Contains("Enabled=", diagnostics["TestFeature"]);
        Assert.Contains("Healthy=", diagnostics["TestFeature"]);
    }

    [Fact]
    public void GetDiagnostics_NoFeatures_ReturnsEmptyDictionary()
    {
        // Arrange
        CleanupRegistry();

        // Act
        var diagnostics = VirabisCore.GetDiagnostics();

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void MultipleEventSubscriptions_AllHandlersCalled()
    {
        // Arrange
        int callCount = 0;
        VirabisCore.OnFeatureEnabled(_ => callCount++);
        VirabisCore.OnFeatureEnabled(_ => callCount++);
        VirabisCore.OnFeatureEnabled(_ => callCount++);

        VirabisCore.DisableFeature("TestFeature");

        // Act
        VirabisCore.EnableFeature("TestFeature");

        // Assert
        Assert.Equal(3, callCount);
    }

    [Fact]
    public void EnableDisableSequence_WorksCorrectly()
    {
        // Arrange
        var events = new List<string>();
        VirabisCore.OnFeatureEnabled(name => events.Add($"Enabled:{name}"));
        VirabisCore.OnFeatureDisabled(name => events.Add($"Disabled:{name}"));

        // Act
        VirabisCore.DisableFeature("TestFeature");
        VirabisCore.EnableFeature("TestFeature");
        VirabisCore.DisableFeature("TestFeature");
        VirabisCore.EnableFeature("TestFeature");

        // Assert
        Assert.Equal(4, events.Count);
        Assert.Equal("Disabled:TestFeature", events[0]);
        Assert.Equal("Enabled:TestFeature", events[1]);
        Assert.Equal("Disabled:TestFeature", events[2]);
        Assert.Equal("Enabled:TestFeature", events[3]);
    }

    /// <summary>
    /// Test feature implementation for testing purposes.
    /// </summary>
    private class TestFeature : IFeature, IHealthCheck
    {
        private bool _isHealthy = true;

        public TestFeature(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public void Initialize()
        {
            // Test initialization
        }

        public void Shutdown()
        {
            // Test shutdown
        }

        public void Update(float deltaTime)
        {
            // Test update
        }

        public bool IsHealthy()
        {
            return _isHealthy;
        }

        public string GetHealthStatus()
        {
            return $"{Name}: Healthy={_isHealthy}";
        }

        public void SetHealthy(bool healthy)
        {
            _isHealthy = healthy;
        }
    }
}
