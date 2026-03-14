namespace Virabis.Core.Tags;

/// <summary>
/// Fluent query builder for gameplay tags.
/// "Satranç gibi, hamleleri önceden düşün" - Can Hızlı (Taksi Şoförü)
///
/// Usage:
/// <code>
/// var query = TagQuery.HasAny(EnemyTags.Boss, EnemyTags.Elite)
///                     .HasNone(StatusTags.Dead)
///                     .HasAll(CombatTags.Aware);
/// bool matches = query.Matches(container);
/// </code>
/// </summary>
public class TagQuery
{
    private readonly List<GameplayTag> _hasAny = new();
    private readonly List<GameplayTag> _hasAll = new();
    private readonly List<GameplayTag> _hasNone = new();

    /// <summary>
    /// Creates a new empty query.
    /// </summary>
    public static TagQuery Create() => new();

    /// <summary>
    /// Requires ANY of the specified tags.
    /// </summary>
    public TagQuery HasAny(params GameplayTag[] tags)
    {
        _hasAny.AddRange(tags.Where(t => t != GameplayTag.None));
        return this;
    }

    /// <summary>
    /// Requires ALL of the specified tags.
    /// </summary>
    public TagQuery HasAll(params GameplayTag[] tags)
    {
        _hasAll.AddRange(tags.Where(t => t != GameplayTag.None));
        return this;
    }

    /// <summary>
    /// Requires NONE of the specified tags.
    /// </summary>
    public TagQuery HasNone(params GameplayTag[] tags)
    {
        _hasNone.AddRange(tags.Where(t => t != GameplayTag.None));
        return this;
    }

    /// <summary>
    /// Evaluates this query against a tag container.
    /// </summary>
    public bool Matches(GameplayTagContainer container)
    {
        // Check HasAny (if any specified, must match at least one)
        if (_hasAny.Count > 0 && !container.HasAny(_hasAny.ToArray()))
            return false;

        // Check HasAll (must match all specified)
        if (_hasAll.Count > 0 && !container.HasAll(_hasAll.ToArray()))
            return false;

        // Check HasNone (must not have any of these)
        if (_hasNone.Count > 0 && !container.HasNone(_hasNone.ToArray()))
            return false;

        return true;
    }

    /// <summary>
    /// Evaluates this query against a TagComponent (legacy support).
    /// </summary>
    public bool Matches(TagComponent tagComponent)
    {
        var container = new GameplayTagContainer();
        foreach (var tag in tagComponent.GetTags())
            container.AddTag(tag);
        return Matches(container);
    }

    /// <summary>
    /// Combines this query with another (AND operation).
    /// </summary>
    public TagQuery And(TagQuery other)
    {
        _hasAny.AddRange(other._hasAny);
        _hasAll.AddRange(other._hasAll);
        _hasNone.AddRange(other._hasNone);
        return this;
    }

    /// <summary>
    /// Clears all conditions.
    /// </summary>
    public void Clear()
    {
        _hasAny.Clear();
        _hasAll.Clear();
        _hasNone.Clear();
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (_hasAny.Count > 0)
            parts.Add($"HasAny:[{string.Join(",", _hasAny.Select(t => t.Name))}]");
        if (_hasAll.Count > 0)
            parts.Add($"HasAll:[{string.Join(",", _hasAll.Select(t => t.Name))}]");
        if (_hasNone.Count > 0)
            parts.Add($"HasNone:[{string.Join(",", _hasNone.Select(t => t.Name))}]");
        return $"Query({string.Join(" & ", parts)})";
    }
}

/// <summary>
/// Extension methods for easy querying.
/// </summary>
public static class TagQueryExtensions
{
    /// <summary>
    /// Creates a query for this container.
    /// </summary>
    public static TagQuery Query(this GameplayTagContainer container) => TagQuery.Create();

    /// <summary>
    /// Quick check: Has any of these tags?
    /// </summary>
    public static bool HasAnyTags(this GameplayTagContainer container, params GameplayTag[] tags)
        => container.HasAny(tags);

    /// <summary>
    /// Quick check: Has all of these tags?
    /// </summary>
    public static bool HasAllTags(this GameplayTagContainer container, params GameplayTag[] tags)
        => container.HasAll(tags);

    /// <summary>
    /// Quick check: Has none of these tags?
    /// </summary>
    public static bool HasNoneOfTags(this GameplayTagContainer container, params GameplayTag[] tags)
        => container.HasNone(tags);
}
