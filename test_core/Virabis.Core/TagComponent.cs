using Virabis.Core.Events;
using Virabis.Core.Tags;

namespace Virabis.Core;

/// <summary>
/// Manages entity state tags for context-aware mechanics.
/// Enhanced with hierarchical GameplayTags support.
///
/// v2.0: Integrated with Event Bus for decoupled tag notifications.
/// </summary>
public class TagComponent
{
    private readonly Entity _owner;

    // Legacy string-based tags (backward compatibility)
    private readonly HashSet<string> _legacyTags = new(StringComparer.OrdinalIgnoreCase);

    // New GameplayTag system
    private readonly GameplayTagContainer _gameplayTags = new();

    /// <summary>
    /// Access to the new GameplayTag system.
    /// </summary>
    public GameplayTagContainer GameplayTags => _gameplayTags;

    /// <summary>
    /// Creates a new TagComponent with owner reference.
    /// </summary>
    public TagComponent(Entity owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    // ========================================
    // LEGACY METHODS (String-based)
    // ========================================

    /// <summary>
    /// Adds a simple string tag (legacy).
    /// </summary>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        if (_legacyTags.Add(tag))
        {
            Events.Publish(new TagAddedEvent(_owner, tag, this));
        }
    }

    /// <summary>
    /// Removes a simple string tag (legacy).
    /// </summary>
    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        if (_legacyTags.Remove(tag))
        {
            Events.Publish(new TagRemovedEvent(_owner, tag, this));
        }
    }

    /// <summary>
    /// Checks for a simple string tag (legacy).
    /// </summary>
    public bool HasTag(string tag) => _legacyTags.Contains(tag);

    /// <summary>
    /// Gets all legacy string tags.
    /// </summary>
    public IReadOnlyCollection<string> GetTags() => _legacyTags;

    // ========================================
    // NEW GAMEPLAY TAG METHODS
    // ========================================

    /// <summary>
    /// Adds a GameplayTag.
    /// </summary>
    public void AddGameplayTag(GameplayTag tag)
    {
        if (tag == GameplayTag.None) return;

        _gameplayTags.AddTag(tag);

        // Also publish as string for backwards compatibility
        Events.Publish(new TagAddedEvent(_owner, tag.Name, this));
    }

    /// <summary>
    /// Adds multiple GameplayTags.
    /// </summary>
    public void AddGameplayTags(params GameplayTag[] tags)
    {
        foreach (var tag in tags)
            AddGameplayTag(tag);
    }

    /// <summary>
    /// Removes a GameplayTag.
    /// </summary>
    public void RemoveGameplayTag(GameplayTag tag)
    {
        if (tag == GameplayTag.None) return;

        _gameplayTags.RemoveTag(tag);
        Events.Publish(new TagRemovedEvent(_owner, tag.Name, this));
    }

    /// <summary>
    /// Checks for exact GameplayTag match.
    /// </summary>
    public bool HasGameplayTag(GameplayTag tag) => _gameplayTags.HasTag(tag);

    /// <summary>
    /// Checks for GameplayTag match (including parent matches).
    /// </summary>
    public bool HasGameplayTagMatch(GameplayTag tag) => _gameplayTags.HasTagMatch(tag);

    /// <summary>
    /// Checks for GameplayTag match by name (including parent matches).
    /// </summary>
    public bool HasGameplayTagMatch(string tagName) => _gameplayTags.HasTagMatch(GameplayTag.Create(tagName));

    /// <summary>
    /// Checks if has ANY of the specified tags.
    /// </summary>
    public bool HasAnyTags(params GameplayTag[] tags) => _gameplayTags.HasAny(tags);

    /// <summary>
    /// Checks if has ALL of the specified tags.
    /// </summary>
    public bool HasAllTags(params GameplayTag[] tags) => _gameplayTags.HasAll(tags);

    /// <summary>
    /// Checks if has NONE of the specified tags.
    /// </summary>
    public bool HasNoneOfTags(params GameplayTag[] tags) => _gameplayTags.HasNone(tags);

    // ========================================
    // COMBINED CHECK (Legacy + New)
    // ========================================

    /// <summary>
    /// Checks for tag (both legacy string and GameplayTag).
    /// </summary>
    public bool HasAnyTag(string legacyTag, GameplayTag gameplayTag)
        => HasTag(legacyTag) || HasGameplayTagMatch(gameplayTag);

    /// <summary>
    /// Checks if entity has any tag from a list (string or GameplayTag).
    /// </summary>
    public bool HasAnyTagFrom(params string[] tags)
    {
        foreach (var tag in tags)
        {
            if (HasTag(tag)) return true;
        }
        return false;
    }

    // ========================================
    // MODIFICATION METHODS
    // ========================================

    /// <summary>
    /// Toggles a tag on/off.
    /// </summary>
    public void ToggleTag(string tag, bool state)
    {
        if (state) AddTag(tag);
        else RemoveTag(tag);
    }

    /// <summary>
    /// Toggles a GameplayTag on/off.
    /// </summary>
    public void ToggleGameplayTag(GameplayTag tag, bool state)
    {
        if (state) AddGameplayTag(tag);
        else RemoveGameplayTag(tag);
    }

    /// <summary>
    /// Clears all tags (both legacy and gameplay).
    /// </summary>
    public void ClearAll()
    {
        var legacyTagsCopy = _legacyTags.ToList();
        foreach (var tag in legacyTagsCopy)
        {
            RemoveTag(tag);
        }

        var gameplayTagsCopy = _gameplayTags.Tags.ToList();
        foreach (var tag in gameplayTagsCopy)
        {
            RemoveGameplayTag(tag);
        }

        _legacyTags.Clear();
        _gameplayTags.Clear();
    }
}
