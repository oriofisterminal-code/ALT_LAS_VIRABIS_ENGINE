"""
ALT_LAS Engine - Movement System
Handles grid-based entity movement with collision checking.
"""

from typing import Optional
from Source.Physics.collision import CollisionSystem


DIRECTION_MAP = {
    "up": (0, -1),
    "down": (0, 1),
    "left": (-1, 0),
    "right": (1, 0),
}


class MovementSystem:
    """Processes movement requests against collision system."""

    def __init__(self, collision: CollisionSystem):
        self.collision = collision
        self._move_cooldowns: dict[str, float] = {}
        self.cooldown_time = 0.12

    def try_move(
        self, entity_id: str, x: int, y: int,
        direction: str, current_time: float
    ) -> Optional[tuple[int, int]]:
        if entity_id in self._move_cooldowns:
            if current_time - self._move_cooldowns[entity_id] < self.cooldown_time:
                return None

        delta = DIRECTION_MAP.get(direction)
        if not delta:
            return None

        nx = x + delta[0]
        ny = y + delta[1]

        if self.collision.can_move_to(nx, ny, ignore_entity=entity_id):
            self.collision.update_entity_position(entity_id, nx, ny)
            self.collision.check_and_fire_triggers(nx, ny)
            self._move_cooldowns[entity_id] = current_time
            return (nx, ny)
        return None

    def teleport(self, entity_id: str, x: int, y: int) -> None:
        self.collision.update_entity_position(entity_id, x, y)
        self.collision.check_and_fire_triggers(x, y)

    def reset_cooldown(self, entity_id: str) -> None:
        self._move_cooldowns.pop(entity_id, None)

    def set_cooldown(self, cooldown: float) -> None:
        self.cooldown_time = cooldown
