using Virabis.Core.Nexus;

namespace Virabis.Features.Test;

[VirabisFeature(
    ManifestPath = "core/Virabis.Features.Test/test_manifest.json",
    Capabilities = new[] { "test_infrastructure", "validation", "test_coordination" },
    InitTimeMs = 30,
    LazyLoad = false,
    Priority = 50
)]
public class TestFeature : IFeature, IHealthCheck
{
    /// <summary>
    /// Feature name identifier.
    /// </summary>
    public string Name => "Test";

    private bool _isInitialized;

    /// <summary>
    /// Initialize the test feature.
    /// Sets up the test infrastructure coordination layer.
    /// </summary>
    public void Initialize()
    {
        _isInitialized = true;
        Console.WriteLine("[Test] Initialized - Test infrastructure ready");
        Console.WriteLine("[Test] GDScript components: test_level_controller.gd, validation_system.gd, teleport_pad.gd");
    }

    /// <summary>
    /// Shutdown the test feature.
    /// Cleans up test coordination resources.
    /// </summary>
    public void Shutdown()
    {
        _isInitialized = false;
        Console.WriteLine("[Test] Shutdown");
    }

    /// <summary>
    /// Update the test feature.
    /// Currently a no-op as test infrastructure is managed by GDScript.
    /// </summary>
    /// <param name="deltaTime">Time since last frame in seconds</param>
    public void Update(float deltaTime)
    {
        // No-op: Test infrastructure is managed by GDScript
        // This method is here to satisfy the IFeature interface
    }

    /// <summary>
    /// Check if test feature is healthy.
    /// Always returns true - tests should never be auto-disabled.
    /// </summary>
    /// <returns>Always true</returns>
    /// <remarks>
    /// Tests are critical infrastructure and should always remain active.
    /// If there are issues with tests, they should be addressed manually
    /// rather than automatically disabled by the health check system.
    /// </remarks>
    public bool IsHealthy()
    {
        return true;
    }

    /// <summary>
    /// Get detailed health status message.
    /// </summary>
    /// <returns>Human-readable health status</returns>
    public string GetHealthStatus()
    {
        return $"Test: Initialized={_isInitialized}, Healthy=true (always)";
    }

    /// <summary>
    /// Check if the test feature is initialized.
    /// </summary>
    /// <returns>True if initialized, false otherwise</returns>
    [RequiresCapability("test_infrastructure")]
    public bool IsInitialized()
    {
        return _isInitialized;
    }
}
