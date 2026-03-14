namespace Virabis.Core.Tags;

/// <summary>
/// Container for managing multiple gameplay tags with query support.
/// "Origami gibi kat kat, hiyerarşik" - Ayşe Döner (Muhasebeci)
/// </summary>
public class GameplayTagContainer
{
    private readonly HashSet<GameplayTag> _tags = new();

    /// <summary>
    /// Number of tags in container.
    /// </summary>
    public int Count => _tags.Count;

    /// <summary>
    /// All tags in this container.
    /// </summary>
    public IReadOnlyCollection<GameplayTag> Tags => _tags;

    /// <summary>
    /// Adds a tag to the container.
    /// </summary>
    public void AddTag(GameplayTag tag)
    {
        if (tag != GameplayTag.None)
            _tags.Add(tag);
    }

    /// <summary>
    /// Adds multiple tags to the container.
    /// </summary>
    public void AddTags(params GameplayTag[] tags)
    {
        foreach (var tag in tags)
            AddTag(tag);
    }

    /// <summary>
    /// Removes a tag from the container.
    /// </summary>
    public void RemoveTag(GameplayTag tag) => _tags.Remove(tag);

    /// <summary>
    /// Removes multiple tags from the container.
    /// </summary>
    public void RemoveTags(params GameplayTag[] tags)
    {
        foreach (var tag in tags)
            _tags.Remove(tag);
    }

    /// <summary>
    /// Checks if container has exact tag.
    /// </summary>
    public bool HasTag(GameplayTag tag) => _tags.Contains(tag);

    /// <summary>
    /// Checks if container has a tag that matches (including parent matches).
    /// Example: HasTagMatch("Enemy") returns true if container has "Enemy.Boss"
    /// </summary>
    public bool HasTagMatch(GameplayTag tag)
    {
        foreach (var t in _tags)
        {
            if (t.Matches(tag) || tag.Matches(t))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if container has ANY of the specified tags (OR operation).
    /// </summary>
    public bool HasAny(params GameplayTag[] tags)
    {
        foreach (var tag in tags)
        {
            if (HasTagMatch(tag))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if container has ALL of the specified tags (AND operation).
    /// </summary>
    public bool HasAll(params GameplayTag[] tags)
    {
        foreach (var tag in tags)
        {
            if (!HasTagMatch(tag))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Checks if container has NONE of the specified tags.
    /// </summary>
    public bool HasNone(params GameplayTag[] tags) => !HasAny(tags);

    /// <summary>
    /// Clears all tags.
    /// </summary>
    public void Clear() => _tags.Clear();

    /// <summary>
    /// Creates a copy of this container.
    /// </summary>
    public GameplayTagContainer Clone()
    {
        var clone = new GameplayTagContainer();
        foreach (var tag in _tags)
            clone.AddTag(tag);
        return clone;
    }

    /// <summary>
    /// Gets all matching tags from a query.
    /// </summary>
    public IEnumerable<GameplayTag> GetMatchingTags(GameplayTag query)
    {
        foreach (var tag in _tags)
        {
            if (tag.Matches(query) || query.Matches(tag))
                yield return tag;
        }
    }

    public override string ToString() => $"[{string.Join(", ", _tags.Select(t => t.Name))}]";
}
