using Virabis.Core.Nexus.Events;

namespace Virabis.Core.Nexus;

/// <summary>
/// Central registry for all Virabis features.
/// Singleton pattern - use FeatureRegistry.Instance.
/// Thread-safe implementation.
/// </summary>
/// <remarks>
/// The FeatureRegistry is the heart of the Core Nexus system.
/// It manages feature lifecycle, health monitoring, and coordination.
/// </remarks>
public sealed class FeatureRegistry
{
    private static readonly Lazy<FeatureRegistry> _instance = 
        new(() => new FeatureRegistry());

    /// <summary>
    /// Gets the singleton instance of the FeatureRegistry.
    /// </summary>
    public static FeatureRegistry Instance => _instance.Value;

    private readonly Dictionary<string, IFeature> _features;
    private readonly Dictionary<string, bool> _enabled;
    private readonly Dictionary<string, bool> _healthy;
    private readonly object _lock = new();

    private FeatureRegistry()
    {
        _features = new Dictionary<string, IFeature>();
        _enabled = new Dictionary<string, bool>();
        _healthy = new Dictionary<string, bool>();
    }

    /// <summary>
    /// Automatically discover and load all features from the specified assembly.
    /// SIMPLIFIED: Only loads Combat and Movement features.
    /// </summary>
    [Obsolete("Use manual Register() instead. Auto-discovery removed for simplicity.")]
    public int LoadAllFeatures(System.Reflection.Assembly assembly)
    {
        throw new NotImplementedException("Auto-discovery removed. Use Register() manually.");
    }

    /// <summary>
    /// Register a new feature with the registry.
    /// The feature will be initialized and enabled by default.
    /// </summary>
    /// <param name="feature">Feature to register</param>
    /// <exception cref="ArgumentNullException">If feature is null</exception>
    /// <exception cref="ArgumentException">If feature name is null or empty</exception>
    /// <exception cref="InvalidOperationException">If feature already registered</exception>
    /// <example>
    /// var combatFeature = new CombatFeature();
    /// FeatureRegistry.Instance.Register(combatFeature);
    /// </example>
    public void Register(IFeature feature)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        if (string.IsNullOrWhiteSpace(feature.Name))
            throw new ArgumentException(
                "Feature name cannot be null or empty", 
                nameof(feature));

        lock (_lock)
        {
            if (_features.ContainsKey(feature.Name))
                throw new InvalidOperationException(
                    $"Feature '{feature.Name}' is already registered");

            try
            {
                // Initialize feature
                feature.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nexus] Failed to initialize feature '{feature.Name}': {ex.Message}");
                throw;
            }

            // Add to registry
            _features[feature.Name] = feature;
            _enabled[feature.Name] = true;
            _healthy[feature.Name] = true;

            Console.WriteLine($"[Nexus] Registered feature: {feature.Name}");

            // Publish event (never throws)
            try
            {
                EventChannel<FeatureRegisteredEvent>.Publish(new FeatureRegisteredEvent
                {
                    Name = feature.Name,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nexus] Event publish failed: {ex.Message}");
            }
        }
    }
    /// <summary>
    /// Unregister a feature from the registry.
    /// The feature will be shut down and removed.
    /// </summary>
    /// <param name="name">Feature name</param>
    /// <example>
    /// FeatureRegistry.Instance.Unregister("Combat");
    /// </example>
    public void Unregister(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        lock (_lock)
        {
            if (!_features.ContainsKey(name))
                return;

            var feature = _features[name];

            try
            {
                feature.Shutdown();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nexus] Failed to shutdown feature '{name}': {ex.Message}");
            }

            _features.Remove(name);
            _enabled.Remove(name);
            _healthy.Remove(name);

            Console.WriteLine($"[Nexus] Unregistered feature: {name}");

            try
            {
                EventChannel<FeatureUnregisteredEvent>.Publish(new FeatureUnregisteredEvent
                {
                    Name = name,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nexus] Event publish failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Enable a feature.
    /// Enabled features will be updated every frame.
    /// </summary>
    /// <param name="name">Feature name</param>
    /// <exception cref="InvalidOperationException">If feature not registered</exception>
    /// <example>
    /// FeatureRegistry.Instance.Enable("Combat");
    /// </example>
    public void Enable(string name)
    {
        lock (_lock)
        {
            if (!_features.ContainsKey(name))
                throw new InvalidOperationException(
                    $"Feature '{name}' is not registered");

            if (_enabled[name])
                return; // Already enabled

            _enabled[name] = true;
            Console.WriteLine($"[Nexus] Enabled feature: {name}");

            try
            {
                EventChannel<FeatureEnabledEvent>.Publish(new FeatureEnabledEvent
                {
                    Name = name,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nexus] Event publish failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Disable a feature.
    /// Disabled features will not be updated.
    /// </summary>
    /// <param name="name">Feature name</param>
    /// <exception cref="InvalidOperationException">If feature not registered</exception>
    /// <example>
    /// FeatureRegistry.Instance.Disable("Combat");
    /// </example>
    public void Disable(string name)
    {
        lock (_lock)
        {
            if (!_features.ContainsKey(name))
                throw new InvalidOperationException(
                    $"Feature '{name}' is not registered");

            if (!_enabled[name])
                return; // Already disabled

            _enabled[name] = false;
            Console.WriteLine($"[Nexus] Disabled feature: {name}");

            try
            {
                EventChannel<FeatureDisabledEvent>.Publish(new FeatureDisabledEvent
                {
                    Name = name,
                    Reason = "Manual disable",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Nexus] Event publish failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Check if a feature is enabled.
    /// </summary>
    /// <param name="name">Feature name</param>
    /// <returns>True if enabled, false otherwise</returns>
    /// <example>
    /// if (FeatureRegistry.Instance.IsEnabled("Combat"))
    /// {
    ///     // Use combat system
    /// }
    /// </example>
    public bool IsEnabled(string name)
    {
        lock (_lock)
        {
            return _enabled.TryGetValue(name, out var enabled) && enabled;
        }
    }

    /// <summary>
    /// Check if a feature is healthy.
    /// </summary>
    /// <param name="name">Feature name</param>
    /// <returns>True if healthy, false otherwise</returns>
    /// <example>
    /// if (FeatureRegistry.Instance.IsHealthy("Combat"))
    /// {
    ///     // Combat system is operating normally
    /// }
    /// </example>
    public bool IsHealthy(string name)
    {
        lock (_lock)
        {
            return _healthy.TryGetValue(name, out var healthy) && healthy;
        }
    }

    /// <summary>
    /// Get all registered features.
    /// </summary>
    /// <returns>Read-only collection of features</returns>
    /// <example>
    /// foreach (var feature in FeatureRegistry.Instance.GetAllFeatures())
    /// {
    ///     Console.WriteLine($"{feature.Name}: Enabled={IsEnabled(feature.Name)}");
    /// }
    /// </example>
    public IReadOnlyCollection<IFeature> GetAllFeatures()
    {
        lock (_lock)
        {
            return _features.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Get a specific feature by name.
    /// </summary>
    /// <param name="name">Feature name</param>
    /// <returns>Feature instance, or null if not found</returns>
    public IFeature? GetFeature(string name)
    {
        lock (_lock)
        {
            return _features.TryGetValue(name, out var feature) ? feature : null;
        }
    }

    /// <summary>
    /// Update all enabled features.
    /// Called every frame by game loop.
    /// Performs health checks and automatically disables unhealthy features.
    /// </summary>
    /// <param name="deltaTime">Time since last frame in seconds</param>
    /// <remarks>
    /// This method should be called from your main game loop at 60 FPS.
    /// It handles health monitoring and feature updates automatically.
    /// </remarks>
    /// <example>
    /// // In your game loop:
    /// void Update(float deltaTime)
    /// {
    ///     FeatureRegistry.Instance.UpdateAll(deltaTime);
    /// }
    /// </example>
    public void UpdateAll(float deltaTime)
    {
        lock (_lock)
        {
            foreach (var kvp in _features.ToList()) // ToList to avoid modification during iteration
            {
                var name = kvp.Key;
                var feature = kvp.Value;

                try
                {
                    // Skip disabled features
                    if (!_enabled[name])
                        continue;

                    // Health check
                    if (feature is IHealthCheck healthCheck)
                    {
                        try
                        {
                            var wasHealthy = _healthy[name];
                            var isHealthy = healthCheck.IsHealthy();
                            _healthy[name] = isHealthy;

                            if (wasHealthy && !isHealthy)
                            {
                                // Feature became unhealthy - disable it
                                _enabled[name] = false;
                                Console.WriteLine($"[Nexus] Feature '{name}' disabled (unhealthy): {healthCheck.GetHealthStatus()}");

                                try
                                {
                                    EventChannel<HealthStatusChangedEvent>.Publish(
                                        new HealthStatusChangedEvent
                                        {
                                            Name = name,
                                            IsHealthy = false,
                                            Status = healthCheck.GetHealthStatus(),
                                            Timestamp = DateTime.UtcNow
                                        });
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[Nexus] Event publish failed: {ex.Message}");
                                }
                            }
                            else if (!wasHealthy && isHealthy)
                            {
                                // Feature recovered
                                Console.WriteLine($"[Nexus] Feature '{name}' recovered: {healthCheck.GetHealthStatus()}");

                                try
                                {
                                    EventChannel<HealthStatusChangedEvent>.Publish(
                                        new HealthStatusChangedEvent
                                        {
                                            Name = name,
                                            IsHealthy = true,
                                            Status = healthCheck.GetHealthStatus(),
                                            Timestamp = DateTime.UtcNow
                                        });
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[Nexus] Event publish failed: {ex.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Health check failed - assume unhealthy
                            _healthy[name] = false;
                            _enabled[name] = false;
                            Console.WriteLine($"[Nexus] Health check failed for '{name}': {ex.Message}");
                        }
                    }

                    // Update feature if still enabled and healthy
                    if (_enabled[name] && _healthy[name])
                    {
                        try
                        {
                            feature.Update(deltaTime);
                        }
                        catch (Exception ex)
                        {
                            // Update failed - disable feature
                            _enabled[name] = false;
                            _healthy[name] = false;
                            Console.WriteLine($"[Nexus] Update failed for '{name}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Catastrophic failure - log and continue
                    Console.WriteLine($"[Nexus] Critical error processing '{name}': {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Get diagnostic information about all features.
    /// Useful for debugging and monitoring.
    /// </summary>
    /// <returns>Dictionary of feature names to diagnostic info</returns>
    public Dictionary<string, string> GetDiagnostics()
    {
        lock (_lock)
        {
            var diagnostics = new Dictionary<string, string>();

            foreach (var kvp in _features)
            {
                var name = kvp.Key;
                var feature = kvp.Value;
                var enabled = _enabled[name];
                var healthy = _healthy[name];

                var status = feature is IHealthCheck healthCheck
                    ? healthCheck.GetHealthStatus()
                    : "No health check";

                diagnostics[name] = $"Enabled={enabled}, Healthy={healthy}, Status={status}";
            }

            return diagnostics;
        }
    }

    /// <summary>
    /// Shutdown all features and clear the registry.
    /// Call this when application is closing.
    /// </summary>
    public void ShutdownAll()
    {
        lock (_lock)
        {
            Console.WriteLine("[Nexus] Shutting down all features...");

            foreach (var kvp in _features.ToList())
            {
                try
                {
                    kvp.Value.Shutdown();
                    Console.WriteLine($"[Nexus] Shutdown feature: {kvp.Key}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Nexus] Failed to shutdown '{kvp.Key}': {ex.Message}");
                }
            }

            _features.Clear();
            _enabled.Clear();
            _healthy.Clear();

            Console.WriteLine("[Nexus] All features shut down");
        }
    }
}
