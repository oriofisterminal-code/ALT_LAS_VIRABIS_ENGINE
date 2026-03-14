namespace Virabis.Core;

/// <summary>
/// Abstraction for random number generation to enable deterministic testing.
/// </summary>
public interface IRandomProvider
{
    double NextDouble();
}

/// <summary>
/// Production implementation using System.Random.
/// </summary>
public class SystemRandomProvider : IRandomProvider
{
    private readonly Random _random = new();
    public double NextDouble() => _random.NextDouble();
}
