"""
ALT_LAS Engine - State Machine System
Ported from VirabisCore (C#) StateMachine architecture.

Generic state machine for managing entity states with:
- Allowed transition validation
- Enter/Exit/Update lifecycle hooks
- Event Bus integration for state change notifications
- Force transitions for special cases (death, stun)

Reference: test_core/Virabis.Core/State/StateMachine.cs
           test_core/Virabis.Core/State/IEntityState.cs
           test_core/Virabis.Core/State/EntityStates.cs
           test_core/Virabis.Core/AI/EnemyAI.cs
"""

import math
import logging
from dataclasses import dataclass, field
from typing import Optional, Callable

from src.core.event_bus import StateChangedEvent, get_event_bus

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# State IDs (matches VirabisCore EntityStates constants)
# ---------------------------------------------------------------------------

class StateID:
    """Pre-defined state IDs for common entity states."""

    # Common entity states (0-9)
    IDLE = 0
    MOVING = 1
    ATTACKING = 2
    DEFENDING = 3
    STUNNED = 4
    DEAD = 5
    INVISIBLE = 6
    STEALTHED = 7

    # AI-specific states (10-19)
    PATROLLING = 10
    CHASING = 11
    FLEEING = 12
    ALERTED = 13

    # Player-specific states (20-29)
    JUMPING = 20
    CROUCHING = 21
    INTERACTING = 22

    _NAMES = {
        0: "Idle", 1: "Moving", 2: "Attacking", 3: "Defending",
        4: "Stunned", 5: "Dead", 6: "Invisible", 7: "Stealthed",
        10: "Patrolling", 11: "Chasing", 12: "Fleeing", 13: "Alerted",
        20: "Jumping", 21: "Crouching", 22: "Interacting",
    }

    @classmethod
    def get_name(cls, state_id: int) -> str:
        return cls._NAMES.get(state_id, f"Unknown({state_id})")

    @classmethod
    def is_valid(cls, state_id: int) -> bool:
        return state_id in cls._NAMES


# ---------------------------------------------------------------------------
# Entity State (matches VirabisCore IEntityState)
# ---------------------------------------------------------------------------

class EntityState:
    """
    Base class for entity states.

    Override on_enter, on_exit, and on_update for custom behavior.
    Set allowed_transitions to control which states can be transitioned to.
    """

    def __init__(self, state_id: int, name: str, allowed_transitions: Optional[list[int]] = None):
        self._id = state_id
        self._name = name
        self._allowed_transitions: set[int] = set(allowed_transitions or [])

    @property
    def id(self) -> int:
        return self._id

    @property
    def name(self) -> str:
        return self._name

    @property
    def allowed_transitions(self) -> set[int]:
        return self._allowed_transitions

    def allow_transitions_to(self, *state_ids: int) -> None:
        """Add allowed transition targets."""
        self._allowed_transitions.update(state_ids)

    def on_enter(self) -> None:
        """Called when entering this state."""
        pass

    def on_exit(self) -> None:
        """Called when exiting this state."""
        pass

    def on_update(self, delta_time: float) -> None:
        """Called every frame while in this state."""
        pass

    def __repr__(self) -> str:
        return f"EntityState({self._id}, '{self._name}')"


def create_state(state_id: int, name: str, *allowed_transitions: int) -> EntityState:
    """Convenience factory for creating simple states."""
    return EntityState(state_id, name, list(allowed_transitions))


# ---------------------------------------------------------------------------
# Common State Presets (matches VirabisCore CommonStates)
# ---------------------------------------------------------------------------

class CommonStates:
    """Pre-configured state sets for common use cases."""

    @staticmethod
    def combatant() -> list[EntityState]:
        """Basic combat entity states: Idle -> Moving -> Attacking -> Stunned -> Dead."""
        idle = create_state(StateID.IDLE, "Idle",
                            StateID.MOVING, StateID.ATTACKING, StateID.STUNNED, StateID.DEAD)
        moving = create_state(StateID.MOVING, "Moving",
                              StateID.IDLE, StateID.ATTACKING, StateID.JUMPING, StateID.DEAD)
        attacking = create_state(StateID.ATTACKING, "Attacking",
                                 StateID.IDLE, StateID.STUNNED, StateID.DEAD)
        stunned = create_state(StateID.STUNNED, "Stunned",
                               StateID.IDLE, StateID.DEAD)
        dead = create_state(StateID.DEAD, "Dead")
        return [idle, moving, attacking, stunned, dead]

    @staticmethod
    def ai() -> list[EntityState]:
        """AI states: Idle -> Patrolling -> Chasing -> Attacking -> Fleeing."""
        idle = create_state(StateID.IDLE, "Idle",
                            StateID.PATROLLING, StateID.ALERTED, StateID.CHASING, StateID.DEAD)
        patrolling = create_state(StateID.PATROLLING, "Patrolling",
                                  StateID.IDLE, StateID.ALERTED, StateID.CHASING, StateID.DEAD)
        chasing = create_state(StateID.CHASING, "Chasing",
                               StateID.ATTACKING, StateID.IDLE, StateID.FLEEING, StateID.DEAD)
        attacking = create_state(StateID.ATTACKING, "Attacking",
                                 StateID.CHASING, StateID.IDLE, StateID.FLEEING, StateID.DEAD)
        fleeing = create_state(StateID.FLEEING, "Fleeing",
                               StateID.IDLE, StateID.DEAD)
        alerted = create_state(StateID.ALERTED, "Alerted",
                               StateID.IDLE, StateID.CHASING, StateID.DEAD)
        dead = create_state(StateID.DEAD, "Dead")
        return [idle, patrolling, chasing, attacking, fleeing, alerted, dead]


# ---------------------------------------------------------------------------
# State Machine (matches VirabisCore StateMachine)
# ---------------------------------------------------------------------------

class StateMachine:
    """
    Generic state machine for managing entity states.

    Supports:
    - Allowed transition validation
    - Enter/Exit/Update lifecycle
    - Event Bus integration (publishes StateChangedEvent)
    - Force transitions for special cases (death, stun)
    - Previous state tracking

    Usage:
        states = CommonStates.combatant()
        sm = StateMachine(states, owner=player_entity)

        sm.update(delta_time)
        sm.transition_to(StateID.ATTACKING)
        sm.force_transition(StateID.DEAD)
    """

    def __init__(self, states: list[EntityState], owner=None):
        if not states:
            raise ValueError("At least one state is required")

        self._states: dict[int, EntityState] = {s.id: s for s in states}
        self._current_state: EntityState = states[0]
        self._previous_state: Optional[EntityState] = None
        self._time_in_current_state: float = 0.0
        self._owner = owner

        # Legacy callback support
        self._on_state_change: Optional[Callable] = None

        self._current_state.on_enter()

    @property
    def current_state_id(self) -> int:
        return self._current_state.id

    @property
    def current_state_name(self) -> str:
        return self._current_state.name

    @property
    def current_state(self) -> EntityState:
        return self._current_state

    @property
    def previous_state(self) -> Optional[EntityState]:
        return self._previous_state

    @property
    def time_in_current_state(self) -> float:
        return self._time_in_current_state

    @property
    def owner(self):
        return self._owner

    def set_on_state_change(self, callback: Optional[Callable]) -> None:
        """Set a legacy callback for state changes."""
        self._on_state_change = callback

    def add_state(self, state: EntityState) -> None:
        """Add a state to the machine."""
        self._states[state.id] = state

    def get_state(self, state_id: int) -> Optional[EntityState]:
        """Get a state by ID."""
        return self._states.get(state_id)

    def can_transition_to(self, target_state_id: int) -> bool:
        """Check if transition to target state is allowed."""
        if target_state_id not in self._states:
            return False
        return target_state_id in self._current_state.allowed_transitions

    def transition_to(self, target_state_id: int) -> bool:
        """
        Attempt to transition to a new state.
        Returns True if the transition was allowed and performed.
        """
        if not self.can_transition_to(target_state_id):
            return False
        new_state = self._states[target_state_id]
        self._perform_transition(new_state)
        return True

    def force_transition(self, target_state_id: int) -> None:
        """
        Force a transition without checking allowed transitions.
        Use for special cases like death, stun, etc.
        """
        new_state = self._states.get(target_state_id)
        if new_state is None:
            raise ValueError(f"State {target_state_id} not found")
        self._perform_transition(new_state)

    def return_to_previous(self) -> bool:
        """Transition back to the previous state (if allowed)."""
        if self._previous_state is None:
            return False
        return self.transition_to(self._previous_state.id)

    def update(self, delta_time: float) -> None:
        """Update the state machine. Call every frame."""
        self._time_in_current_state += delta_time
        self._current_state.on_update(delta_time)

    def reset(self, initial_state: Optional[EntityState] = None) -> None:
        """Reset to initial state (or a specified state)."""
        self._current_state.on_exit()
        self._previous_state = None
        self._time_in_current_state = 0.0

        if initial_state is not None:
            self._current_state = initial_state
            self._states[initial_state.id] = initial_state

        self._current_state.on_enter()

    def _perform_transition(self, new_state: EntityState) -> None:
        previous_state = self._current_state
        previous_time = self._time_in_current_state

        self._current_state.on_exit()
        self._previous_state = self._current_state
        self._current_state = new_state
        self._time_in_current_state = 0.0
        self._current_state.on_enter()

        # Publish StateChangedEvent via global Event Bus
        if self._owner is not None:
            get_event_bus().publish(StateChangedEvent(
                entity=self._owner,
                previous_state=previous_state.name,
                new_state=new_state.name,
                previous_state_id=previous_state.id,
                new_state_id=new_state.id,
                time_in_previous_state=previous_time,
                source=self,
            ))

        # Legacy callback
        if self._on_state_change is not None:
            self._on_state_change(previous_state, new_state)

    def __repr__(self) -> str:
        return f"StateMachine[{self.current_state_name}]"


# ---------------------------------------------------------------------------
# Enemy AI (matches VirabisCore EnemyAI)
# ---------------------------------------------------------------------------

class AIState:
    """AI state enum."""
    IDLE = 0
    CHASE = 1
    ATTACK = 2
    DEAD = 3


@dataclass
class EnemyAIConfig:
    """Configuration for enemy AI behavior."""
    detection_range: float = 10.0
    attack_range: float = 2.0
    attack_cooldown: float = 1.0


@dataclass
class AICommand:
    """Output from AI update: where to move and whether to attack."""
    move_direction: tuple[float, float] = (0.0, 0.0)
    should_attack: bool = False


class EnemyAI:
    """
    Pure Python enemy AI with state machine logic.
    Ported from VirabisCore EnemyAI (zero Godot dependencies).

    States: Idle -> Chase -> Attack -> Dead

    Usage:
        config = EnemyAIConfig(detection_range=8.0, attack_range=2.0, attack_cooldown=1.5)
        ai = EnemyAI(config)

        command = ai.update(delta_time, enemy_pos=(5, 10), player_pos=(8, 12))
        if command.should_attack:
            # perform attack
            pass
        # move enemy by command.move_direction
    """

    def __init__(self, config: EnemyAIConfig):
        self._config = config
        self._current_state = AIState.IDLE
        self._time_since_last_attack: float = 0.0

    @property
    def current_state(self) -> int:
        return self._current_state

    @property
    def time_since_last_attack(self) -> float:
        return self._time_since_last_attack

    def set_dead(self) -> None:
        """Force the AI into the Dead state (terminal)."""
        self._current_state = AIState.DEAD

    def update(
        self,
        delta_time: float,
        enemy_pos: tuple[float, float],
        player_pos: tuple[float, float],
    ) -> AICommand:
        """
        Update AI state and return a command.

        Args:
            delta_time: Time since last frame.
            enemy_pos: (x, y) position of the enemy.
            player_pos: (x, y) position of the player.

        Returns:
            AICommand with move_direction and should_attack.
        """
        # Validate positions
        ex, ey = enemy_pos
        px, py = player_pos
        if not (math.isfinite(ex) and math.isfinite(ey) and
                math.isfinite(px) and math.isfinite(py)):
            self._current_state = AIState.IDLE
            return AICommand()

        # Dead state is terminal
        if self._current_state == AIState.DEAD:
            return AICommand()

        delta_time = max(0.0, delta_time)
        self._time_since_last_attack += delta_time

        # Calculate distance
        dx = px - ex
        dy = py - ey
        distance = math.sqrt(dx * dx + dy * dy)

        # State transitions
        if self._current_state == AIState.IDLE:
            if distance <= self._config.detection_range:
                self._current_state = AIState.CHASE

        elif self._current_state == AIState.CHASE:
            if distance <= self._config.attack_range:
                self._current_state = AIState.ATTACK
            elif distance > self._config.detection_range:
                self._current_state = AIState.IDLE

        elif self._current_state == AIState.ATTACK:
            if distance > self._config.attack_range:
                self._current_state = AIState.CHASE

        # Calculate move direction
        move_direction = (0.0, 0.0)
        if self._current_state in (AIState.CHASE, AIState.ATTACK):
            length_sq = dx * dx + dy * dy
            if length_sq > 0:
                length = math.sqrt(length_sq)
                move_direction = (dx / length, dy / length)

        # Determine if should attack
        should_attack = False
        if (
            self._current_state == AIState.ATTACK
            and self._time_since_last_attack >= self._config.attack_cooldown
        ):
            should_attack = True
            self._time_since_last_attack = 0.0

        return AICommand(move_direction=move_direction, should_attack=should_attack)
