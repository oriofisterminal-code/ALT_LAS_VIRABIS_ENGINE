namespace Virabis.Core.Weapons;

/// <summary>
/// Configuration for weapon behavior
/// </summary>
public class WeaponConfig
{
    public float Damage { get; set; }
    public float FireRate { get; set; }  // shots per second
    public float Range { get; set; }
    public int HitCount { get; set; }    // max targets per shot
}

/// <summary>
/// Tracks weapon fire state and cooldown
/// </summary>
public class WeaponState
{
    private float _lastShotTime;
    private float _fireRate;

    public WeaponState(float fireRate)
    {
        _fireRate = fireRate;
        _lastShotTime = float.MinValue;  // Allow immediate first shot
    }

    public bool CanFire(float currentTime)
    {
        float cooldown = 1f / _fireRate;
        return currentTime - _lastShotTime >= cooldown;
    }

    public void RecordShot(float currentTime)
    {
        _lastShotTime = currentTime;
    }
}
