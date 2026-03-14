namespace Virabis.Core.Nexus;

/// <summary>
/// Base interface for all Virabis features.
/// Every feature must implement this interface to be managed by Core Nexus.
/// </summary>
/// <remarks>
/// Features are modular, self-contained systems that can be registered,
/// enabled, disabled, and monitored by the Core Nexus system.
/// </remarks>
public interface IFeature
{
    /// <summary>
    /// Unique name of the feature.
    /// Must be unique across all features in the system.
    /// </summary>
    /// <example>
    /// "Combat", "Movement", "Test", "MCP"
    /// </example>
    string Name { get; }

    /// <summary>
    /// Initialize the feature.
    /// Called once when feature is registered with the FeatureRegistry.
    /// Use this method to set up resources, load configurations, etc.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if initialization fails
    /// </exception>
    void Initialize();

    /// <summary>
    /// Shutdown the feature.
    /// Called when feature is unregistered or application closes.
    /// Use this method to clean up resources, save state, etc.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Update the feature.
    /// Called every frame if feature is enabled and healthy.
    /// </summary>
    /// <param name="deltaTime">Time since last frame in seconds</param>
    /// <remarks>
    /// This method should be lightweight and complete quickly.
    /// Heavy operations should be done asynchronously.
    /// </remarks>
    void Update(float deltaTime);
}
