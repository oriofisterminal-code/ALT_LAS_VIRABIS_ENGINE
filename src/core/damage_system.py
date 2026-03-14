"""
ALT_LAS Engine - Damage System
Ported from VirabisCore (C#) DamageSystem.

Sophisticated damage calculation pipeline:
  PreDamage -> Team check -> Base x HitCount -> Stealth -> Crit -> Clamp -> Apply

Reference: test_core/Virabis.Core/DamageSystem.cs
           test_core/Virabis.Core/DamageConfiguration.cs
           test_core/Virabis.Core/HitContext.cs
"""

import random as _random_module
from dataclasses import dataclass, field
from typing import Optional, Protocol

from src.core.event_bus import (
    EventBus,
    PreDamageEvent,
    AttackHitEvent,
    get_event_bus,
)


# ---------------------------------------------------------------------------
# Random Provider (matches VirabisCore IRandomProvider)
# ---------------------------------------------------------------------------

class RandomProvider(Protocol):
    """Interface for random number generation (allows deterministic testing)."""

    def next_double(self) -> float:
        """Return a random float in [0.0, 1.0)."""
        ...


class DefaultRandomProvider:
    """Default random provider using Python's random module."""

    def next_double(self) -> float:
        return _random_module.random()


# ---------------------------------------------------------------------------
# Damage Configuration (matches VirabisCore DamageConfiguration)
# ---------------------------------------------------------------------------

@dataclass
class DamageConfiguration:
    """
    Configurable parameters for the damage pipeline.

    Attributes:
        stealth_multiplier: Multiplier for stealth attacks on unaware targets.
        crit_chance_cap: Maximum allowed crit chance (0.0-1.0).
        min_damage: Minimum damage that can be dealt per hit.
        max_damage_cap: Maximum damage cap per hit.
        friendly_fire_enabled: Whether same-team damage is allowed.
    """

    stealth_multiplier: float = 2.0
    crit_chance_cap: float = 1.0
    min_damage: float = 0.0
    max_damage_cap: float = float("inf")
    friendly_fire_enabled: bool = False

    @staticmethod
    def default() -> "DamageConfiguration":
        return DamageConfiguration()

    @staticmethod
    def hardcore() -> "DamageConfiguration":
        return DamageConfiguration(stealth_multiplier=3.0, friendly_fire_enabled=True)

    @staticmethod
    def casual() -> "DamageConfiguration":
        return DamageConfiguration(stealth_multiplier=1.5, max_damage_cap=500.0)

    @staticmethod
    def test() -> "DamageConfiguration":
        return DamageConfiguration(
            stealth_multiplier=2.0, crit_chance_cap=1.0, max_damage_cap=1000.0,
        )


# ---------------------------------------------------------------------------
# Hit Context (matches VirabisCore HitContext)
# ---------------------------------------------------------------------------

@dataclass(frozen=True)
class HitContext:
    """
    Immutable container for all attack information.

    Attributes:
        attacker: The entity performing the attack.
        target: The entity being attacked.
        base_damage: Raw damage before modifiers.
        hit_count: Number of hits in this attack (multi-hit support).
        is_stealth_attack: Whether this is a stealth attack.
    """

    attacker: object
    target: object
    base_damage: float
    hit_count: int = 1
    is_stealth_attack: bool = False

    def __post_init__(self):
        if self.attacker is None:
            raise ValueError("attacker cannot be None")
        if self.target is None:
            raise ValueError("target cannot be None")
        if self.base_damage < 0:
            raise ValueError(f"base_damage cannot be negative: {self.base_damage}")
        if self.hit_count < 1:
            raise ValueError(f"hit_count must be at least 1: {self.hit_count}")

    @property
    def is_valid(self) -> bool:
        return self.attacker is not None and self.target is not None


class HitContextBuilder:
    """Builder pattern for constructing HitContext instances."""

    def __init__(self):
        self._attacker = None
        self._target = None
        self._base_damage: float = 10.0
        self._hit_count: int = 1
        self._is_stealth: bool = False

    def attacker(self, entity) -> "HitContextBuilder":
        self._attacker = entity
        return self

    def target(self, entity) -> "HitContextBuilder":
        self._target = entity
        return self

    def damage(self, amount: float) -> "HitContextBuilder":
        self._base_damage = amount
        return self

    def hits(self, count: int) -> "HitContextBuilder":
        self._hit_count = count
        return self

    def stealth(self, is_stealth: bool = True) -> "HitContextBuilder":
        self._is_stealth = is_stealth
        return self

    def build(self) -> HitContext:
        return HitContext(
            attacker=self._attacker,
            target=self._target,
            base_damage=self._base_damage,
            hit_count=self._hit_count,
            is_stealth_attack=self._is_stealth,
        )


# ---------------------------------------------------------------------------
# Damage System (matches VirabisCore DamageSystem)
# ---------------------------------------------------------------------------

class DamageSystem:
    """
    Core damage calculation pipeline.

    Pipeline order:
        1. PreDamage event (allows modification/cancellation)
        2. Friendly fire check
        3. Base damage x HitCount
        4. Stealth multiplier (if target unaware)
        5. Critical hit roll
        6. Damage capping (min/max)
        7. AttackHit event
        8. Apply damage to target

    Usage:
        config = DamageConfiguration.default()
        ds = DamageSystem(config=config)

        ctx = HitContext(attacker=player, target=enemy, base_damage=25.0)
        actual_damage = ds.process_hit(ctx)
    """

    def __init__(
        self,
        random_provider: Optional[RandomProvider] = None,
        config: Optional[DamageConfiguration] = None,
        event_bus: Optional[EventBus] = None,
    ):
        self._random = random_provider or DefaultRandomProvider()
        self._config = config or DamageConfiguration.default()
        self._event_bus = event_bus

    def process_hit(self, context: HitContext) -> float:
        """
        Process a hit through the full damage pipeline and apply damage.

        Returns the final damage dealt.
        """
        attacker = context.attacker
        target = context.target

        # Step 1: Publish PreDamage event (allows modification/cancellation)
        pre_damage = PreDamageEvent(attacker, target, context.base_damage, source=self)
        self._publish(pre_damage)

        if pre_damage.is_cancelled:
            return 0.0

        base_damage = pre_damage.final_damage

        # Step 2: Friendly fire check
        attacker_team = getattr(attacker, "team_id", None)
        target_team = getattr(target, "team_id", None)
        if (
            not self._config.friendly_fire_enabled
            and attacker_team is not None
            and attacker_team == target_team
        ):
            return 0.0

        # Step 3: Base damage with multi-hit
        damage = base_damage * context.hit_count

        # Step 4: Stealth multiplier (only if target is unaware)
        target_tags = getattr(target, "tags", set())
        has_aware_tag = (
            "aware" in target_tags
            if isinstance(target_tags, (set, list, frozenset))
            else False
        )
        is_stealth = context.is_stealth_attack and not has_aware_tag
        if is_stealth:
            damage *= self._config.stealth_multiplier

        # Step 5: Crit roll
        crit_chance = getattr(attacker, "crit_chance", 0.0)
        crit_chance = min(crit_chance, self._config.crit_chance_cap)
        is_critical = self._random.next_double() < crit_chance
        if is_critical:
            crit_multiplier = getattr(attacker, "crit_multiplier", 2.0)
            damage *= crit_multiplier

        # Step 6: Damage capping
        damage = max(self._config.min_damage, min(damage, self._config.max_damage_cap))

        # Step 7: Publish AttackHit event
        self._publish(
            AttackHitEvent(attacker, target, damage, is_critical, is_stealth, source=self)
        )

        # Step 8: Apply damage to target
        apply_damage_fn = getattr(target, "apply_damage", None)
        if callable(apply_damage_fn):
            apply_damage_fn(damage)
        else:
            # Fallback: directly reduce hp
            current_hp = getattr(target, "hp", 0)
            new_hp = max(0, current_hp - damage)
            if hasattr(target, "hp"):
                target.hp = new_hp

        return damage

    def calculate_expected_damage(self, context: HitContext) -> float:
        """
        Calculate expected (average) damage without applying it.
        Useful for UI damage previews and AI decision-making.
        """
        attacker = context.attacker
        target = context.target

        # Friendly fire check
        attacker_team = getattr(attacker, "team_id", None)
        target_team = getattr(target, "team_id", None)
        if (
            not self._config.friendly_fire_enabled
            and attacker_team is not None
            and attacker_team == target_team
        ):
            return 0.0

        damage = context.base_damage * context.hit_count

        # Stealth
        target_tags = getattr(target, "tags", set())
        has_aware_tag = (
            "aware" in target_tags
            if isinstance(target_tags, (set, list, frozenset))
            else False
        )
        if context.is_stealth_attack and not has_aware_tag:
            damage *= self._config.stealth_multiplier

        # Expected crit contribution (average over crit chance)
        crit_chance = getattr(attacker, "crit_chance", 0.0)
        crit_chance = min(crit_chance, self._config.crit_chance_cap)
        crit_multiplier = getattr(attacker, "crit_multiplier", 2.0)
        expected_crit_multiplier = 1.0 + (crit_chance * (crit_multiplier - 1.0))
        damage *= expected_crit_multiplier

        return max(self._config.min_damage, min(damage, self._config.max_damage_cap))

    def _publish(self, event) -> None:
        if self._event_bus is not None:
            self._event_bus.publish(event)
        # Also publish to the global bus
        get_event_bus().publish(event)
