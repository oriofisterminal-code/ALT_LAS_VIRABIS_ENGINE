using Xunit;
using Virabis.Core.Nexus;
using System.Linq;
using System.Reflection;

namespace Virabis.Core.Nexus.Tests;

public class FeatureDiscoveryTests
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void DiscoverFeatures_FindsCombatFeature()
    {
        var assembly = Assembly.Load("Virabis.Features.Combat");
        var features = FeatureDiscovery.DiscoverFeatures(assembly).ToList();
        
        Assert.NotEmpty(features);
        Assert.Contains(features, f => f.Type.Name == "CombatFeature");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DiscoverFeatures_FindsTestFeature()
    {
        var assembly = Assembly.Load("Virabis.Features.Test");
        var features = FeatureDiscovery.DiscoverFeatures(assembly).ToList();
        
        Assert.NotEmpty(features);
        Assert.Contains(features, f => f.Type.Name == "TestFeature");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DiscoverFeatures_FindsMCPFeature()
    {
        var assembly = Assembly.Load("Virabis.Features.MCP");
        var features = FeatureDiscovery.DiscoverFeatures(assembly).ToList();
        
        Assert.NotEmpty(features);
        Assert.Contains(features, f => f.Type.Name == "MCPFeature");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void GetMetadata_ExtractsAttributeData()
    {
        var assembly = Assembly.Load("Virabis.Features.Combat");
        var combatType = assembly.GetTypes().First(t => t.Name == "CombatFeature");
        
        var metadata = FeatureDiscovery.GetMetadata(combatType);
        
        Assert.NotNull(metadata);
        Assert.Equal(combatType, metadata.Type);
        Assert.NotNull(metadata.Attribute);
        Assert.Contains("damage_calculation", metadata.Attribute.Capabilities);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetMetadata_ReturnsNullForNonAttributedClass()
    {
        var metadata = FeatureDiscovery.GetMetadata(typeof(FeatureDiscoveryTests));
        Assert.Null(metadata);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void DiscoverFeatures_FindsDoubleJumpModifier()
    {
        // Verify that DoubleJumpModifier is discoverable via [VirabisFeature] attribute
        var assembly = Assembly.Load("Virabis.Movement.Core");
        var features = FeatureDiscovery.DiscoverFeatures(assembly).ToList();
        
        Assert.NotEmpty(features);
        Assert.Contains(features, f => f.Type.Name == "DoubleJumpModifier");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void DiscoverFeatures_FindsWallRunModifier()
    {
        // Verify that WallRunModifier is discoverable via [VirabisFeature] attribute
        var assembly = Assembly.Load("Virabis.Movement.Core");
        var features = FeatureDiscovery.DiscoverFeatures(assembly).ToList();
        
        Assert.NotEmpty(features);
        Assert.Contains(features, f => f.Type.Name == "WallRunModifier");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetMetadata_DoubleJumpModifier_HasCorrectCapabilities()
    {
        // Verify DoubleJumpModifier has correct metadata
        var assembly = Assembly.Load("Virabis.Movement.Core");
        var doubleJumpType = assembly.GetTypes().First(t => t.Name == "DoubleJumpModifier");
        
        var metadata = FeatureDiscovery.GetMetadata(doubleJumpType);
        
        Assert.NotNull(metadata);
        Assert.Equal(doubleJumpType, metadata.Type);
        Assert.NotNull(metadata.Attribute);
        Assert.Contains("movement.double_jump", metadata.Attribute.Capabilities);
        Assert.Equal(10, metadata.Attribute.Priority);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetMetadata_WallRunModifier_HasCorrectCapabilities()
    {
        // Verify WallRunModifier has correct metadata
        var assembly = Assembly.Load("Virabis.Movement.Core");
        var wallRunType = assembly.GetTypes().First(t => t.Name == "WallRunModifier");
        
        var metadata = FeatureDiscovery.GetMetadata(wallRunType);
        
        Assert.NotNull(metadata);
        Assert.Equal(wallRunType, metadata.Type);
        Assert.NotNull(metadata.Attribute);
        Assert.Contains("movement.wall_run", metadata.Attribute.Capabilities);
        Assert.Equal(20, metadata.Attribute.Priority);
    }
}
