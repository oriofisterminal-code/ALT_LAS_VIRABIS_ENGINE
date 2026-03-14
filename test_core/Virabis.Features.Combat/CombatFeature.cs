using Virabis.Core;
using Virabis.Core.Nexus;

namespace Virabis.Features.Combat;

[VirabisFeature(
    ManifestPath = "core/Virabis.Features.Combat/combat_manifest.json",
    Capabilities = new[] { "damage_calculation", "health_tracking", "combat_events" },
    InitTimeMs = 50,
    LazyLoad = false,
    Priority = 100
)]
public class CombatFeature : IFeature, IHealthCheck
{
    /// <summary>
    /// Feature name identifier.
    /// </summary>
    public string Name => "Combat";

    private DamageSystem? _damageSystem;
    private float _lastFps;
    private int _frameCount;
    private const float MinHealthyFps = 45f;
    private const int WarmupFrames = 10; // Ignore first 10 frames
    private const float FpsSmoothingFactor = 0.9f; // Exponential smoothing

    /// <summary>
    /// Initialize the combat feature.
    /// Creates the DamageSystem with default random provider.
    /// </summary>
    public void Initialize()
    {
        _damageSystem = new DamageSystem(new DefaultRandomProvider());
        _lastFps = 60f; // Start with healthy FPS
        _frameCount = 0;
        Console.WriteLine("[Combat] Initialized");
    }

    /// <summary>
    /// Shutdown the combat feature.
    /// Cleans up resources.
    /// </summary>
    public void Shutdown()
    {
        _damageSystem = null;
        Console.WriteLine("[Combat] Shutdown");
    }

    /// <summary>
    /// Update the combat feature.
    /// Tracks FPS for health monitoring with smoothing and warmup period.
    /// </summary>
    /// <param name="deltaTime">Time since last frame in seconds</param>
    public void Update(float deltaTime)
    {
        _frameCount++;
        
        // Skip FPS tracking during warmup period to avoid initialization spikes
        if (_frameCount <= WarmupFrames)
        {
            return;
        }
        
        // Update FPS tracking with exponential smoothing (avoid division by zero)
        if (deltaTime > 0)
        {
            float instantFps = 1.0f / deltaTime;
            // Smooth FPS: new = old * factor + instant * (1 - factor)
            _lastFps = _lastFps * FpsSmoothingFactor + instantFps * (1.0f - FpsSmoothingFactor);
        }
    }

    /// <summary>
    /// Check if combat feature is healthy.
    /// Combat is healthy if FPS >= 45 (after warmup period).
    /// During warmup, always returns true.
    /// </summary>
    /// <returns>True if FPS is at or above minimum threshold or still warming up</returns>
    public bool IsHealthy()
    {
        // Always healthy during warmup period
        if (_frameCount <= WarmupFrames)
        {
            return true;
        }
        
        return _lastFps >= MinHealthyFps;
    }

    /// <summary>
    /// Get detailed health status message.
    /// </summary>
    /// <returns>Human-readable health status with FPS information</returns>
    public string GetHealthStatus()
    {
        return $"Combat: FPS={_lastFps:F1}, Healthy={IsHealthy()}";
    }

    /// <summary>
    /// Process a hit using the combat system.
    /// </summary>
    /// <param name="context">Hit context containing attacker, target, and damage info</param>
    /// <returns>Final damage dealt</returns>
    /// <exception cref="InvalidOperationException">If feature is not initialized</exception>
    [RequiresCapability("damage_calculation")]
    [RequiresCapability("combat_events")]
    public float ProcessHit(HitContext context)
    {
        if (_damageSystem == null)
        {
            throw new InvalidOperationException(
                "CombatFeature must be initialized before processing hits");
        }

        return _damageSystem.ProcessHit(context);
    }

    /// <summary>
    /// Get the underlying damage system.
    /// </summary>
    /// <returns>The DamageSystem instance</returns>
    /// <exception cref="InvalidOperationException">If feature is not initialized</exception>
    [RequiresCapability("damage_calculation")]
    public DamageSystem GetDamageSystem()
    {
        if (_damageSystem == null)
        {
            throw new InvalidOperationException(
                "CombatFeature must be initialized before accessing DamageSystem");
        }

        return _damageSystem;
    }
}
