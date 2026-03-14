namespace Virabis.Core;

/// <summary>
/// Default random provider using System.Random.
/// Provides random number generation for combat systems (crit rolls, etc.).
/// </summary>
public sealed class DefaultRandomProvider : IRandomProvider
{
    private readonly Random _random = new();

    /// <summary>
    /// Generates a random double between 0.0 and 1.0.
    /// </summary>
    /// <returns>Random value in range [0.0, 1.0)</returns>
    public double NextDouble() => _random.NextDouble();
}
