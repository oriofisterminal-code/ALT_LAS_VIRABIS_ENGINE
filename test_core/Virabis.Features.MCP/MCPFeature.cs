using Virabis.Core.Nexus;

namespace Virabis.Features.MCP;

[VirabisFeature(
    ManifestPath = "core/Virabis.Features.MCP/mcp_manifest.json",
    Capabilities = new[] { "mcp_server", "godot_interaction", "scene_queries" },
    InitTimeMs = 100,
    LazyLoad = false,
    Priority = 75
)]
public class MCPFeature : IFeature, IHealthCheck
{
    /// <summary>
    /// Feature name identifier.
    /// </summary>
    public string Name => "MCP";

    private bool _isInitialized;

    /// <summary>
    /// Initialize the MCP feature.
    /// Sets up the coordination layer for the GDScript MCP server.
    /// </summary>
    /// <remarks>
    /// The actual MCP server is started by Godot as an autoload.
    /// This method just initializes the C# coordination layer.
    /// </remarks>
    public void Initialize()
    {
        _isInitialized = true;
        
        Console.WriteLine("[MCP] Initialized - MCP coordination layer ready");
        Console.WriteLine("[MCP] GDScript server: mcp_interaction_server.gd (autoload)");
        Console.WriteLine("[MCP] Expected port range: 9090-9099");
    }

    /// <summary>
    /// Shutdown the MCP feature.
    /// Cleans up coordination resources.
    /// </summary>
    /// <remarks>
    /// The GDScript MCP server will be shut down by Godot automatically.
    /// This method only cleans up the C# coordination layer.
    /// </remarks>
    public void Shutdown()
    {
        _isInitialized = false;
        Console.WriteLine("[MCP] Shutdown");
    }

    /// <summary>
    /// Update the MCP feature.
    /// Currently a no-op since health checks are simplified.
    /// </summary>
    /// <param name="deltaTime">Time since last frame in seconds</param>
    public void Update(float deltaTime)
    {
        // No-op for now - health checks simplified
    }

    /// <summary>
    /// Check if MCP feature is healthy.
    /// MCP is healthy if initialized (server health check disabled for now).
    /// </summary>
    /// <returns>True if initialized, false otherwise</returns>
    /// <remarks>
    /// Health check currently only verifies initialization.
    /// Full server connectivity checks are disabled to avoid blocking
    /// when the Godot server is not running (e.g., during unit tests).
    /// </remarks>
    public bool IsHealthy()
    {
        return _isInitialized;
    }

    /// <summary>
    /// Get detailed health status message.
    /// </summary>
    /// <returns>Human-readable health status</returns>
    public string GetHealthStatus()
    {
        if (_isInitialized)
        {
            return "MCP: Initialized=true, Healthy=true";
        }
        else
        {
            return "MCP: Initialized=false, Healthy=false";
        }
    }

    /// <summary>
    /// Get the port the MCP server is running on.
    /// </summary>
    /// <returns>Always returns -1 (port detection disabled)</returns>
    [RequiresCapability("mcp_server")]
    [RequiresCapability("godot_interaction")]
    public int GetServerPort()
    {
        return -1;
    }

    /// <summary>
    /// Check if the MCP feature is initialized.
    /// </summary>
    /// <returns>True if initialized, false otherwise</returns>
    [RequiresCapability("mcp_server")]
    public bool IsInitialized()
    {
        return _isInitialized;
    }
}
