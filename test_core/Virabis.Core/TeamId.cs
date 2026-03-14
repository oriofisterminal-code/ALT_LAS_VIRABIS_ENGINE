namespace Virabis.Core;

/// <summary>
/// Strongly-typed team identifier for type safety and domain clarity.
/// </summary>
public readonly record struct TeamId(int Value)
{
    public static TeamId None => new(-1);
    public static TeamId Player => new(0);
    public static TeamId Enemy => new(1);
}
