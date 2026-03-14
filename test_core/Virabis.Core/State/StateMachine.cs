using Virabis.Core.Events;

namespace Virabis.Core.State;

/// <summary>
/// Generic state machine for managing entity states.
/// "Sabırsızım, basit olsun" - Can Hızlı (Taksi Şoförü)
///
/// v2.0: Integrated with Event Bus for decoupled state change notifications.
/// </summary>
public class StateMachine
{
    private readonly Dictionary<int, IEntityState> _states = new();
    private IEntityState _currentState;
    private IEntityState? _previousState;
    private float _timeInCurrentState;
    private readonly Entity? _owner;

    /// <summary>
    /// Current state ID.
    /// </summary>
    public int CurrentStateId => _currentState.Id;

    /// <summary>
    /// Current state name.
    /// </summary>
    public string CurrentStateName => _currentState.Name;

    /// <summary>
    /// Current state instance.
    /// </summary>
    public IEntityState CurrentState => _currentState;

    /// <summary>
    /// Previous state (null if none).
    /// </summary>
    public IEntityState? PreviousState => _previousState;

    /// <summary>
    /// Time spent in current state (seconds).
    /// </summary>
    public float TimeInCurrentState => _timeInCurrentState;

    /// <summary>
    /// Entity that owns this state machine (for events).
    /// </summary>
    public Entity? Owner => _owner;

    /// <summary>
    /// Event fired when state changes.
    /// </summary>
    [Obsolete("Use EventBus.Subscribe<StateChangedEvent> instead")]
    public event Action<StateChangeEvent>? OnStateChange;

    /// <summary>
    /// Creates a state machine with an initial state.
    /// </summary>
    public StateMachine(IEntityState initialState, Entity? owner = null)
    {
        _currentState = initialState;
        _states[initialState.Id] = initialState;
        _owner = owner;
        _currentState.OnEnter();
    }

    /// <summary>
    /// Creates a state machine with multiple states.
    /// First state becomes the initial state.
    /// </summary>
    public StateMachine(params IEntityState[] states)
        : this(states, null)
    {
    }

    /// <summary>
    /// Creates a state machine with multiple states and owner.
    /// </summary>
    public StateMachine(IEntityState[] states, Entity? owner)
    {
        if (states == null || states.Length == 0)
            throw new ArgumentException("At least one state required");

        _owner = owner;

        foreach (var state in states)
            _states[state.Id] = state;

        _currentState = states[0];
        _currentState.OnEnter();
    }

    /// <summary>
    /// Adds a state to the machine.
    /// </summary>
    public void AddState(IEntityState state) => _states[state.Id] = state;

    /// <summary>
    /// Gets a state by ID.
    /// </summary>
    public IEntityState? GetState(int stateId) => _states.TryGetValue(stateId, out var state) ? state : null;

    /// <summary>
    /// Checks if transition to target state is allowed.
    /// </summary>
    public bool CanTransitionTo(int targetStateId)
    {
        if (!_states.ContainsKey(targetStateId))
            return false;

        return _currentState.AllowedTransitions.Contains(targetStateId);
    }

    /// <summary>
    /// Attempts to transition to a new state.
    /// Returns true if successful.
    /// </summary>
    public bool TransitionTo(int targetStateId)
    {
        if (!CanTransitionTo(targetStateId))
            return false;

        var newState = _states[targetStateId];
        PerformTransition(newState);
        return true;
    }

    /// <summary>
    /// Forces a transition without checking allowed transitions.
    /// Use for special cases (death, stun, etc.)
    /// </summary>
    public void ForceTransition(int targetStateId)
    {
        if (!_states.TryGetValue(targetStateId, out var newState))
            throw new InvalidOperationException($"State {targetStateId} not found");

        PerformTransition(newState);
    }

    /// <summary>
    /// Transitions back to the previous state.
    /// </summary>
    public bool ReturnToPrevious()
    {
        if (_previousState == null)
            return false;

        return TransitionTo(_previousState.Id);
    }

    /// <summary>
    /// Updates the state machine (call every frame).
    /// </summary>
    public void Update(float deltaTime)
    {
        _timeInCurrentState += deltaTime;
        _currentState.OnUpdate(deltaTime);
    }

    /// <summary>
    /// Resets to the initial state.
    /// </summary>
    public void Reset(IEntityState? initialState = null)
    {
        _currentState.OnExit();
        _previousState = null;
        _timeInCurrentState = 0f;

        if (initialState != null)
        {
            _currentState = initialState;
            _states[initialState.Id] = initialState;
        }

        _currentState.OnEnter();
    }

    private void PerformTransition(IEntityState newState)
    {
        var previousState = _currentState;

        _currentState.OnExit();
        _previousState = _currentState;
        _currentState = newState;
        _timeInCurrentState = 0f;
        _currentState.OnEnter();

        // Publish event via Event Bus
        if (_owner != null)
        {
            Events.Publish(new StateChangedEvent(
                entity: _owner,
                previousState: previousState.Name,
                newState: newState.Name,
                previousStateId: previousState.Id,
                newStateId: newState.Id,
                timeInPreviousState: _timeInCurrentState,
                source: this
            ));
        }

        // Legacy event (backwards compatibility)
#pragma warning disable CS0618
        OnStateChange?.Invoke(new StateChangeEvent(
            previousState.Id, previousState.Name,
            newState.Id, newState.Name
        ));
#pragma warning restore CS0618
    }

    public override string ToString() => $"StateMachine[{CurrentStateName}]";
}

/// <summary>
/// Event data for state changes.
/// </summary>
public readonly record struct StateChangeEvent(
    int PreviousStateId,
    string PreviousStateName,
    int NewStateId,
    string NewStateName
);
