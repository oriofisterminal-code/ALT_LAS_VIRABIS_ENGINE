using Xunit;
using Virabis.Core.State;

namespace Virabis.Core.Tests;

/// <summary>
/// Tests for State Machine system.
/// "En kısa yoldan gitmek lazım" - Can Hızlı (Taksi Şoförü)
/// </summary>
public class StateMachineTests
{
    // ========================================
    // BASIC STATE MACHINE TESTS
    // ========================================

    [Fact, Trait("Category", "Smoke")]
    public void StateMachine_Create_WithInitialState()
    {
        var sm = new StateMachine(CommonStates.Combatant.Idle);
        Assert.Equal(EntityStates.Idle, sm.CurrentStateId);
        Assert.Equal("Idle", sm.CurrentStateName);
    }

    [Fact, Trait("Category", "Unit")]
    public void StateMachine_Create_WithMultipleStates()
    {
        var sm = new StateMachine(
            CommonStates.Combatant.Idle,
            CommonStates.Combatant.Attacking,
            CommonStates.Combatant.Dead
        );

        Assert.Equal(EntityStates.Idle, sm.CurrentStateId);
    }

    // ========================================
    // TRANSITION TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Transition_Allowed_ReturnsTrue()
    {
        var sm = new StateMachine(CommonStates.Combatant.Idle);
        Assert.True(sm.TransitionTo(EntityStates.Attacking));
        Assert.Equal(EntityStates.Attacking, sm.CurrentStateId);
    }

    [Fact, Trait("Category", "Unit")]
    public void Transition_NotAllowed_ReturnsFalse()
    {
        // Dead state can't transition to anything
        var sm = new StateMachine(CommonStates.Combatant.Dead);
        Assert.False(sm.TransitionTo(EntityStates.Idle));
        Assert.Equal(EntityStates.Dead, sm.CurrentStateId);
    }

    [Fact, Trait("Category", "Unit")]
    public void CanTransition_Check()
    {
        var sm = new StateMachine(CommonStates.Combatant.Idle);
        Assert.True(sm.CanTransitionTo(EntityStates.Attacking));
        Assert.False(sm.CanTransitionTo(999)); // Non-existent state
    }

    [Fact, Trait("Category", "Unit")]
    public void ForceTransition_SkipsChecks()
    {
        var sm = new StateMachine(CommonStates.Combatant.Dead);
        sm.ForceTransition(EntityStates.Idle);
        Assert.Equal(EntityStates.Idle, sm.CurrentStateId);
    }

    // ========================================
    // STATE HISTORY TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void PreviousState_Tracked()
    {
        var sm = new StateMachine(CommonStates.Combatant.Idle);
        sm.TransitionTo(EntityStates.Attacking);

        Assert.NotNull(sm.PreviousState);
        Assert.Equal(EntityStates.Idle, sm.PreviousState?.Id);
    }

    [Fact, Trait("Category", "Unit")]
    public void ReturnToPrevious_Success()
    {
        var sm = new StateMachine(CommonStates.Combatant.Idle);
        sm.TransitionTo(EntityStates.Attacking);
        var returned = sm.ReturnToPrevious();

        Assert.True(returned);
        Assert.Equal(EntityStates.Idle, sm.CurrentStateId);
    }

    // ========================================
    // TIME TRACKING TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void TimeInCurrentState_TracksCorrectly()
    {
        var sm = new StateMachine(CommonStates.Combatant.Idle);

        sm.Update(0.016f);
        sm.Update(0.016f);
        sm.Update(0.016f);

        Assert.Equal(0.048f, sm.TimeInCurrentState, precision: 3);
    }

    [Fact, Trait("Category", "Unit")]
    public void TimeInCurrentState_ResetsOnTransition()
    {
        var sm = new StateMachine(CommonStates.Combatant.Idle);
        sm.Update(1.0f);

        sm.TransitionTo(EntityStates.Attacking);

        Assert.Equal(0f, sm.TimeInCurrentState);
    }

    // ========================================
    // EVENT TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void OnStateChange_Fired()
    {
        var sm = new StateMachine(CommonStates.Combatant.Idle);
        StateChangeEvent? firedEvent = null;
        sm.OnStateChange += e => firedEvent = e;

        sm.TransitionTo(EntityStates.Attacking);

        Assert.NotNull(firedEvent);
        Assert.Equal(EntityStates.Idle, firedEvent.Value.PreviousStateId);
        Assert.Equal(EntityStates.Attacking, firedEvent.Value.NewStateId);
    }

    // ========================================
    // AI STATES TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void AIStates_IdleToPatrolling()
    {
        var sm = new StateMachine(
            CommonStates.AI.Idle,
            CommonStates.AI.Patrolling,
            CommonStates.AI.Chasing,
            CommonStates.AI.Attacking
        );

        Assert.True(sm.TransitionTo(EntityStates.Patrolling));
        Assert.Equal(EntityStates.Patrolling, sm.CurrentStateId);
    }

    [Fact, Trait("Category", "Unit")]
    public void AIStates_FullChain()
    {
        var sm = new StateMachine(
            CommonStates.AI.Idle,
            CommonStates.AI.Patrolling,
            CommonStates.AI.Chasing,
            CommonStates.AI.Attacking,
            CommonStates.AI.Dead
        );

        // Idle -> Alerted -> Chasing -> Attacking -> Dead
        sm.TransitionTo(EntityStates.Alerted);
        sm.TransitionTo(EntityStates.Chasing);
        sm.TransitionTo(EntityStates.Attacking);
        sm.ForceTransition(EntityStates.Dead);

        Assert.Equal(EntityStates.Dead, sm.CurrentStateId);
    }

    // ========================================
    // CUSTOM STATE TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void CustomState_Simple()
    {
        var customState = new SimpleEntityState(100, "Custom");
        var sm = new StateMachine(customState);

        Assert.Equal(100, sm.CurrentStateId);
        Assert.Equal("Custom", sm.CurrentStateName);
    }

    [Fact, Trait("Category", "Unit")]
    public void CustomState_WithTransitions()
    {
        var stateA = SimpleEntityState.Create(1, "A", 2, 3);
        var stateB = SimpleEntityState.Create(2, "B", 1);
        var sm = new StateMachine(stateA, stateB);

        Assert.True(sm.TransitionTo(2)); // A -> B
        Assert.True(sm.TransitionTo(1)); // B -> A
    }
}
