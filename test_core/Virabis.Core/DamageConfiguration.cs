namespace Virabis.Core;

/// <summary>
/// Default implementation of damage configuration.
/// Provides sensible defaults while allowing customization.
/// </summary>
/// <remarks>
/// v3.0: Extracted from DamageSystem magic numbers.
///
/// For game balancing, adjust these values:
/// - Increase StealthMultiplier for more rewarding stealth gameplay
/// - Lower CritChanceCap to prevent RNG dominance
/// - Set MaxDamageCap for PvP balance
///
/// Usage:
/// <code>
/// // Default configuration
/// var config = DamageConfiguration.Default;
///
/// // Hardcore mode
/// var hardcore = DamageConfiguration.Hardcore;
///
/// // Custom configuration
/// var custom = new DamageConfiguration
/// {
///     StealthMultiplier = 3.0f,
///     FriendlyFireEnabled = true
/// };
/// </code>
/// </remarks>
public class DamageConfiguration : IDamageConfiguration
{
    /// <inheritdoc />
    public float StealthMultiplier { get; set; } = 2.0f;

    /// <inheritdoc />
    public float CritChanceCap { get; set; } = 1.0f;

    /// <inheritdoc />
    public float MinDamage { get; set; } = 0.0f;

    /// <inheritdoc />
    public float MaxDamageCap { get; set; } = float.MaxValue;

    /// <inheritdoc />
    public bool FriendlyFireEnabled { get; set; } = false;

    /// <summary>
    /// Creates default configuration with standard game balance values.
    /// </summary>
    public static DamageConfiguration Default => new();

    /// <summary>
    /// Creates configuration for hardcore mode (higher damage, no friendly fire protection).
    /// </summary>
    public static DamageConfiguration Hardcore => new()
    {
        StealthMultiplier = 3.0f,
        FriendlyFireEnabled = true
    };

    /// <summary>
    /// Creates configuration for casual mode (lower damage, more forgiving).
    /// </summary>
    public static DamageConfiguration Casual => new()
    {
        StealthMultiplier = 1.5f,
        MaxDamageCap = 500f
    };

    /// <summary>
    /// Creates configuration for testing (deterministic values).
    /// </summary>
    public static DamageConfiguration Test => new()
    {
        StealthMultiplier = 2.0f,
        CritChanceCap = 1.0f,
        MaxDamageCap = 1000f
    };
}
