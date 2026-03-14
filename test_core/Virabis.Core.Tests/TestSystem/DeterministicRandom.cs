namespace Virabis.Core.Tests.TestSystem;

/// <summary>
/// Deterministic random provider for testing.
/// Always returns the same value for predictable test outcomes.
/// </summary>
public class DeterministicRandom : IRandomProvider
{
    private readonly double _value;

    /// <summary>
    /// Creates a deterministic random provider that always returns the specified value.
    /// </summary>
    /// <param name="value">The value to return (0.0 to 1.0)</param>
    public DeterministicRandom(double value) => _value = value;

    /// <summary>
    /// Returns the configured value.
    /// </summary>
    public double NextDouble() => _value;
}
