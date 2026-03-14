using Xunit;

namespace Virabis.Core.Nexus;

public abstract class FeatureTestBase<TFeature> : IDisposable where TFeature : IFeature, new()
{
    protected TFeature Feature { get; private set; } = default!;
    protected FeatureRegistry Registry => FeatureRegistry.Instance;
    protected EventBus EventBus => EventBus.Instance;

    protected FeatureTestBase()
    {
        Feature = new TFeature();
        CustomSetUp();
    }

    public void Dispose()
    {
        CustomTearDown();
        Feature?.Shutdown();
        
        if (Registry.IsEnabled(Feature.Name))
        {
            Registry.Unregister(Feature.Name);
        }
    }

    [Fact]
    public void Feature_ShouldInitializeSuccessfully()
    {
        var exception = Record.Exception(() => Feature.Initialize());
        Assert.Null(exception);
    }

    [Fact]
    public void Feature_ShouldShutdownSuccessfully()
    {
        Feature.Initialize();
        var exception = Record.Exception(() => Feature.Shutdown());
        Assert.Null(exception);
    }

    [Fact]
    public void Feature_ShouldRegisterWithNexus()
    {
        Registry.Register(Feature);
        Assert.True(Registry.IsEnabled(Feature.Name));
    }

    [Fact]
    public void Feature_ShouldPassHealthCheck()
    {
        Feature.Initialize();
        if (Feature is IHealthCheck healthCheck)
        {
            Assert.True(healthCheck.IsHealthy());
        }
    }

    protected virtual void CustomSetUp() { }
    protected virtual void CustomTearDown() { }
}
