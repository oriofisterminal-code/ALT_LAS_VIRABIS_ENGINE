namespace Virabis.Core;

/// <summary>
/// Configuration interface for damage calculation parameters.
/// Replaces magic numbers with configurable values.
/// </summary>
/// <remarks>
/// v3.0: Added to eliminate magic numbers from DamageSystem.
///
/// Default values:
/// - StealthMultiplier: 2.0x
/// - CritChanceCap: 1.0 (100%)
/// - MinDamage: 0.0
///
/// Example configuration in appsettings.json:
/// <code>
/// {
///   "Damage": {
///     "StealthMultiplier": 2.5,
///     "FriendlyFireEnabled": true
///   }
/// }
/// </code>
/// </remarks>
public interface IDamageConfiguration
{
    /// <summary>
    /// Multiplier applied to stealth attacks when target is unaware.
    /// Default: 2.0x
    /// </summary>
    float StealthMultiplier { get; }

    /// <summary>
    /// Maximum allowed critical hit chance (0.0 to 1.0).
    /// Prevents 100%+ crit chance exploits.
    /// Default: 1.0
    /// </summary>
    float CritChanceCap { get; }

    /// <summary>
    /// Minimum damage that can be dealt.
    /// Default: 0.0
    /// </summary>
    float MinDamage { get; }

    /// <summary>
    /// Maximum damage that can be dealt in a single hit.
    /// Default: float.MaxValue (no limit)
    /// </summary>
    float MaxDamageCap { get; }

    /// <summary>
    /// Whether friendly fire is enabled.
    /// Default: false
    /// </summary>
    bool FriendlyFireEnabled { get; }
}
