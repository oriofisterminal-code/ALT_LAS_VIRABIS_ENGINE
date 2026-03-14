using Virabis.Core.Nexus;
using Virabis.Core.Nexus.Events;

namespace Virabis.Facade;

/// <summary>
/// Simplified static API for Virabis Core Nexus.
/// Provides easy access to common feature management operations without
/// directly accessing the singleton instances.
/// </summary>
/// <remarks>
/// <para>
/// The VirabisCore facade simplifies interaction with the Core Nexus system
/// by providing a clean, static API that wraps the FeatureRegistry and EventBus.
/// This makes it easier for developers to manage features without understanding
/// the underlying singleton architecture.
/// </para>
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>Enable/disable features at runtime</item>
/// <item>Check feature health status</item>
/// <item>Query all registered features</item>
/// <item>Subscribe to feature lifecycle events</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Enable a feature
/// VirabisCore.EnableFeature("Combat");
/// 
/// // Check if feature is healthy
/// if (VirabisCore.IsFeatureHealthy("Combat"))
/// {
///     // Use combat system
/// }
/// 
/// // Subscribe to events
/// VirabisCore.OnFeatureEnabled(name => 
///     Console.WriteLine($"Feature {name} enabled"));
/// 
/// // Get all features
/// foreach (var feature in VirabisCore.GetAllFeatures())
/// {
///     Console.WriteLine($"Feature: {feature.Name}");
/// }
/// </code>
/// </example>
public static class VirabisCore
{
    /// <summary>
    /// Enable a feature by name.
    /// The feature must be registered before it can be enabled.
    /// </summary>
    /// <param name="name">Name of the feature to enable</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the feature is not registered
    /// </exception>
    /// <remarks>
    /// Enabling a feature allows it to be updated every frame and participate
    /// in the game loop. Enabled features will have their Update() method called
    /// if they are also healthy.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Enable the combat feature
    /// VirabisCore.EnableFeature("Combat");
    /// 
    /// // The feature will now be updated every frame
    /// </code>
    /// </example>
    public static void EnableFeature(string name)
    {
        FeatureRegistry.Instance.Enable(name);
    }

    /// <summary>
    /// Disable a feature by name.
    /// The feature must be registered before it can be disabled.
    /// </summary>
    /// <param name="name">Name of the feature to disable</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the feature is not registered
    /// </exception>
    /// <remarks>
    /// Disabling a feature prevents it from being updated. The feature remains
    /// registered and can be re-enabled later. This is useful for temporarily
    /// turning off features during testing or debugging.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Disable movement during combat testing
    /// VirabisCore.DisableFeature("Movement");
    /// 
    /// // Run combat tests...
    /// 
    /// // Re-enable movement
    /// VirabisCore.EnableFeature("Movement");
    /// </code>
    /// </example>
    public static void DisableFeature(string name)
    {
        FeatureRegistry.Instance.Disable(name);
    }

    /// <summary>
    /// Check if a feature is healthy.
    /// </summary>
    /// <param name="name">Name of the feature to check</param>
    /// <returns>
    /// True if the feature is healthy, false if unhealthy or not registered.
    /// Features that don't implement IHealthCheck are always considered healthy.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Health checks are performed automatically every frame by the Core Nexus
    /// system. Unhealthy features are automatically disabled to prevent system
    /// degradation.
    /// </para>
    /// <para>
    /// Common health check criteria include:
    /// <list type="bullet">
    /// <item>Performance metrics (FPS, frame time)</item>
    /// <item>Resource availability (memory, connections)</item>
    /// <item>Error rates</item>
    /// <item>Dependency status</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Check if combat system is healthy before using it
    /// if (VirabisCore.IsFeatureHealthy("Combat"))
    /// {
    ///     // Safe to use combat system
    ///     ProcessCombat();
    /// }
    /// else
    /// {
    ///     // Combat system is experiencing issues
    ///     ShowWarningToPlayer();
    /// }
    /// </code>
    /// </example>
    public static bool IsFeatureHealthy(string name)
    {
        return FeatureRegistry.Instance.IsHealthy(name);
    }

    /// <summary>
    /// Get all registered features.
    /// </summary>
    /// <returns>
    /// Read-only collection of all features registered with the Core Nexus system.
    /// The collection is a snapshot and will not reflect changes made after this call.
    /// </returns>
    /// <remarks>
    /// This method is useful for debugging, monitoring, and displaying feature
    /// status in debug panels or admin interfaces.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Display all features and their status
    /// foreach (var feature in VirabisCore.GetAllFeatures())
    /// {
    ///     var enabled = FeatureRegistry.Instance.IsEnabled(feature.Name);
    ///     var healthy = VirabisCore.IsFeatureHealthy(feature.Name);
    ///     
    ///     Console.WriteLine($"{feature.Name}:");
    ///     Console.WriteLine($"  Enabled: {enabled}");
    ///     Console.WriteLine($"  Healthy: {healthy}");
    ///     
    ///     if (feature is IHealthCheck healthCheck)
    ///     {
    ///         Console.WriteLine($"  Status: {healthCheck.GetHealthStatus()}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IReadOnlyCollection<IFeature> GetAllFeatures()
    {
        return FeatureRegistry.Instance.GetAllFeatures();
    }

    /// <summary>
    /// Subscribe to feature enabled events.
    /// The handler will be called whenever any feature is enabled.
    /// </summary>
    /// <param name="handler">
    /// Callback that receives the name of the enabled feature.
    /// The handler is stored as a weak reference to prevent memory leaks.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if handler is null
    /// </exception>
    /// <remarks>
    /// <para>
    /// The EventBus uses weak references, so subscribers can be garbage collected
    /// even if still subscribed. This prevents memory leaks but means you should
    /// keep a strong reference to your handler if you want it to persist.
    /// </para>
    /// <para>
    /// The handler will be called on the same thread that enables the feature,
    /// typically the main game thread.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Subscribe to feature enabled events
    /// VirabisCore.OnFeatureEnabled(featureName =>
    /// {
    ///     Console.WriteLine($"[System] Feature '{featureName}' is now enabled");
    ///     
    ///     // Update UI, log metrics, etc.
    ///     UpdateDebugPanel();
    /// });
    /// 
    /// // Later, when a feature is enabled:
    /// VirabisCore.EnableFeature("Combat");
    /// // Output: [System] Feature 'Combat' is now enabled
    /// </code>
    /// </example>
    public static void OnFeatureEnabled(Action<string> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        EventChannel<FeatureEnabledEvent>.Subscribe(
            e => handler(e.Name));
    }

    /// <summary>
    /// Subscribe to feature disabled events.
    /// The handler will be called whenever any feature is disabled.
    /// </summary>
    /// <param name="handler">
    /// Callback that receives the name of the disabled feature.
    /// The handler is stored as a weak reference to prevent memory leaks.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if handler is null
    /// </exception>
    /// <remarks>
    /// Features can be disabled manually via DisableFeature() or automatically
    /// by the health check system when they become unhealthy.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Subscribe to feature disabled events
    /// VirabisCore.OnFeatureDisabled(featureName =>
    /// {
    ///     Console.WriteLine($"[Warning] Feature '{featureName}' disabled");
    ///     
    ///     // Handle feature shutdown
    ///     if (featureName == "Combat")
    ///     {
    ///         PauseCombat();
    ///     }
    /// });
    /// </code>
    /// </example>
    public static void OnFeatureDisabled(Action<string> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        EventChannel<FeatureDisabledEvent>.Subscribe(
            e => handler(e.Name));
    }

    /// <summary>
    /// Subscribe to health status change events.
    /// The handler will be called whenever a feature's health status changes.
    /// </summary>
    /// <param name="handler">
    /// Callback that receives the feature name and new health status.
    /// Parameters: (featureName, isHealthy)
    /// The handler is stored as a weak reference to prevent memory leaks.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if handler is null
    /// </exception>
    /// <remarks>
    /// <para>
    /// Health status changes are detected automatically during the UpdateAll()
    /// cycle. When a feature becomes unhealthy, it is automatically disabled.
    /// When it recovers, it remains disabled until manually re-enabled.
    /// </para>
    /// <para>
    /// This event is useful for:
    /// <list type="bullet">
    /// <item>Logging and monitoring</item>
    /// <item>Alerting players to system issues</item>
    /// <item>Triggering fallback behaviors</item>
    /// <item>Collecting telemetry data</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Subscribe to health status changes
    /// VirabisCore.OnHealthStatusChanged((featureName, isHealthy) =>
    /// {
    ///     if (isHealthy)
    ///     {
    ///         Console.WriteLine($"[Recovery] Feature '{featureName}' is healthy again");
    ///     }
    ///     else
    ///     {
    ///         Console.WriteLine($"[Alert] Feature '{featureName}' became unhealthy!");
    ///         
    ///         // Log to telemetry
    ///         LogHealthIssue(featureName);
    ///         
    ///         // Show warning to player if critical feature
    ///         if (featureName == "Combat")
    ///         {
    ///             ShowPlayerWarning("Combat system experiencing issues");
    ///         }
    ///     }
    /// });
    /// </code>
    /// </example>
    public static void OnHealthStatusChanged(Action<string, bool> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        EventChannel<HealthStatusChangedEvent>.Subscribe(
            e => handler(e.Name, e.IsHealthy));
    }

    /// <summary>
    /// Subscribe to feature registered events.
    /// The handler will be called whenever a new feature is registered.
    /// </summary>
    /// <param name="handler">
    /// Callback that receives the name of the registered feature.
    /// The handler is stored as a weak reference to prevent memory leaks.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if handler is null
    /// </exception>
    /// <remarks>
    /// This event is useful for tracking which features are available in the
    /// system and for initializing feature-specific UI or monitoring.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Track all registered features
    /// var registeredFeatures = new List&lt;string&gt;();
    /// 
    /// VirabisCore.OnFeatureRegistered(featureName =>
    /// {
    ///     registeredFeatures.Add(featureName);
    ///     Console.WriteLine($"[System] Registered feature: {featureName}");
    ///     Console.WriteLine($"[System] Total features: {registeredFeatures.Count}");
    /// });
    /// </code>
    /// </example>
    public static void OnFeatureRegistered(Action<string> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        EventChannel<FeatureRegisteredEvent>.Subscribe(
            e => handler(e.Name));
    }

    /// <summary>
    /// Subscribe to feature unregistered events.
    /// The handler will be called whenever a feature is unregistered.
    /// </summary>
    /// <param name="handler">
    /// Callback that receives the name of the unregistered feature.
    /// The handler is stored as a weak reference to prevent memory leaks.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if handler is null
    /// </exception>
    /// <remarks>
    /// Features are typically unregistered during application shutdown or
    /// when dynamically unloading plugins/modules.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clean up feature-specific resources
    /// VirabisCore.OnFeatureUnregistered(featureName =>
    /// {
    ///     Console.WriteLine($"[System] Unregistered feature: {featureName}");
    ///     
    ///     // Clean up any feature-specific UI or resources
    ///     CleanupFeatureResources(featureName);
    /// });
    /// </code>
    /// </example>
    public static void OnFeatureUnregistered(Action<string> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        EventChannel<FeatureUnregisteredEvent>.Subscribe(
            e => handler(e.Name));
    }

    /// <summary>
    /// Check if a feature is enabled.
    /// </summary>
    /// <param name="name">Name of the feature to check</param>
    /// <returns>
    /// True if the feature is enabled, false if disabled or not registered.
    /// </returns>
    /// <remarks>
    /// A feature can be enabled but unhealthy. Use IsFeatureHealthy() to check
    /// health status. For a feature to be updated, it must be both enabled AND healthy.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Check if feature is enabled before using it
    /// if (VirabisCore.IsFeatureEnabled("Combat"))
    /// {
    ///     // Combat feature is enabled
    ///     ProcessCombat();
    /// }
    /// 
    /// // Check both enabled and healthy
    /// if (VirabisCore.IsFeatureEnabled("Combat") &amp;&amp; 
    ///     VirabisCore.IsFeatureHealthy("Combat"))
    /// {
    ///     // Combat feature is fully operational
    ///     ProcessCombat();
    /// }
    /// </code>
    /// </example>
    public static bool IsFeatureEnabled(string name)
    {
        return FeatureRegistry.Instance.IsEnabled(name);
    }

    /// <summary>
    /// Get diagnostic information about all features.
    /// </summary>
    /// <returns>
    /// Dictionary mapping feature names to diagnostic strings containing
    /// enabled status, health status, and additional information.
    /// </returns>
    /// <remarks>
    /// This method is useful for debugging, monitoring dashboards, and
    /// admin interfaces. The diagnostic strings include all relevant
    /// information about each feature's current state.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Display diagnostics for all features
    /// var diagnostics = VirabisCore.GetDiagnostics();
    /// 
    /// Console.WriteLine("=== Feature Diagnostics ===");
    /// foreach (var kvp in diagnostics)
    /// {
    ///     Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    /// }
    /// 
    /// // Example output:
    /// // Combat: Enabled=true, Healthy=true, Status=Combat: FPS=60.0, Entities=10
    /// // Movement: Enabled=true, Healthy=true, Status=Movement: FPS=60.0
    /// // Test: Enabled=true, Healthy=true, Status=No health check
    /// </code>
    /// </example>
    public static Dictionary<string, string> GetDiagnostics()
    {
        return FeatureRegistry.Instance.GetDiagnostics();
    }
}
