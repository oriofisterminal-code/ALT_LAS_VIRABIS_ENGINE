using Virabis.Core.Components;
using Virabis.Core.Events;
using Virabis.Core.State;

namespace Virabis.Core;

/// <summary>
/// Represents any combat-capable entity in the game world.
/// v5.0: Full component system with event-driven architecture.
/// </summary>
public class Entity : IDisposable
{
    private bool _disposed;
    private readonly Dictionary<Type, IComponent> _components = new();
    private readonly List<IComponent> _updateableComponents = new();

    /// <summary>
    /// Unique identifier for this entity.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Team affiliation for friendly fire detection.
    /// </summary>
    public TeamId TeamId { get; set; }

    /// <summary>
    /// Legacy health component (use GetComponent&lt;HealthComponent&gt;() for new code).
    /// </summary>
    public HealthComponent Health { get; private set; }

    /// <summary>
    /// Legacy stats component (use GetComponent&lt;StatComponent&gt;() for new code).
    /// </summary>
    public StatComponent Stats { get; private set; }

    /// <summary>
    /// Legacy tags component (use GetComponent&lt;TagComponent&gt;() for new code).
    /// </summary>
    public TagComponent Tags { get; private set; }

    /// <summary>
    /// State machine for managing entity states.
    /// </summary>
    public StateMachine? State { get; private set; }

    /// <summary>
    /// Custom data attached to this entity.
    /// </summary>
    public Dictionary<string, object> UserData { get; } = new();

    /// <summary>
    /// Number of components attached.
    /// </summary>
    public int ComponentCount => _components.Count;

    /// <summary>
    /// Whether this entity has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    // ========================================
    // CONSTRUCTORS
    // ========================================

    /// <summary>
    /// Creates a new Entity with auto-generated ID.
    /// </summary>
    public Entity(TeamId teamId, float maxHealth)
    {
        Id = Guid.NewGuid();
        TeamId = teamId;

        // Create core components
        Health = new HealthComponent(maxHealth) { Entity = this };
        Stats = new StatComponent { Entity = this };
        Tags = new TagComponent { Entity = this };

        _components[typeof(HealthComponent)] = Health;
        _components[typeof(StatComponent)] = Stats;
        _components[typeof(TagComponent)] = Tags;

        // Publish creation event
        Events.Publish(new EntityCreatedEvent(this, this));
    }

    /// <summary>
    /// Creates a new Entity with a specific ID (factory pattern support).
    /// </summary>
    internal Entity(Guid id, TeamId teamId, float maxHealth)
    {
        Id = id;
        TeamId = teamId;

        Health = new HealthComponent(maxHealth) { Entity = this };
        Stats = new StatComponent { Entity = this };
        Tags = new TagComponent { Entity = this };

        _components[typeof(HealthComponent)] = Health;
        _components[typeof(StatComponent)] = Stats;
        _components[typeof(TagComponent)] = Tags;

        Events.Publish(new EntityCreatedEvent(this, this));
    }

    /// <summary>
    /// Factory method for creating entities with specific IDs.
    /// </summary>
    public static Entity CreateWithId(Guid id, TeamId teamId, float maxHealth)
    {
        return new Entity(id, teamId, maxHealth);
    }

    // ========================================
    // COMPONENT SYSTEM
    // ========================================

    /// <summary>
    /// Adds a component to this entity.
    /// </summary>
    public T AddComponent<T>(T component) where T : class, IComponent
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Entity));

        if (component == null)
            throw new ArgumentNullException(nameof(component));

        var type = typeof(T);

        // Check if already exists
        if (_components.ContainsKey(type))
            throw new InvalidOperationException($"Entity already has component of type {type.Name}");

        // Check dependencies
        var requires = ComponentRegistry.GetRequiredComponents(type);
        foreach (var required in requires)
        {
            if (!_components.ContainsKey(required))
            {
                // Auto-add if attribute present
                if (type.GetCustomAttributes(typeof(AutoAddDependenciesAttribute), false).Any())
                {
                    var instance = ComponentRegistry.CreateInstance(required);
                    if (instance != null)
                    {
                        var addMethod = typeof(Entity).GetMethod(nameof(AddComponent))!;
                        addMethod.MakeGenericMethod(required).Invoke(this, new[] { instance });
                    }
                }
                else
                {
                    throw new InvalidOperationException($"{type.Name} requires {required.Name}");
                }
            }
        }

        component.Entity = this;
        _components[type] = component;

        if (component is ComponentBase cb && cb.IsEnabled)
        {
            _updateableComponents.Add(component);
        }

        component.OnAdded();

        return component;
    }

    /// <summary>
    /// Adds a new component instance.
    /// </summary>
    public T AddComponent<T>() where T : class, IComponent, new()
    {
        return AddComponent(new T());
    }

    /// <summary>
    /// Gets a component by type.
    /// </summary>
    public T? GetComponent<T>() where T : class, IComponent
    {
        return _components.TryGetValue(typeof(T), out var component) ? component as T : null;
    }

    /// <summary>
    /// Gets a component or adds it if not present.
    /// </summary>
    public T GetOrAddComponent<T>() where T : class, IComponent, new()
    {
        return GetComponent<T>() ?? AddComponent<T>();
    }

    /// <summary>
    /// Checks if entity has a component type.
    /// </summary>
    public bool HasComponent<T>() where T : class, IComponent
    {
        return _components.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Checks if entity has a component type.
    /// </summary>
    public bool HasComponent(Type componentType)
    {
        return _components.ContainsKey(componentType);
    }

    /// <summary>
    /// Tries to get a component.
    /// </summary>
    public bool TryGetComponent<T>(out T? component) where T : class, IComponent
    {
        if (_components.TryGetValue(typeof(T), out var c))
        {
            component = c as T;
            return component != null;
        }

        component = null;
        return false;
    }

    /// <summary>
    /// Removes a component.
    /// </summary>
    public bool RemoveComponent<T>() where T : class, IComponent
    {
        if (!_components.TryGetValue(typeof(T), out var component))
            return false;

        component.OnRemoved();
        component.Entity = null;
        _components.Remove(typeof(T));
        _updateableComponents.Remove(component);

        return true;
    }

    /// <summary>
    /// Gets all components.
    /// </summary>
    public IReadOnlyCollection<IComponent> GetAllComponents() => _components.Values;

    /// <summary>
    /// Gets all components matching a predicate.
    /// </summary>
    public IEnumerable<IComponent> GetComponents(Func<IComponent, bool> predicate)
        => _components.Values.Where(predicate);

    // ========================================
    // STATE MACHINE HELPERS
    // ========================================

    /// <summary>
    /// Initializes the state machine with given states.
    /// </summary>
    public void InitializeStateMachine(params IEntityState[] states)
    {
        State = new StateMachine(states, this);
    }

    /// <summary>
    /// Initializes with common combatant states.
    /// </summary>
    public void InitializeCombatantStates()
    {
        State = new StateMachine(CommonStates.Combatant.AllStates, this);
    }

    /// <summary>
    /// Initializes with AI states.
    /// </summary>
    public void InitializeAIStates()
    {
        State = new StateMachine(CommonStates.AI.AllStates, this);
    }

    /// <summary>
    /// Quick state check.
    /// </summary>
    public bool IsInState(int stateId) => State?.CurrentStateId == stateId;

    /// <summary>
    /// Quick state transition.
    /// </summary>
    public bool TryChangeState(int targetStateId) => State?.TransitionTo(targetStateId) ?? false;

    // ========================================
    // UPDATE
    // ========================================

    /// <summary>
    /// Updates entity (call every frame).
    /// </summary>
    public void Update(float deltaTime)
    {
        if (_disposed) return;

        // Update state machine
        State?.Update(deltaTime);

        // Update components
        foreach (var component in _updateableComponents)
        {
            if (component.IsEnabled)
            {
                component.OnUpdate(deltaTime);
            }
        }
    }

    // ========================================
    // CONVENIENCE METHODS
    // ========================================

    /// <summary>
    /// Quick damage application.
    /// </summary>
    public void TakeDamage(float damage, Entity? attacker = null)
    {
        if (attacker != null)
            Health.TakeDamage(damage, attacker);
        else
            Health.TakeDamage(damage);
    }

    /// <summary>
    /// Quick heal.
    /// </summary>
    public void Heal(float amount, Entity? healer = null)
    {
        Health.Heal(amount, healer);
    }

    /// <summary>
    /// Checks if entity is alive.
    /// </summary>
    public bool IsAlive => Health.IsAlive;

    /// <summary>
    /// Checks if entity is dead.
    /// </summary>
    public bool IsDead => !Health.IsAlive;

    /// <summary>
    /// Gets current health percentage.
    /// </summary>
    public float HealthPercent => Health.HealthPercent;

    // ========================================
    // DISPOSAL
    // ========================================

    /// <summary>
    /// Destroys this entity and publishes EntityDestroyingEvent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Publish destruction event first
        Events.Publish(new EntityDestroyingEvent(this, this));

        // Remove all components
        foreach (var component in _components.Values.ToList())
        {
            component.OnRemoved();
            component.Entity = null;
        }

        _components.Clear();
        _updateableComponents.Clear();
        Tags.ClearAll();
        UserData.Clear();
    }

    /// <summary>
    /// Destroys this entity (alias for Dispose).
    /// </summary>
    public void Destroy() => Dispose();

    public override string ToString() => $"Entity[{Id.ToString()[..8]}] Team:{TeamId} HP:{Health.CurrentHealth:F0}/{Health.MaxHealth:F0}";
}

// ========================================
// ENTITY EXTENSIONS
// ========================================

/// <summary>
/// Extension methods for entities.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Registers entity with global debug overlay.
    /// </summary>
    public static Entity RegisterForDebug(this Entity entity, string? displayName = null)
    {
        Debugger.DebugOverlay.Instance.RegisterEntity(entity, displayName);
        return entity;
    }

    /// <summary>
    /// Creates a snapshot of entity state.
    /// </summary>
    public static Debugger.EntitySnapshot CreateSnapshot(this Entity entity)
    {
        return Debugger.EntityDebug.Snapshot(entity);
    }

    /// <summary>
    /// Queries for entities with specific component.
    /// </summary>
    public static IEnumerable<Entity> WithComponent<T>(this IEnumerable<Entity> entities)
        where T : class, IComponent
        => entities.Where(e => e.HasComponent<T>());

    /// <summary>
    /// Queries for entities matching predicate.
    /// </summary>
    public static IEnumerable<Entity> Where(this IEnumerable<Entity> entities, Func<Entity, bool> predicate)
        => entities.Where(predicate);
}
