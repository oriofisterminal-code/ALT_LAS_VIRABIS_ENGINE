using Virabis.Core.Events;

namespace Virabis.Core;

/// <summary>
/// Core damage calculation pipeline.
/// Pipeline order: PreDamage → Team check → Base × HitCount → Stealth → Crit → Apply → DamageTaken
/// </summary>
/// <remarks>
/// v4.0 Improvements:
/// - Integrated with Event Bus for decoupled communication
/// - PreDamageEvent allows damage modification
/// - AttackHitEvent for hit feedback
/// - Full visibility into damage pipeline
/// </remarks>
public class DamageSystem
{
    private readonly IRandomProvider _random;
    private readonly IDamageConfiguration _config;
    private readonly IEventBus? _eventBus;

    /// <summary>
    /// Creates a DamageSystem with default configuration.
    /// </summary>
    public DamageSystem(IRandomProvider random)
        : this(random, DamageConfiguration.Default, null)
    {
    }

    /// <summary>
    /// Creates a DamageSystem with custom configuration.
    /// </summary>
    public DamageSystem(IRandomProvider random, IDamageConfiguration config)
        : this(random, config, null)
    {
    }

    /// <summary>
    /// Creates a DamageSystem with event bus.
    /// </summary>
    public DamageSystem(IRandomProvider random, IDamageConfiguration config, IEventBus? eventBus)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _eventBus = eventBus;
    }

    /// <summary>
    /// Processes a hit and returns the final damage dealt.
    /// </summary>
    public float ProcessHit(HitContext context)
    {
        ValidateContext(context);

        var attacker = context.Attacker!;
        var target = context.Target!;

        // Step 1: Publish PreDamage event (allows modification/cancellation)
        var preDamage = new PreDamageEvent(attacker, target, context.BaseDamage, this);
        Publish(preDamage);

        if (preDamage.IsCancelled)
            return 0f;

        // Use potentially modified damage
        var baseDamage = preDamage.FinalDamage;

        // Step 2: Friendly fire check (configurable)
        if (!_config.FriendlyFireEnabled && attacker.TeamId == target.TeamId)
            return 0f;

        // Step 3: Base damage with multi-hit
        float damage = baseDamage * context.HitCount;

        // Step 4: Stealth multiplier (only if target is unaware)
        var isStealth = context.IsStealthAttack && !target.Tags.HasTag("aware");
        if (isStealth)
            damage *= _config.StealthMultiplier;

        // Step 5: Crit roll (once per context)
        var critChance = Math.Min(attacker.Stats.CritChance, _config.CritChanceCap);
        var isCritical = _random.NextDouble() < critChance;
        if (isCritical)
            damage *= attacker.Stats.CritMultiplier;

        // Step 6: Apply damage caps
        damage = Math.Clamp(damage, _config.MinDamage, _config.MaxDamageCap);

        // Step 7: Publish AttackHit event (before applying)
        Publish(new AttackHitEvent(attacker, target, damage, isCritical, isStealth, this));

        // Step 8: Apply damage to target
        var previousHealth = target.Health.CurrentHealth;
        target.Health.ApplyDamage(damage);

        return damage;
    }

    /// <summary>
    /// Calculates potential damage without applying it.
    /// </summary>
    public float CalculateExpectedDamage(HitContext context)
    {
        ValidateContext(context);

        var attacker = context.Attacker!;
        var target = context.Target!;

        if (!_config.FriendlyFireEnabled && attacker.TeamId == target.TeamId)
            return 0f;

        float damage = context.BaseDamage * context.HitCount;

        if (context.IsStealthAttack && !target.Tags.HasTag("aware"))
            damage *= _config.StealthMultiplier;

        // Expected crit contribution (average)
        var critChance = Math.Min(attacker.Stats.CritChance, _config.CritChanceCap);
        var expectedCritMultiplier = 1.0f + (critChance * (attacker.Stats.CritMultiplier - 1.0f));
        damage *= expectedCritMultiplier;

        return Math.Clamp(damage, _config.MinDamage, _config.MaxDamageCap);
    }

    private void Publish<TEvent>(TEvent e) where TEvent : IEvent
    {
        _eventBus?.Publish(e);
        Events.Publish(e); // Also publish to global bus
    }

    private static void ValidateContext(HitContext context)
    {
        if (context == default)
            throw new ArgumentNullException(nameof(context), "HitContext cannot be default");

        if (context.Attacker == null)
            throw new ArgumentException("Context must have a valid attacker", nameof(context));

        if (context.Target == null)
            throw new ArgumentException("Context must have a valid target", nameof(context));

        if (context.BaseDamage < 0)
            throw new ArgumentException($"Base damage cannot be negative: {context.BaseDamage}", nameof(context));

        if (context.HitCount < 1)
            throw new ArgumentException($"Hit count must be at least 1: {context.HitCount}", nameof(context));
    }
}
