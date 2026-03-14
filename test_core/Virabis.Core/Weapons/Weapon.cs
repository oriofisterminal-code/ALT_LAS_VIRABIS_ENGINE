using System.Collections.Generic;
using System.Numerics;

namespace Virabis.Core.Weapons;

/// <summary>
/// Core weapon logic - handles fire rate and damage application
/// Uses existing DamageSystem and HitContext
/// </summary>
public class Weapon
{
    private readonly WeaponConfig _config;
    private readonly WeaponState _state;
    private float _timeSinceLastShot;
    private readonly DamageSystem _damageSystem;

    public Weapon(WeaponConfig config, DamageSystem damageSystem)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _damageSystem = damageSystem ?? throw new ArgumentNullException(nameof(damageSystem));
        _state = new WeaponState(config.FireRate);
        _timeSinceLastShot = 0f;
    }

    /// <summary>
    /// Fire weapon and apply damage to targets
    /// Returns total damage applied (0 if cooldown not ready)
    /// </summary>
    public float Fire(float deltaTime, Entity attacker, Vector3 from, Vector3 to, List<Entity> targets)
    {
        _timeSinceLastShot += deltaTime;

        // Check if weapon can fire
        if (!_state.CanFire(_timeSinceLastShot))
            return 0f;

        // Apply damage to each target using existing DamageSystem
        float totalDamage = 0f;
        int hitCount = 0;
        foreach (var target in targets)
        {
            if (hitCount >= _config.HitCount)
                break;

            var hitContext = new HitContext(
                attacker: attacker,
                target: target,
                baseDamage: _config.Damage,
                hitCount: 1,
                isStealthAttack: false
            );

            float damageApplied = _damageSystem.ProcessHit(hitContext);
            totalDamage += damageApplied;
            hitCount++;
        }

        // Record shot for cooldown tracking
        _state.RecordShot(_timeSinceLastShot);
        return totalDamage;
    }
}
