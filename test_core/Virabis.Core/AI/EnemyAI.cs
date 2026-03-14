using System.Numerics;

namespace Virabis.Core.AI;

/// <summary>
/// AI state for enemy behavior
/// </summary>
public enum AIState
{
    Idle,
    Chase,
    Attack,
    Dead
}

/// <summary>
/// Configuration for enemy AI behavior
/// </summary>
public class EnemyAIConfig
{
    public float DetectionRange { get; set; }
    public float AttackRange { get; set; }
    public float AttackCooldown { get; set; }
}

/// <summary>
/// Command output from AI Update
/// </summary>
public struct AICommand
{
    public Vector3 MoveDirection;
    public bool ShouldAttack;
}

/// <summary>
/// Pure C# enemy AI with state machine logic (zero Godot dependencies)
/// </summary>
public class EnemyAI
{
    private readonly EnemyAIConfig _config;
    private AIState _currentState;
    private float _timeSinceLastAttack;

    public AIState CurrentState => _currentState;
    public float TimeSinceLastAttack => _timeSinceLastAttack;

    public EnemyAI(EnemyAIConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _currentState = AIState.Idle;
        _timeSinceLastAttack = 0f;
    }

    public void SetDead()
    {
        _currentState = AIState.Dead;
    }

    public AICommand Update(float deltaTime, Vector3 enemyPosition, Vector3 playerPosition)
    {
        // Handle invalid positions - treat as maximum distance (return to Idle)
        if (!float.IsFinite(enemyPosition.X) || !float.IsFinite(enemyPosition.Y) || !float.IsFinite(enemyPosition.Z) ||
            !float.IsFinite(playerPosition.X) || !float.IsFinite(playerPosition.Y) || !float.IsFinite(playerPosition.Z))
        {
            _currentState = AIState.Idle;
            return new AICommand
            {
                MoveDirection = Vector3.Zero,
                ShouldAttack = false
            };
        }

        // Dead state is terminal
        if (_currentState == AIState.Dead)
        {
            return new AICommand
            {
                MoveDirection = Vector3.Zero,
                ShouldAttack = false
            };
        }

        // Handle negative deltaTime
        deltaTime = Math.Max(0f, deltaTime);

        // Update cooldown timer
        _timeSinceLastAttack += deltaTime;

        // Calculate distance
        float distance = Vector3.Distance(enemyPosition, playerPosition);

        // State transitions
        switch (_currentState)
        {
            case AIState.Idle:
                if (distance <= _config.DetectionRange)
                {
                    _currentState = AIState.Chase;
                }
                break;

            case AIState.Chase:
                if (distance <= _config.AttackRange)
                {
                    _currentState = AIState.Attack;
                }
                else if (distance > _config.DetectionRange)
                {
                    _currentState = AIState.Idle;
                }
                break;

            case AIState.Attack:
                if (distance > _config.AttackRange)
                {
                    _currentState = AIState.Chase;
                }
                break;
        }

        // Calculate move direction
        Vector3 moveDirection = Vector3.Zero;
        if (_currentState == AIState.Chase || _currentState == AIState.Attack)
        {
            Vector3 direction = playerPosition - enemyPosition;
            if (direction.LengthSquared() > 0)
            {
                moveDirection = Vector3.Normalize(direction);
            }
        }

        // Determine if should attack
        bool shouldAttack = false;
        if (_currentState == AIState.Attack && _timeSinceLastAttack >= _config.AttackCooldown)
        {
            shouldAttack = true;
            _timeSinceLastAttack = 0f;
        }

        return new AICommand
        {
            MoveDirection = moveDirection,
            ShouldAttack = shouldAttack
        };
    }
}
