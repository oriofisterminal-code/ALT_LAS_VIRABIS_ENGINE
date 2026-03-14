namespace Virabis.Core.Tags;

/// <summary>
/// Represents a hierarchical gameplay tag (e.g., "Enemy.Boss.Flying").
/// Inspired by Unreal Engine's Gameplay Tags system.
///
/// v2.0: Added thread-safe caching to prevent repeated allocations.
/// "Cimriyim, gereksiz string yazmayalım" - Ayşe Döner (Muhasebeci)
/// </summary>
public readonly struct GameplayTag : IEquatable<GameplayTag>, IComparable<GameplayTag>
{
    // Thread-safe cache for tag instances
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, GameplayTag> Cache = new();

    /// <summary>
    /// The full tag name (e.g., "Enemy.Boss.Flying")
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Hash for fast comparison
    /// </summary>
    public int Hash { get; }

    /// <summary>
    /// Parent tag (e.g., "Enemy.Boss" for "Enemy.Boss.Flying")
    /// </summary>
    public GameplayTag? Parent { get; }

    /// <summary>
    /// Depth in hierarchy (0 = root, 1 = one level deep, etc.)
    /// </summary>
    public int Depth { get; }

    /// <summary>
    /// Empty/invalid tag
    /// </summary>
    public static readonly GameplayTag None = new(string.Empty);

    private GameplayTag(string name, GameplayTag? parent = null)
    {
        Name = name;
        Hash = name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        Parent = parent;
        Depth = parent.HasValue ? parent.Value.Depth + 1 : name.Length == 0 ? -1 : 0;
    }

    /// <summary>
    /// Creates a GameplayTag from a dot-separated string.
    /// Uses caching to return the same instance for identical tag names.
    /// </summary>
    /// <param name="name">Tag name (e.g., "Enemy.Boss.Flying")</param>
    /// <returns>Cached or new GameplayTag instance</returns>
    public static GameplayTag Create(string name)
    {
        if (string.IsNullOrEmpty(name))
            return None;

        name = name.Trim();

        // Use cached version if available
        return Cache.GetOrAdd(name, n => BuildTag(n));
    }

    /// <summary>
    /// Tries to get a cached tag without creating a new one.
    /// </summary>
    public static bool TryGetCached(string name, out GameplayTag tag)
    {
        if (string.IsNullOrEmpty(name))
        {
            tag = None;
            return false;
        }

        return Cache.TryGetValue(name.Trim(), out tag);
    }

    /// <summary>
    /// Clears the tag cache (for testing purposes).
    /// </summary>
    public static void ClearCache()
    {
        Cache.Clear();
    }

    /// <summary>
    /// Gets the number of cached tags.
    /// </summary>
    public static int CacheCount => Cache.Count;

    private static GameplayTag BuildTag(string name)
    {
        var parts = name.Split('.');

        if (parts.Length == 1)
            return new GameplayTag(name);

        // Build parent chain (parents are also cached)
        GameplayTag? parent = null;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var parentName = string.Join('.', parts.Take(i + 1));
            // Recursively get or create parent (will use cache)
            parent = Cache.GetOrAdd(parentName, n => BuildTagInternal(n, parent));
        }

        return new GameplayTag(name, parent);
    }

    private static GameplayTag BuildTagInternal(string name, GameplayTag? existingParent)
    {
        return new GameplayTag(name, existingParent);
    }

    /// <summary>
    /// Checks if this tag matches another tag (exact match or is parent of).
    /// "Enemy" matches "Enemy.Boss" and "Enemy.Boss.Flying"
    /// </summary>
    public bool Matches(GameplayTag other)
    {
        if (Hash == other.Hash && Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if we are a parent of the other tag
        var currentParent = other.Parent;
        while (currentParent.HasValue)
        {
            if (Hash == currentParent.Value.Hash &&
                Name.Equals(currentParent.Value.Name, StringComparison.OrdinalIgnoreCase))
                return true;
            currentParent = currentParent.Value.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks if this tag is a child of another tag.
    /// </summary>
    public bool IsChildOf(GameplayTag potentialParent) => potentialParent.Matches(this);

    /// <summary>
    /// Checks if this tag is a parent of another tag.
    /// </summary>
    public bool IsParentOf(GameplayTag potentialChild) => Matches(potentialChild);

    /// <summary>
    /// Gets all parent tags up to root.
    /// </summary>
    public IEnumerable<GameplayTag> GetParents()
    {
        var current = Parent;
        while (current.HasValue)
        {
            yield return current.Value;
            current = current.Value.Parent;
        }
    }

    /// <summary>
    /// Checks if this tag is a valid (non-empty) tag.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(Name);

    // IEquatable
    public bool Equals(GameplayTag other) => Hash == other.Hash && Name == other.Name;
    public override bool Equals(object? obj) => obj is GameplayTag tag && Equals(tag);
    public override int GetHashCode() => Hash;

    // IComparable
    public int CompareTo(GameplayTag other) => string.Compare(Name, other.Name, StringComparison.Ordinal);

    // Operators
    public static bool operator ==(GameplayTag left, GameplayTag right) => left.Equals(right);
    public static bool operator !=(GameplayTag left, GameplayTag right) => !left.Equals(right);

    public override string ToString() => Name;

    /// <summary>
    /// Implicit conversion from string for convenience.
    /// </summary>
    public static implicit operator GameplayTag(string name) => Create(name);
}
