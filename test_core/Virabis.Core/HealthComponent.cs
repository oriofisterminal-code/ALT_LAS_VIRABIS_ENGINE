using Virabis.Core.Events;

namespace Virabis.Core;

/// <summary>
/// Manages entity health state.
/// v3.0: Integrated with Event Bus for decoupled communication.
/// </summary>
public class HealthComponent
{
    private float _currentHealth;
    private readonly object? _owner;

    public float MaxHealth { get; }
    public float CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;
    public float HealthPercent => MaxHealth > 0 ? _currentHealth / MaxHealth : 0f;

    /// <summary>
    /// Event fired when health changes (legacy, use EventBus instead).
    /// </summary>
    public event Action<float>? HealthChanged;

    /// <summary>
    /// Event fired when entity dies (legacy, use EventBus instead).
    /// </summary>
    public event Action? Died;

    /// <summary>
    /// Creates a new HealthComponent.
    /// </summary>
    public HealthComponent(float maxHealth, object? owner = null)
    {
        if (maxHealth <= 0)
            throw new ArgumentException("MaxHealth must be greater than zero.", nameof(maxHealth));

        MaxHealth = maxHealth;
        _currentHealth = maxHealth;
        _owner = owner;
    }

    /// <summary>
    /// Applies damage and publishes DamageTakenEvent.
    /// </summary>
    public void ApplyDamage(float damage)
    {
        if (damage <= 0 || !IsAlive) return;

        var previousHealth = _currentHealth;
        _currentHealth = Math.Max(0, _currentHealth - damage);

        // Publish event
        if (_owner is Entity entity)
        {
            Events.Publish(new DamageTakenEvent(
                target: entity,
                attacker: null,
                damage: damage,
                previousHealth: previousHealth,
                currentHealth: _currentHealth,
                source: this
            ));
        }

        HealthChanged?.Invoke(_currentHealth - previousHealth);

        if (_currentHealth <= 0 && previousHealth > 0)
        {
            OnDeath(damage);
        }
    }

    /// <summary>
    /// Applies damage with attacker information.
    /// </summary>
    public void ApplyDamage(float damage, Entity attacker, bool isCritical = false, bool isStealth = false)
    {
        if (damage <= 0 || !IsAlive) return;

        var previousHealth = _currentHealth;
        _currentHealth = Math.Max(0, _currentHealth - damage);

        // Publish event
        if (_owner is Entity entity)
        {
            Events.Publish(new DamageTakenEvent(
                target: entity,
                attacker: attacker,
                damage: damage,
                previousHealth: previousHealth,
                currentHealth: _currentHealth,
                isCritical: isCritical,
                isStealth: isStealth,
                source: this
            ));
        }

        HealthChanged?.Invoke(_currentHealth - previousHealth);

        if (_currentHealth <= 0 && previousHealth > 0)
        {
            OnDeath(damage, attacker, isCritical);
        }
    }

    /// <summary>
    /// Heals and publishes HealedEvent.
    /// </summary>
    public void Heal(float amount)
    {
        if (amount <= 0 || _currentHealth >= MaxHealth) return;

        var previousHealth = _currentHealth;
        _currentHealth = Math.Min(MaxHealth, _currentHealth + amount);

        if (_owner is Entity entity)
        {
            Events.Publish(new HealedEvent(
                target: entity,
                healer: null,
                amount: amount,
                previousHealth: previousHealth,
                currentHealth: _currentHealth,
                source: this
            ));
        }

        HealthChanged?.Invoke(_currentHealth - previousHealth);
    }

    /// <summary>
    /// Heals with healer information.
    /// </summary>
    public void Heal(float amount, Entity healer)
    {
        if (amount <= 0 || _currentHealth >= MaxHealth) return;

        var previousHealth = _currentHealth;
        _currentHealth = Math.Min(MaxHealth, _currentHealth + amount);

        if (_owner is Entity entity)
        {
            Events.Publish(new HealedEvent(
                target: entity,
                healer: healer,
                amount: amount,
                previousHealth: previousHealth,
                currentHealth: _currentHealth,
                source: this
            ));
        }

        HealthChanged?.Invoke(_currentHealth - previousHealth);
    }

    /// <summary>
    /// Sets health to a specific value.
    /// </summary>
    public void SetHealth(float value)
    {
        var previousHealth = _currentHealth;
        _currentHealth = Math.Clamp(value, 0, MaxHealth);

        if (previousHealth != _currentHealth)
        {
            HealthChanged?.Invoke(_currentHealth - previousHealth);

            if (_currentHealth <= 0 && previousHealth > 0)
            {
                OnDeath(previousHealth - _currentHealth);
            }
        }
    }

    /// <summary>
    /// Revives the entity.
    /// </summary>
    public void Revive(float healthPercent = 1f)
    {
        if (IsAlive) return;

        var wasDead = _currentHealth <= 0;
        _currentHealth = MaxHealth * Math.Clamp(healthPercent, 0.01f, 1f);

        if (wasDead && _owner is Entity entity)
        {
            Events.Publish(new RevivedEvent(entity, healthPercent, this));
        }
    }

    private void OnDeath(float finalDamage, Entity? killer = null, bool wasCritical = false)
    {
        if (_owner is Entity entity)
        {
            Events.Publish(new DeathEvent(
                entity: entity,
                killer: killer,
                finalDamage: finalDamage,
                wasCritical: wasCritical,
                source: this
            ));
        }

        Died?.Invoke();
    }
}
