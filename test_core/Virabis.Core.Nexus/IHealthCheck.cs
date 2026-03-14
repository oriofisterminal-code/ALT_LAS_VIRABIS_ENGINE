namespace Virabis.Core.Nexus;

/// <summary>
/// Health check interface for features.
/// Features implementing this interface will be automatically monitored
/// by the Core Nexus system.
/// </summary>
/// <remarks>
/// Health checks are performed every frame. Unhealthy features are
/// automatically disabled to prevent system degradation.
/// </remarks>
public interface IHealthCheck
{
    /// <summary>
    /// Check if feature is healthy.
    /// Unhealthy features will be automatically disabled by the system.
    /// </summary>
    /// <returns>
    /// True if feature is operating normally, false if feature is
    /// experiencing issues and should be disabled.
    /// </returns>
    /// <remarks>
    /// Common health check criteria:
    /// - Performance metrics (FPS, frame time)
    /// - Resource availability (memory, connections)
    /// - Error rates
    /// - Dependency status
    /// </remarks>
    bool IsHealthy();

    /// <summary>
    /// Get detailed health status message.
    /// Used for debugging, logging, and monitoring.
    /// </summary>
    /// <returns>
    /// Human-readable health status description.
    /// Should include relevant metrics and diagnostic information.
    /// </returns>
    /// <example>
    /// "Combat: FPS=58.3, Entities=15, Healthy=true"
    /// </example>
    string GetHealthStatus();
}
