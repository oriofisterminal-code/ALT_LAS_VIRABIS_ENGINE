using Virabis.Core.Nexus;
using Xunit;

namespace Virabis.Core.Nexus.Tests;

/// <summary>
/// Tests for the Capability System (RequiresCapability, CapabilityValidator, CapabilityRegistry).
/// </summary>
public class CapabilitySystemTests
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void CapabilityRegistry_Register_ShouldStoreCapabilities()
    {
        // Arrange
        var registry = CapabilityRegistry.Instance;
        registry.ClearAll();

        // Act
        registry.Register("TestFeature", new[] { "cap1", "cap2" });

        // Assert
        Assert.True(registry.HasCapability("TestFeature", "cap1"));
        Assert.True(registry.HasCapability("TestFeature", "cap2"));
        Assert.False(registry.HasCapability("TestFeature", "cap3"));
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void CapabilityRegistry_EnforceCapability_ShouldThrowIfMissing()
    {
        // Arrange
        var registry = CapabilityRegistry.Instance;
        registry.ClearAll();
        registry.Register("TestFeature", new[] { "cap1" });

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
            registry.EnforceCapability("TestFeature", "cap2"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CapabilityRegistry_EnforceCapability_ShouldNotThrowIfPresent()
    {
        // Arrange
        var registry = CapabilityRegistry.Instance;
        registry.ClearAll();
        registry.Register("TestFeature", new[] { "cap1" });

        // Act & Assert (should not throw)
        registry.EnforceCapability("TestFeature", "cap1");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CapabilityRegistry_GetCapabilities_ShouldReturnAllCapabilities()
    {
        // Arrange
        var registry = CapabilityRegistry.Instance;
        registry.ClearAll();
        registry.Register("TestFeature", new[] { "cap1", "cap2", "cap3" });

        // Act
        var capabilities = registry.GetCapabilities("TestFeature");

        // Assert
        Assert.Equal(3, capabilities.Count);
        Assert.Contains("cap1", capabilities);
        Assert.Contains("cap2", capabilities);
        Assert.Contains("cap3", capabilities);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CapabilityRegistry_Unregister_ShouldRemoveCapabilities()
    {
        // Arrange
        var registry = CapabilityRegistry.Instance;
        registry.ClearAll();
        registry.Register("TestFeature", new[] { "cap1" });

        // Act
        registry.Unregister("TestFeature");

        // Assert
        Assert.False(registry.HasCapability("TestFeature", "cap1"));
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void CapabilityValidator_ValidateFeature_ShouldPassWhenAllCapabilitiesDeclared()
    {
        // Arrange
        var declaredCapabilities = new[] { "test_capability" };

        // Act
        var result = CapabilityValidator.ValidateFeature(
            typeof(TestFeatureWithCapability), 
            declaredCapabilities);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CapabilityValidator_ValidateFeature_ShouldFailWhenCapabilityMissing()
    {
        // Arrange
        var declaredCapabilities = new string[] { }; // No capabilities declared

        // Act
        var result = CapabilityValidator.ValidateFeature(
            typeof(TestFeatureWithCapability), 
            declaredCapabilities);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("test_capability", result.GetErrorMessage());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CapabilityValidator_GetRequiredCapabilities_ShouldReturnMethodCapabilities()
    {
        // Arrange
        var method = typeof(TestFeatureWithCapability).GetMethod(nameof(TestFeatureWithCapability.TestMethod));

        // Act
        var capabilities = CapabilityValidator.GetRequiredCapabilities(method!);

        // Assert
        Assert.Single(capabilities);
        Assert.Contains("test_capability", capabilities);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RequiresCapabilityAttribute_ShouldThrowOnNullCapability()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RequiresCapabilityAttribute(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RequiresCapabilityAttribute_ShouldThrowOnEmptyCapability()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RequiresCapabilityAttribute(""));
        Assert.Throws<ArgumentException>(() => new RequiresCapabilityAttribute("   "));
    }
}

/// <summary>
/// Test feature class with capability requirements.
/// </summary>
internal class TestFeatureWithCapability
{
    [RequiresCapability("test_capability")]
    public void TestMethod()
    {
        // Test method
    }
}
