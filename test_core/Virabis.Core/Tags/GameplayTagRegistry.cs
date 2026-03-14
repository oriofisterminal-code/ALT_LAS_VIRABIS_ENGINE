namespace Virabis.Core.Tags;

/// <summary>
/// Central registry for gameplay tags.
/// "Anahtar gibi, her tag'ın bir yeri var" - Dr. Lara Zeka (Sosyolog)
/// </summary>
public static class GameplayTagRegistry
{
    private static readonly Dictionary<string, GameplayTag> _registeredTags = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, HashSet<GameplayTag>> _children = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All registered tags.
    /// </summary>
    public static IReadOnlyCollection<GameplayTag> AllTags => _registeredTags.Values;

    /// <summary>
    /// Registers a tag in the system.
    /// </summary>
    public static GameplayTag Register(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return GameplayTag.None;

        if (_registeredTags.TryGetValue(tagName, out var existing))
            return existing;

        var tag = GameplayTag.Create(tagName);
        _registeredTags[tagName] = tag;

        // Track parent-child relationships
        if (tag.Parent.HasValue)
        {
            var parentName = tag.Parent.Value.Name;
            if (!_children.ContainsKey(parentName))
                _children[parentName] = new HashSet<GameplayTag>();
            _children[parentName].Add(tag);
        }

        return tag;
    }

    /// <summary>
    /// Registers multiple tags.
    /// </summary>
    public static void Register(params string[] tagNames)
    {
        foreach (var name in tagNames)
            Register(name);
    }

    /// <summary>
    /// Gets a registered tag by name, or creates it.
    /// </summary>
    public static GameplayTag GetOrAdd(string tagName) => Register(tagName);

    /// <summary>
    /// Gets a registered tag, returns None if not found.
    /// </summary>
    public static GameplayTag Get(string tagName)
    {
        return _registeredTags.TryGetValue(tagName, out var tag) ? tag : GameplayTag.None;
    }

    /// <summary>
    /// Gets all direct children of a tag.
    /// </summary>
    public static IEnumerable<GameplayTag> GetChildren(string tagName)
    {
        return _children.TryGetValue(tagName, out var children)
            ? children
            : Enumerable.Empty<GameplayTag>();
    }

    /// <summary>
    /// Gets all descendants of a tag (recursive).
    /// </summary>
    public static IEnumerable<GameplayTag> GetAllDescendants(string tagName)
    {
        var result = new List<GameplayTag>();
        CollectDescendants(tagName, result);
        return result;
    }

    private static void CollectDescendants(string tagName, List<GameplayTag> result)
    {
        foreach (var child in GetChildren(tagName))
        {
            result.Add(child);
            CollectDescendants(child.Name, result);
        }
    }

    /// <summary>
    /// Clears all registered tags (for testing).
    /// </summary>
    public static void Clear()
    {
        _registeredTags.Clear();
        _children.Clear();
    }
}

/// <summary>
/// Pre-defined tag categories for common use cases.
/// </summary>
public static class GameplayTags
{
    // ========================================
    // ENTITY TYPE TAGS
    // ========================================

    public static class Entity
    {
        public static GameplayTag Player => "Entity.Player";
        public static GameplayTag Enemy => "Entity.Enemy";
        public static CharacterTags Character => new();
        public static GameplayTag NPC => "Entity.NPC";
        public static GameplayTag Object => "Entity.Object";
    }

    public class CharacterTags
    {
        public GameplayTag Hero => "Entity.Character.Hero";
        public GameplayTag Boss => "Entity.Character.Boss";
        public GameplayTag Elite => "Entity.Character.Elite";
        public GameplayTag Minion => "Entity.Character.Minion";
        public GameplayTag Flying => "Entity.Character.Flying";
        public GameplayTag Ground => "Entity.Character.Ground";
    }

    // ========================================
    // STATUS EFFECT TAGS
    // ========================================

    public static class Status
    {
        public static GameplayTag Alive => "Status.Alive";
        public static GameplayTag Dead => "Status.Dead";
        public static GameplayTag Stunned => "Status.Stunned";
        public static GameplayTag Invisible => "Status.Invisible";
        public static GameplayTag Invulnerable => "Status.Invulnerable";
        public static DebuffTags Debuff => new();
        public static BuffTags Buff => new();
    }

    public class DebuffTags
    {
        public GameplayTag Poisoned => "Status.Debuff.Poisoned";
        public GameplayTag Burning => "Status.Debuff.Burning";
        public GameplayTag Frozen => "Status.Debuff.Frozen";
        public GameplayTag Slowed => "Status.Debuff.Slowed";
        public GameplayTag Bleeding => "Status.Debuff.Bleeding";
        public GameplayTag Cursed => "Status.Debuff.Cursed";
    }

    public class BuffTags
    {
        public GameplayTag Shielded => "Status.Buff.Shielded";
        public GameplayTag Hasted => "Status.Buff.Hasted";
        public GameplayTag Strengthened => "Status.Buff.Strengthened";
        public GameplayTag Regenerating => "Status.Buff.Regenerating";
    }

    // ========================================
    // COMBAT TAGS
    // ========================================

    public static class Combat
    {
        public static GameplayTag Attacking => "Combat.Attacking";
        public static GameplayTag Defending => "Combat.Defending";
        public static GameplayTag Dodging => "Combat.Dodging";
        public static GameplayTag CriticalHit => "Combat.CriticalHit";
        public static GameplayTag Stealthed => "Combat.Stealthed";
        public static GameplayTag Aware => "Combat.Aware";
    }

    // ========================================
    // AI TAGS
    // ========================================

    public static class AI
    {
        public static GameplayTag Idle => "AI.Idle";
        public static GameplayTag Patrolling => "AI.Patrolling";
        public static GameplayTag Chasing => "AI.Chasing";
        public static GameplayTag Attacking => "AI.Attacking";
        public static GameplayTag Fleeing => "AI.Fleeing";
        public static GameplayTag Alerted => "AI.Alerted";
    }

    /// <summary>
    /// Registers all predefined tags at once.
    /// </summary>
    public static void RegisterAll()
    {
        // Entity tags
        Register(Entity.Player, Entity.Enemy, Entity.NPC, Entity.Object);
        Register(Entity.Character.Hero, Entity.Character.Boss, Entity.Character.Elite);
        Register(Entity.Character.Minion, Entity.Character.Flying, Entity.Character.Ground);

        // Status tags
        Register(Status.Alive, Status.Dead, Status.Stunned, Status.Invisible, Status.Invulnerable);
        Register(Status.Debuff.Poisoned, Status.Debuff.Burning, Status.Debuff.Frozen);
        Register(Status.Debuff.Slowed, Status.Debuff.Bleeding, Status.Debuff.Cursed);
        Register(Status.Buff.Shielded, Status.Buff.Hasted, Status.Buff.Strengthened, Status.Buff.Regenerating);

        // Combat tags
        Register(Combat.Attacking, Combat.Defending, Combat.Dodging);
        Register(Combat.CriticalHit, Combat.Stealthed, Combat.Aware);

        // AI tags
        Register(AI.Idle, AI.Patrolling, AI.Chasing, AI.Attacking, AI.Fleeing, AI.Alerted);
    }

    private static void Register(params GameplayTag[] tags)
    {
        foreach (var tag in tags)
            GameplayTagRegistry.Register(tag.Name);
    }
}
