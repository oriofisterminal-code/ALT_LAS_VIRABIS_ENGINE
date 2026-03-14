namespace Virabis.Core.Components;

/// <summary>
/// Standard components for common entity functionality.
/// "Temel parçalar her zaman lazım" - Ahmet Mimari (Barmen)
/// </summary>

// ========================================
// HEALTH COMPONENT
// ========================================

/// <summary>
/// Component for entities that can take damage.
/// </summary>
public class HealthComponent : ComponentBase
{
    private float _currentHealth;
    private float _maxHealth;

    public float MaxHealth
    {
        get => _maxHealth;
        set
        {
            _maxHealth = value;
            _currentHealth = Math.Min(_currentHealth, value);
        }
    }

    public float CurrentHealth
    {
        get => _currentHealth;
        set => _currentHealth = Math.Clamp(value, 0, MaxHealth);
    }

    public bool IsAlive => _currentHealth > 0;
    public float HealthPercent => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
    public bool IsFull => CurrentHealth >= MaxHealth;
    public bool IsLow => HealthPercent <= 0.25f;
    public bool IsCritical => HealthPercent <= 0.1f;

    public HealthComponent() { }

    public HealthComponent(float maxHealth)
    {
        _maxHealth = maxHealth;
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive || damage <= 0) return;

        var previous = _currentHealth;
        _currentHealth = Math.Max(0, _currentHealth - damage);

        Events.Publish(new DamageTakenEvent(
            target: Entity!,
            attacker: null,
            damage: damage,
            previousHealth: previous,
            currentHealth: _currentHealth,
            source: this
        ));

        if (!IsAlive && previous > 0)
        {
            Events.Publish(new DeathEvent(Entity!, null, damage, false, this));
        }
    }

    public void TakeDamage(float damage, Entity attacker, bool critical = false)
    {
        if (!IsAlive || damage <= 0) return;

        var previous = _currentHealth;
        _currentHealth = Math.Max(0, _currentHealth - damage);

        Events.Publish(new DamageTakenEvent(
            target: Entity!,
            attacker: attacker,
            damage: damage,
            previousHealth: previous,
            currentHealth: _currentHealth,
            isCritical: critical,
            source: this
        ));

        if (!IsAlive && previous > 0)
        {
            Events.Publish(new DeathEvent(Entity!, attacker, damage, critical, this));
        }
    }

    public void Heal(float amount, Entity? healer = null)
    {
        if (!IsAlive || amount <= 0 || IsFull) return;

        var previous = _currentHealth;
        _currentHealth = Math.Min(MaxHealth, _currentHealth + amount);

        Events.Publish(new HealedEvent(
            target: Entity!,
            healer: healer,
            amount: amount,
            previousHealth: previous,
            currentHealth: _currentHealth,
            source: this
        ));
    }

    public void Kill(Entity? killer = null)
    {
        if (!IsAlive) return;

        var damage = _currentHealth;
        TakeDamage(damage, killer!);
    }

    public void Revive(float healthPercent = 1f)
    {
        if (IsAlive) return;

        _currentHealth = MaxHealth * Math.Clamp(healthPercent, 0.01f, 1f);
        Events.Publish(new RevivedEvent(Entity!, healthPercent, this));
    }

    public void Reset()
    {
        _currentHealth = MaxHealth;
    }
}

// ========================================
// STAT COMPONENT
// ========================================

/// <summary>
/// Component for entity stats (attack, defense, speed, etc.)
/// </summary>
public class StatComponent : ComponentBase
{
    // Combat stats
    public float AttackPower { get; set; } = 10f;
    public float Defense { get; set; } = 0f;
    public float CritChance { get; set; } = 0.05f;
    public float CritMultiplier { get; set; } = 2f;

    // Movement stats
    public float MoveSpeed { get; set; } = 5f;
    public float AttackRange { get; set; } = 1.5f;
    public float AttackSpeed { get; set; } = 1f;

    // Utility stats
    public float CooldownReduction { get; set; } = 0f;
    public float DamageMultiplier { get; set; } = 1f;
    public float HealMultiplier { get; set; } = 1f;

    /// <summary>
    /// Modifies a stat and publishes an event.
    /// </summary>
    public void ModifyStat(string statName, float newValue)
    {
        var property = GetType().GetProperty(statName);
        if (property == null || !property.CanWrite)
            throw new ArgumentException($"Unknown stat: {statName}");

        var oldValue = (float)property.GetValue(this)!;
        property.SetValue(this, newValue);

        if (oldValue != newValue && Entity != null)
        {
            Events.Publish(new StatChangedEvent(Entity, statName, oldValue, newValue, this));
        }
    }

    /// <summary>
    /// Gets a stat value by name.
    /// </summary>
    public float GetStat(string statName)
    {
        var property = GetType().GetProperty(statName);
        return property != null ? (float)property.GetValue(this)! : 0f;
    }

    /// <summary>
    /// Calculates damage after defense.
    /// </summary>
    public float CalculateDamageReduction(float incomingDamage)
    {
        // Simple formula: damage * 100 / (100 + defense)
        return incomingDamage * 100f / (100f + Defense);
    }
}

// ========================================
// TAG COMPONENT
// ========================================

/// <summary>
/// Component for entity tags and status effects.
/// </summary>
public class TagComponent : ComponentBase
{
    private readonly HashSet<string> _tags = new(StringComparer.OrdinalIgnoreCase);
    private readonly Tags.GameplayTagContainer _gameplayTags = new();

    public Tags.GameplayTagContainer GameplayTags => _gameplayTags;
    public IReadOnlySet<string> Tags => _tags;

    // ========================================
    // STRING TAGS
    // ========================================

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        if (_tags.Add(tag))
        {
            Events.Publish(new TagAddedEvent(Entity!, tag, this));
        }
    }

    public void RemoveTag(string tag)
    {
        if (_tags.Remove(tag))
        {
            Events.Publish(new TagRemovedEvent(Entity!, tag, this));
        }
    }

    public bool HasTag(string tag) => _tags.Contains(tag);

    public void ToggleTag(string tag, bool state)
    {
        if (state) AddTag(tag);
        else RemoveTag(tag);
    }

    // ========================================
    // GAMEPLAY TAGS
    // ========================================

    public void AddGameplayTag(Tags.GameplayTag tag)
    {
        _gameplayTags.AddTag(tag);
        Events.Publish(new TagAddedEvent(Entity!, tag.Name, this));
    }

    public void RemoveGameplayTag(Tags.GameplayTag tag)
    {
        _gameplayTags.RemoveTag(tag);
        Events.Publish(new TagRemovedEvent(Entity!, tag.Name, this));
    }

    public bool HasGameplayTag(Tags.GameplayTag tag) => _gameplayTags.HasTag(tag);

    public bool HasGameplayTagMatch(Tags.GameplayTag tag) => _gameplayTags.HasTagMatch(tag);

    // ========================================
    // CLEAR
    // ========================================

    public void ClearAll()
    {
        foreach (var tag in _tags.ToList())
            RemoveTag(tag);

        foreach (var tag in _gameplayTags.Tags.ToList())
            RemoveGameplayTag(tag);

        _tags.Clear();
        _gameplayTags.Clear();
    }
}

// ========================================
// STATE COMPONENT
// ========================================

/// <summary>
/// Component for entity state machine.
/// </summary>
[RequireComponent(typeof(TagComponent))]
public class StateComponent : ComponentBase
{
    private State.StateMachine? _stateMachine;
    private int _currentStateId;

    public State.StateMachine? StateMachine => _stateMachine;
    public int CurrentStateId => _stateMachine?.CurrentStateId ?? _currentStateId;
    public string CurrentStateName => _stateMachine?.CurrentStateName ?? "None";
    public float TimeInState => _stateMachine?.TimeInCurrentState ?? 0f;

    public void Initialize(params State.IEntityState[] states)
    {
        _stateMachine = new State.StateMachine(states, Entity!);
    }

    public void InitializeCombatant()
    {
        _stateMachine = new State.StateMachine(State.CommonStates.Combatant.AllStates, Entity!);
    }

    public void InitializeAI()
    {
        _stateMachine = new State.StateMachine(State.CommonStates.AI.AllStates, Entity!);
    }

    public bool TransitionTo(int stateId)
    {
        return _stateMachine?.TransitionTo(stateId) ?? false;
    }

    public bool IsInState(int stateId) => CurrentStateId == stateId;

    public override void OnUpdate(float deltaTime)
    {
        _stateMachine?.Update(deltaTime);
    }
}

// ========================================
// VELOCITY COMPONENT
// ========================================

/// <summary>
/// Component for entity movement.
/// </summary>
public class VelocityComponent : ComponentBase
{
    public System.Numerics.Vector3 Velocity { get; set; }
    public System.Numerics.Vector3 Position { get; set; }
    public float Speed { get; set; } = 5f;
    public bool IsMoving => Velocity.LengthSquared() > 0.001f;
    public float SpeedMultiplier { get; set; } = 1f;

    public void SetDirection(System.Numerics.Vector3 direction)
    {
        Velocity = direction.LengthSquared() > 0
            ? System.Numerics.Vector3.Normalize(direction) * Speed * SpeedMultiplier
            : System.Numerics.Vector3.Zero;
    }

    public void Stop()
    {
        Velocity = System.Numerics.Vector3.Zero;
    }

    public override void OnUpdate(float deltaTime)
    {
        if (IsMoving)
        {
            Position += Velocity * deltaTime;
        }
    }
}

// ========================================
// INVENTORY COMPONENT
// ========================================

/// <summary>
/// Component for entity inventory.
/// </summary>
public class InventoryComponent : ComponentBase
{
    private readonly List<InventoryItem> _items = new();
    private int _capacity = 20;

    public int Capacity
    {
        get => _capacity;
        set => _capacity = Math.Max(1, value);
    }

    public IReadOnlyList<InventoryItem> Items => _items;
    public int ItemCount => _items.Count;
    public bool IsFull => _items.Count >= _capacity;
    public bool IsEmpty => _items.Count == 0;

    public bool AddItem(InventoryItem item)
    {
        if (IsFull) return false;

        // Try to stack
        var existing = _items.FirstOrDefault(i => i.CanStackWith(item));
        if (existing != null)
        {
            existing.Quantity += item.Quantity;
            return true;
        }

        _items.Add(item);
        return true;
    }

    public bool RemoveItem(InventoryItem item)
    {
        return _items.Remove(item);
    }

    public bool RemoveItem(Guid itemId, int quantity = 1)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return false;

        if (item.Quantity <= quantity)
        {
            _items.Remove(item);
        }
        else
        {
            item.Quantity -= quantity;
        }

        return true;
    }

    public InventoryItem? FindItem(Func<InventoryItem, bool> predicate)
        => _items.FirstOrDefault(predicate);

    public IEnumerable<T> FindItemsOfType<T>() where T : InventoryItem
        => _items.OfType<T>();

    public void Clear() => _items.Clear();
}

/// <summary>
/// Base class for inventory items.
/// </summary>
public class InventoryItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public int Quantity { get; set; } = 1;
    public int MaxStack { get; init; } = 1;
    public bool IsStackable => MaxStack > 1;

    public bool CanStackWith(InventoryItem other)
        => IsStackable && other.IsStackable && Name == other.Name && Quantity + other.Quantity <= MaxStack;
}
