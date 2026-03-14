using Virabis.Core.Nexus;
using Virabis.Features.Test;
using Xunit;

namespace Virabis.Features.Test.Tests;

public class TestFeatureTests : FeatureTestBase<TestFeature>
{
    [Fact]
    public void TestFeature_HasCorrectName()
    {
        Assert.Equal("Test", Feature.Name);
    }

    [Fact]
    public void TestFeature_InitializeSetsInitializedFlag()
    {
        Feature.Initialize();
        Assert.True(Feature.IsInitialized());
    }

    [Fact]
    public void TestFeature_ShutdownClearsInitializedFlag()
    {
        Feature.Initialize();
        Feature.Shutdown();
        Assert.False(Feature.IsInitialized());
    }

    [Fact]
    public void TestFeature_IsHealthyAlwaysReturnsTrue()
    {
        Assert.True(Feature.IsHealthy());
        
        Feature.Initialize();
        Assert.True(Feature.IsHealthy());
        
        Feature.Shutdown();
        Assert.True(Feature.IsHealthy());
    }

    [Fact]
    public void TestFeature_UpdateDoesNotThrow()
    {
        Feature.Initialize();
        
        var exception1 = Record.Exception(() => Feature.Update(0.016f));
        var exception2 = Record.Exception(() => Feature.Update(1.0f));
        var exception3 = Record.Exception(() => Feature.Update(0.0f));
        
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }

    [Fact]
    public void TestFeature_GetHealthStatusReturnsCorrectFormat()
    {
        var status = Feature.GetHealthStatus();
        
        Assert.Contains("Test:", status);
        Assert.Contains("Initialized=", status);
        Assert.Contains("Healthy=true", status);
    }

    [Fact]
    public void TestFeature_RemainsHealthyAfterMultipleUpdates()
    {
        Registry.Register(Feature);
        
        for (int i = 0; i < 100; i++)
        {
            Registry.UpdateAll(0.016f);
        }
        
        Assert.True(Registry.IsHealthy("Test"));
        Assert.True(Registry.IsEnabled("Test"));
    }
}
