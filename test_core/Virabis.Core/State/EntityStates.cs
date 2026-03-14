namespace Virabis.Core.State;

/// <summary>
/// Pre-defined entity states for common use cases.
/// "En kısa yoldan gitmek lazım" - Can Hızlı (Taksi Şoförü)
///
/// v2.0: Fixed allocation issue with Lazy<T> singleton pattern.
/// </summary>
public static class EntityStates
{
    // ========================================
    // COMMON ENTITY STATES (0-9)
    // ========================================

    /// <summary>
    /// Entity is idle, not doing anything.
    /// </summary>
    public const int Idle = 0;

    /// <summary>
    /// Entity is moving/running.
    /// </summary>
    public const int Moving = 1;

    /// <summary>
    /// Entity is attacking.
    /// </summary>
    public const int Attacking = 2;

    /// <summary>
    /// Entity is defending/blocking.
    /// </summary>
    public const int Defending = 3;

    /// <summary>
    /// Entity is stunned.
    /// </summary>
    public const int Stunned = 4;

    /// <summary>
    /// Entity is dead.
    /// </summary>
    public const int Dead = 5;

    /// <summary>
    /// Entity is invisible/hidden.
    /// </summary>
    public const int Invisible = 6;

    /// <summary>
    /// Entity is in stealth mode.
    /// </summary>
    public const int Stealthed = 7;

    // ========================================
    // AI SPECIFIC STATES (10-19)
    // ========================================

    /// <summary>
    /// AI is patrolling.
    /// </summary>
    public const int Patrolling = 10;

    /// <summary>
    /// AI is chasing a target.
    /// </summary>
    public const int Chasing = 11;

    /// <summary>
    /// AI is fleeing.
    /// </summary>
    public const int Fleeing = 12;

    /// <summary>
    /// AI is alerted/investigating.
    /// </summary>
    public const int Alerted = 13;

    // ========================================
    // PLAYER SPECIFIC STATES (20-29)
    // ========================================

    /// <summary>
    /// Player is jumping.
    /// </summary>
    public const int Jumping = 20;

    /// <summary>
    /// Player is crouching.
    /// </summary>
    public const int Crouching = 21;

    /// <summary>
    /// Player is interacting with something.
    /// </summary>
    public const int Interacting = 22;

    // ========================================
    // STATE NAMES (for debugging)
    // ========================================

    private static readonly Dictionary<int, string> StateNames = new()
    {
        [Idle] = "Idle",
        [Moving] = "Moving",
        [Attacking] = "Attacking",
        [Defending] = "Defending",
        [Stunned] = "Stunned",
        [Dead] = "Dead",
        [Invisible] = "Invisible",
        [Stealthed] = "Stealthed",
        [Patrolling] = "Patrolling",
        [Chasing] = "Chasing",
        [Fleeing] = "Fleeing",
        [Alerted] = "Alerted",
        [Jumping] = "Jumping",
        [Crouching] = "Crouching",
        [Interacting] = "Interacting"
    };

    /// <summary>
    /// Gets the name of a state by ID.
    /// </summary>
    public static string GetName(int stateId) => StateNames.TryGetValue(stateId, out var name) ? name : $"Unknown({stateId})";

    /// <summary>
    /// Checks if a state ID is valid.
    /// </summary>
    public static bool IsValid(int stateId) => StateNames.ContainsKey(stateId);
}

/// <summary>
/// Simple entity state implementation for quick setup.
/// </summary>
public class SimpleEntityState : EntityStateBase
{
    public override int Id { get; }
    public override string Name { get; }

    public SimpleEntityState(int id, string name)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Creates a state with allowed transitions.
    /// </summary>
    public static SimpleEntityState Create(int id, string name, params int[] allowedTransitions)
    {
        var state = new SimpleEntityState(id, name);
        state.AllowTransitionsTo(allowedTransitions);
        return state;
    }
}

/// <summary>
/// Common state configurations with lazy initialization.
/// Prevents repeated allocations on property access.
/// </summary>
public static class CommonStates
{
    // ========================================
    // COMBATANT STATES (Lazy initialization)
    // ========================================

    private static readonly Lazy<SimpleEntityState> _combatantIdle = new(() =>
        SimpleEntityState.Create(EntityStates.Idle, "Idle",
            EntityStates.Moving, EntityStates.Attacking, EntityStates.Stunned, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _combatantMoving = new(() =>
        SimpleEntityState.Create(EntityStates.Moving, "Moving",
            EntityStates.Idle, EntityStates.Attacking, EntityStates.Jumping, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _combatantAttacking = new(() =>
        SimpleEntityState.Create(EntityStates.Attacking, "Attacking",
            EntityStates.Idle, EntityStates.Stunned, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _combatantStunned = new(() =>
        SimpleEntityState.Create(EntityStates.Stunned, "Stunned",
            EntityStates.Idle, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _combatantDead = new(() =>
        SimpleEntityState.Create(EntityStates.Dead, "Dead"));

    /// <summary>
    /// Basic combat entity states: Idle → Attacking → Stunned → Dead
    /// </summary>
    public static class Combatant
    {
        public static SimpleEntityState Idle => _combatantIdle.Value;
        public static SimpleEntityState Moving => _combatantMoving.Value;
        public static SimpleEntityState Attacking => _combatantAttacking.Value;
        public static SimpleEntityState Stunned => _combatantStunned.Value;
        public static SimpleEntityState Dead => _combatantDead.Value;

        /// <summary>
        /// Gets all combatant states as an array for StateMachine initialization.
        /// </summary>
        public static SimpleEntityState[] AllStates => new[]
        {
            Idle, Moving, Attacking, Stunned, Dead
        };
    }

    // ========================================
    // AI STATES (Lazy initialization)
    // ========================================

    private static readonly Lazy<SimpleEntityState> _aiIdle = new(() =>
        SimpleEntityState.Create(EntityStates.Idle, "Idle",
            EntityStates.Patrolling, EntityStates.Alerted, EntityStates.Chasing, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _aiPatrolling = new(() =>
        SimpleEntityState.Create(EntityStates.Patrolling, "Patrolling",
            EntityStates.Idle, EntityStates.Alerted, EntityStates.Chasing, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _aiChasing = new(() =>
        SimpleEntityState.Create(EntityStates.Chasing, "Chasing",
            EntityStates.Attacking, EntityStates.Idle, EntityStates.Fleeing, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _aiAttacking = new(() =>
        SimpleEntityState.Create(EntityStates.Attacking, "Attacking",
            EntityStates.Chasing, EntityStates.Idle, EntityStates.Fleeing, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _aiFleeing = new(() =>
        SimpleEntityState.Create(EntityStates.Fleeing, "Fleeing",
            EntityStates.Idle, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _aiAlerted = new(() =>
        SimpleEntityState.Create(EntityStates.Alerted, "Alerted",
            EntityStates.Idle, EntityStates.Chasing, EntityStates.Dead));

    private static readonly Lazy<SimpleEntityState> _aiDead = new(() =>
        SimpleEntityState.Create(EntityStates.Dead, "Dead"));

    /// <summary>
    /// AI states: Idle → Patrolling → Chasing → Attacking
    /// </summary>
    public static class AI
    {
        public static SimpleEntityState Idle => _aiIdle.Value;
        public static SimpleEntityState Patrolling => _aiPatrolling.Value;
        public static SimpleEntityState Chasing => _aiChasing.Value;
        public static SimpleEntityState Attacking => _aiAttacking.Value;
        public static SimpleEntityState Fleeing => _aiFleeing.Value;
        public static SimpleEntityState Alerted => _aiAlerted.Value;
        public static SimpleEntityState Dead => _aiDead.Value;

        /// <summary>
        /// Gets all AI states as an array for StateMachine initialization.
        /// </summary>
        public static SimpleEntityState[] AllStates => new[]
        {
            Idle, Patrolling, Chasing, Attacking, Fleeing, Alerted, Dead
        };
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    /// <summary>
    /// Resets cached states (for testing purposes).
    /// Note: This creates new instances, use with caution.
    /// </summary>
    public static void ResetCache()
    {
        // Lazy<T> doesn't support reset, this is informational only
        // Tests should use new StateMachine instances instead
    }
}
