using Virabis.Core.State;
using Virabis.Core.Tags;

namespace Virabis.Core.Templates;

/// <summary>
/// Template for creating entities with predefined configurations.
/// "Yamaç paraşütü gibi - bir kere tanımla, defalarca kullan" - Zeynep Kod (Aşçı)
///
/// v2.0: Added validation for all parameters.
/// </summary>
public class EntityTemplate
{
    private readonly string _name;
    private float _maxHealth = 100f;
    private TeamId _teamId = TeamId.Enemy;
    private float _critChance;
    private float _critMultiplier = 2f;
    private readonly List<string> _legacyTags = new();
    private readonly List<string> _gameplayTags = new();
    private readonly List<Action<Entity>> _configureActions = new();
    private StateConfig _stateConfig = StateConfig.None;

    /// <summary>
    /// Template name for identification.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Creates a new entity template with the given name.
    /// </summary>
    /// <param name="name">Template name (required)</param>
    /// <exception cref="ArgumentNullException">When name is null or empty</exception>
    public EntityTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name), "Template name cannot be null or empty");

        _name = name;
    }

    // ========================================
    // FLUENT CONFIGURATION
    // ========================================

    /// <summary>
    /// Sets the maximum health.
    /// </summary>
    /// <param name="maxHealth">Maximum health (must be positive)</param>
    /// <exception cref="ArgumentOutOfRangeException">When maxHealth is not positive</exception>
    public EntityTemplate WithHealth(float maxHealth)
    {
        if (maxHealth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be greater than 0");

        _maxHealth = maxHealth;
        return this;
    }

    /// <summary>
    /// Sets the team.
    /// </summary>
    public EntityTemplate WithTeam(TeamId teamId)
    {
        _teamId = teamId;
        return this;
    }

    /// <summary>
    /// Sets as Player team.
    /// </summary>
    public EntityTemplate AsPlayer()
    {
        _teamId = TeamId.Player;
        return this;
    }

    /// <summary>
    /// Sets as Enemy team.
    /// </summary>
    public EntityTemplate AsEnemy()
    {
        _teamId = TeamId.Enemy;
        return this;
    }

    /// <summary>
    /// Sets crit chance and multiplier.
    /// </summary>
    /// <param name="chance">Crit chance (0.0 to 1.0)</param>
    /// <param name="multiplier">Crit multiplier (minimum 1.0)</param>
    /// <exception cref="ArgumentOutOfRangeException">When parameters are out of valid range</exception>
    public EntityTemplate WithCrit(float chance, float multiplier = 2f)
    {
        if (chance < 0f || chance > 1f)
            throw new ArgumentOutOfRangeException(nameof(chance), chance, "Crit chance must be between 0.0 and 1.0");

        if (multiplier < 1f)
            throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "Crit multiplier must be at least 1.0");

        _critChance = chance;
        _critMultiplier = multiplier;
        return this;
    }

    /// <summary>
    /// Adds a legacy string tag.
    /// </summary>
    /// <param name="tag">Tag to add (cannot be null or empty)</param>
    public EntityTemplate WithTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentNullException(nameof(tag), "Tag cannot be null or empty");

        _legacyTags.Add(tag);
        return this;
    }

    /// <summary>
    /// Adds multiple legacy string tags.
    /// </summary>
    /// <param name="tags">Tags to add</param>
    public EntityTemplate WithTags(params string[] tags)
    {
        if (tags == null || tags.Length == 0)
            return this;

        foreach (var tag in tags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
                _legacyTags.Add(tag);
        }
        return this;
    }

    /// <summary>
    /// Adds a GameplayTag by name.
    /// </summary>
    /// <param name="tagName">GameplayTag name (e.g., "Entity.Character.Boss")</param>
    public EntityTemplate WithGameplayTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentNullException(nameof(tagName), "GameplayTag name cannot be null or empty");

        _gameplayTags.Add(tagName);
        return this;
    }

    /// <summary>
    /// Enables combatant states (Idle, Moving, Attacking, Stunned, Dead).
    /// </summary>
    public EntityTemplate WithCombatantStates()
    {
        _stateConfig = StateConfig.Combatant;
        return this;
    }

    /// <summary>
    /// Enables AI states (Idle, Patrolling, Chasing, Attacking, Fleeing, Alerted, Dead).
    /// </summary>
    public EntityTemplate WithAIStates()
    {
        _stateConfig = StateConfig.AI;
        return this;
    }

    /// <summary>
    /// Custom configuration action.
    /// </summary>
    /// <param name="configure">Configuration action (cannot be null)</param>
    public EntityTemplate Configure(Action<Entity> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        _configureActions.Add(configure);
        return this;
    }

    // ========================================
    // CREATION
    // ========================================

    /// <summary>
    /// Creates a new entity from this template.
    /// </summary>
    /// <returns>New entity instance configured according to template</returns>
    public Entity Instantiate()
    {
        var entity = new Entity(_teamId, _maxHealth);

        // Apply stats
        entity.Stats.CritChance = _critChance;
        entity.Stats.CritMultiplier = _critMultiplier;

        // Apply legacy tags
        foreach (var tag in _legacyTags)
        {
            entity.Tags.AddTag(tag);
        }

        // Apply gameplay tags
        foreach (var tagName in _gameplayTags)
        {
            var gTag = GameplayTag.Create(tagName);
            entity.Tags.AddGameplayTag(gTag);
        }

        // Apply state config
        switch (_stateConfig)
        {
            case StateConfig.Combatant:
                entity.InitializeCombatantStates();
                break;
            case StateConfig.AI:
                entity.InitializeAIStates();
                break;
        }

        // Apply custom configurations
        foreach (var action in _configureActions)
        {
            action(entity);
        }

        return entity;
    }

    /// <summary>
    /// Creates multiple entities from this template.
    /// </summary>
    /// <param name="count">Number of entities to create (must be positive)</param>
    /// <exception cref="ArgumentOutOfRangeException">When count is not positive</exception>
    public IEnumerable<Entity> Instantiate(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be greater than 0");

        for (int i = 0; i < count; i++)
        {
            yield return Instantiate();
        }
    }

    // ========================================
    // PREDEFINED TEMPLATES
    // ========================================

    /// <summary>
    /// Default player template.
    /// </summary>
    public static EntityTemplate Player(float health = 100f)
        => new EntityTemplate("Player")
            .AsPlayer()
            .WithHealth(health)
            .WithCombatantStates();

    /// <summary>
    /// Default enemy template.
    /// </summary>
    public static EntityTemplate Enemy(float health = 100f)
        => new EntityTemplate("Enemy")
            .AsEnemy()
            .WithHealth(health)
            .WithAIStates();

    /// <summary>
    /// Boss template with high health.
    /// </summary>
    public static EntityTemplate Boss(float health = 500f)
        => new EntityTemplate("Boss")
            .AsEnemy()
            .WithHealth(health)
            .WithTags("boss", "aware")
            .WithGameplayTag("Entity.Character.Boss")
            .WithAIStates();

    /// <summary>
    /// Minion template with low health.
    /// </summary>
    public static EntityTemplate Minion(float health = 30f)
        => new EntityTemplate("Minion")
            .AsEnemy()
            .WithHealth(health)
            .WithTags("minion")
            .WithAIStates();

    /// <summary>
    /// Elite enemy template.
    /// </summary>
    public static EntityTemplate Elite(float health = 200f, float critChance = 0.2f)
        => new EntityTemplate("Elite")
            .AsEnemy()
            .WithHealth(health)
            .WithCrit(critChance, 2.5f)
            .WithTags("elite", "aware")
            .WithAIStates();

    private enum StateConfig
    {
        None,
        Combatant,
        AI
    }
}
