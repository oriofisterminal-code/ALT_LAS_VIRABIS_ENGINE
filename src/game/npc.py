"""
ALT_LAS Engine - NPC Entity
Non-player characters with dialogue references and AI.
Now integrates VirabisCore-style EnemyAI state machine for combat NPCs.
"""

from typing import Optional

from src.game.entity import Entity
from src.core.state_machine import EnemyAI, EnemyAIConfig, AIState
from src.core.event_bus import get_event_bus, DamageTakenEvent, DeathEvent


class NPC(Entity):
    """NPC with dialogue, patrol behavior, and optional AI state machine."""

    def __init__(self, x: int = 0, y: int = 0, char: str = "N",
                 color: str = "#00ff00", name: str = "npc"):
        super().__init__(x=x, y=y, char=char, color=color, name=name)
        self.blocking = True
        self.dialogue_id = ""
        self.patrol_path: list[tuple[int, int]] = []
        self._patrol_index = 0
        self._patrol_timer = 0.0
        self.patrol_speed = 1.0
        self.interactable = True
        self.facing = "down"
        self.stats = {"hp": 10, "attack": 3, "defense": 1}

        # VirabisCore-style attributes for DamageSystem compatibility
        self.hp: int = 10
        self.max_hp: int = 10
        self.team_id: Optional[int] = None
        self.tags: set[str] = set()
        self.crit_chance: float = 0.0
        self.crit_multiplier: float = 2.0

        # AI state machine (None = passive NPC, set via enable_combat_ai)
        self._ai: Optional[EnemyAI] = None

    def enable_combat_ai(
        self,
        detection_range: float = 8.0,
        attack_range: float = 2.0,
        attack_cooldown: float = 1.5,
    ) -> None:
        """Enable VirabisCore-style AI state machine for this NPC."""
        config = EnemyAIConfig(
            detection_range=detection_range,
            attack_range=attack_range,
            attack_cooldown=attack_cooldown,
        )
        self._ai = EnemyAI(config)

    @property
    def ai(self) -> Optional[EnemyAI]:
        return self._ai

    @property
    def ai_state(self) -> Optional[int]:
        return self._ai.current_state if self._ai else None

    def set_dialogue(self, dialogue_id: str) -> None:
        self.dialogue_id = dialogue_id

    def set_patrol(self, path: list[tuple[int, int]], speed: float = 1.0) -> None:
        self.patrol_path = path
        self.patrol_speed = speed
        self._patrol_index = 0

    def apply_damage(self, amount: float) -> None:
        """Raw damage application used by DamageSystem pipeline."""
        previous_hp = self.hp
        self.hp = max(0, int(self.hp - amount))
        bus = get_event_bus()
        bus.publish(DamageTakenEvent(
            target=self, attacker=None, damage=amount,
            previous_health=previous_hp, current_health=self.hp,
        ))
        if self.hp <= 0:
            bus.publish(DeathEvent(entity=self, final_damage=amount))
            if self._ai is not None:
                self._ai.set_dead()

    def update(self, dt: float) -> None:
        """Update patrol or AI (if AI is enabled, it takes priority over patrol)."""
        if not self.patrol_path:
            return
        self._patrol_timer += dt
        if self._patrol_timer >= self.patrol_speed:
            self._patrol_timer = 0.0
            target = self.patrol_path[self._patrol_index]
            self.x, self.y = target
            self._patrol_index = (self._patrol_index + 1) % len(self.patrol_path)

    def update_ai(self, dt: float, player_pos: tuple[float, float]) -> dict:
        """
        Update combat AI and return the AI command.

        Returns dict with 'move_direction' and 'should_attack' keys,
        or empty dict if AI is not enabled.
        """
        if self._ai is None:
            return {}
        cmd = self._ai.update(dt, (float(self.x), float(self.y)), player_pos)
        return {
            "move_direction": cmd.move_direction,
            "should_attack": cmd.should_attack,
            "ai_state": self._ai.current_state,
        }

    @staticmethod
    def from_data(data: dict) -> "NPC":
        npc = NPC(
            x=data.get("x", 0),
            y=data.get("y", 0),
            char=data.get("char", "N"),
            color=data.get("color", "#00ff00"),
            name=data.get("name", "npc"),
        )
        npc.dialogue_id = data.get("dialogue_id", "")
        if "patrol" in data:
            path = [tuple(p) for p in data["patrol"].get("path", [])]
            speed = data["patrol"].get("speed", 1.0)
            npc.set_patrol(path, speed)
        if "stats" in data:
            npc.stats = data["stats"]
            npc.hp = data["stats"].get("hp", 10)
            npc.max_hp = data["stats"].get("hp", 10)
        if "ai" in data:
            ai_cfg = data["ai"]
            npc.enable_combat_ai(
                detection_range=ai_cfg.get("detection_range", 8.0),
                attack_range=ai_cfg.get("attack_range", 2.0),
                attack_cooldown=ai_cfg.get("attack_cooldown", 1.5),
            )
        npc.interactable = data.get("interactable", True)
        return npc
