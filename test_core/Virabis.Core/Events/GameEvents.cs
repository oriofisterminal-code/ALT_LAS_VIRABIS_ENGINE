namespace Virabis.Core.Events;

/// <summary>
/// Game-specific events for combat system.
/// "Savaş alanında her an önemli" - Can Hızlı (Taksi Şoförü)
/// </summary>

// ========================================
// ENTITY EVENTS
// ========================================

/// <summary>
/// Raised when an entity is created.
/// </summary>
public sealed class EntityCreatedEvent : BaseEvent
{
    public Entity Entity { get; }

    public EntityCreatedEvent(Entity entity, object? source = null)
        : base(source)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }
}

/// <summary>
/// Raised when an entity is about to be destroyed.
/// </summary>
public sealed class EntityDestroyingEvent : BaseEvent
{
    public Entity Entity { get; }

    public EntityDestroyingEvent(Entity entity, object? source = null)
        : base(source)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }
}

// ========================================
// HEALTH EVENTS
// ========================================

/// <summary>
/// Raised when an entity takes damage.
/// </summary>
public sealed class DamageTakenEvent : BaseEvent
{
    public Entity Target { get; }
    public Entity? Attacker { get; }
    public float Damage { get; }
    public float PreviousHealth { get; }
    public float CurrentHealth { get; }
    public bool IsCritical { get; }
    public bool IsStealth { get; }

    public DamageTakenEvent(
        Entity target,
        Entity? attacker,
        float damage,
        float previousHealth,
        float currentHealth,
        bool isCritical = false,
        bool isStealth = false,
        object? source = null)
        : base(source)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Attacker = attacker;
        Damage = damage;
        PreviousHealth = previousHealth;
        CurrentHealth = currentHealth;
        IsCritical = isCritical;
        IsStealth = isStealth;
    }

    public float HealthPercent => Target.Health.MaxHealth > 0
        ? CurrentHealth / Target.Health.MaxHealth
        : 0f;
}

/// <summary>
/// Raised when an entity is healed.
/// </summary>
public sealed class HealedEvent : BaseEvent
{
    public Entity Target { get; }
    public Entity? Healer { get; }
    public float Amount { get; }
    public float PreviousHealth { get; }
    public float CurrentHealth { get; }

    public HealedEvent(
        Entity target,
        Entity? healer,
        float amount,
        float previousHealth,
        float currentHealth,
        object? source = null)
        : base(source)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Healer = healer;
        Amount = amount;
        PreviousHealth = previousHealth;
        CurrentHealth = currentHealth;
    }
}

/// <summary>
/// Raised when an entity dies.
/// </summary>
public sealed class DeathEvent : BaseEvent
{
    public Entity Entity { get; }
    public Entity? Killer { get; }
    public float FinalDamage { get; }
    public bool WasCritical { get; }

    public DeathEvent(
        Entity entity,
        Entity? killer,
        float finalDamage,
        bool wasCritical = false,
        object? source = null)
        : base(source)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        Killer = killer;
        FinalDamage = finalDamage;
        WasCritical = wasCritical;
    }
}

/// <summary>
/// Raised when an entity revives.
/// </summary>
public sealed class RevivedEvent : BaseEvent
{
    public Entity Entity { get; }
    public float HealthPercent { get; }

    public RevivedEvent(Entity entity, float healthPercent = 1f, object? source = null)
        : base(source)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        HealthPercent = healthPercent;
    }
}

// ========================================
// STATE EVENTS
// ========================================

/// <summary>
/// Raised when an entity's state changes.
/// </summary>
public sealed class StateChangedEvent : BaseEvent
{
    public Entity Entity { get; }
    public string PreviousState { get; }
    public string NewState { get; }
    public int PreviousStateId { get; }
    public int NewStateId { get; }
    public float TimeInPreviousState { get; }

    public StateChangedEvent(
        Entity entity,
        string previousState,
        string newState,
        int previousStateId,
        int newStateId,
        float timeInPreviousState,
        object? source = null)
        : base(source)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        PreviousState = previousState;
        NewState = newState;
        PreviousStateId = previousStateId;
        NewStateId = newStateId;
        TimeInPreviousState = timeInPreviousState;
    }
}

// ========================================
// TAG EVENTS
// ========================================

/// <summary>
/// Raised when a tag is added to an entity.
/// </summary>
public sealed class TagAddedEvent : BaseEvent
{
    public Entity Entity { get; }
    public string Tag { get; }

    public TagAddedEvent(Entity entity, string tag, object? source = null)
        : base(source)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        Tag = tag ?? throw new ArgumentNullException(nameof(tag));
    }
}

/// <summary>
/// Raised when a tag is removed from an entity.
/// </summary>
public sealed class TagRemovedEvent : BaseEvent
{
    public Entity Entity { get; }
    public string Tag { get; }

    public TagRemovedEvent(Entity entity, string tag, object? source = null)
        : base(source)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        Tag = tag ?? throw new ArgumentNullException(nameof(tag));
    }
}

// ========================================
// COMBAT EVENTS
// ========================================

/// <summary>
/// Raised before damage is calculated (can be modified).
/// </summary>
public sealed class PreDamageEvent : BaseEvent
{
    public Entity Attacker { get; }
    public Entity Target { get; }
    public float BaseDamage { get; set; }
    public float DamageMultiplier { get; set; } = 1f;
    public bool IsCancelled { get; private set; }

    public PreDamageEvent(
        Entity attacker,
        Entity target,
        float baseDamage,
        object? source = null)
        : base(source)
    {
        Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        BaseDamage = baseDamage;
    }

    /// <summary>
    /// Cancels the damage event.
    /// </summary>
    public void Cancel()
    {
        IsCancelled = true;
    }

    /// <summary>
    /// Gets the final damage after multipliers.
    /// </summary>
    public float FinalDamage => BaseDamage * DamageMultiplier;
}

/// <summary>
/// Raised when an attack hits (before damage calculation).
/// </summary>
public sealed class AttackHitEvent : BaseEvent
{
    public Entity Attacker { get; }
    public Entity Target { get; }
    public float RawDamage { get; }
    public bool IsCritical { get; set; }
    public bool IsStealth { get; }

    public AttackHitEvent(
        Entity attacker,
        Entity target,
        float rawDamage,
        bool isCritical = false,
        bool isStealth = false,
        object? source = null)
        : base(source)
    {
        Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        RawDamage = rawDamage;
        IsCritical = isCritical;
        IsStealth = isStealth;
    }
}

// ========================================
// STAT EVENTS
// ========================================

/// <summary>
/// Raised when an entity's stats change.
/// </summary>
public sealed class StatChangedEvent : BaseEvent
{
    public Entity Entity { get; }
    public string StatName { get; }
    public float OldValue { get; }
    public float NewValue { get; }

    public StatChangedEvent(
        Entity entity,
        string statName,
        float oldValue,
        float newValue,
        object? source = null)
        : base(source)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        StatName = statName ?? throw new ArgumentNullException(nameof(statName));
        OldValue = oldValue;
        NewValue = newValue;
    }
}
