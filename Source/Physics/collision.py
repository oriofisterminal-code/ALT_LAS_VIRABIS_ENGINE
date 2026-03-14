"""
ALT_LAS Engine - Collision System
Grid-based collision detection with support for:
- Solid walls (block movement)
- Trigger zones (fire events on enter)
- Water/special terrain types
"""

from typing import Optional, Callable


TILE_EMPTY = 0
TILE_WALL = 1
TILE_WATER = 2
TILE_TRIGGER = 3


class CollisionMap:
    """Grid-based collision data for a single map."""

    def __init__(self, width: int, height: int):
        self.width = width
        self.height = height
        self.grid: list[list[int]] = [
            [TILE_EMPTY for _ in range(width)] for _ in range(height)
        ]
        self._triggers: dict[tuple[int, int], str] = {}

    def set_tile(self, x: int, y: int, tile_type: int) -> None:
        if 0 <= x < self.width and 0 <= y < self.height:
            self.grid[y][x] = tile_type

    def get_tile(self, x: int, y: int) -> int:
        if 0 <= x < self.width and 0 <= y < self.height:
            return self.grid[y][x]
        return TILE_WALL

    def is_walkable(self, x: int, y: int) -> bool:
        tile = self.get_tile(x, y)
        return tile != TILE_WALL

    def is_solid(self, x: int, y: int) -> bool:
        return self.get_tile(x, y) == TILE_WALL

    def set_trigger(self, x: int, y: int, event_id: str) -> None:
        self.set_tile(x, y, TILE_TRIGGER)
        self._triggers[(x, y)] = event_id

    def get_trigger(self, x: int, y: int) -> Optional[str]:
        return self._triggers.get((x, y))

    def check_triggers(self, x: int, y: int) -> Optional[str]:
        if self.get_tile(x, y) == TILE_TRIGGER:
            return self._triggers.get((x, y))
        return None


class CollisionSystem:
    """Checks movement validity and fires trigger events."""

    def __init__(self):
        self._collision_map: Optional[CollisionMap] = None
        self._trigger_callback: Optional[Callable[[str], None]] = None
        self._entity_positions: dict[str, tuple[int, int]] = {}

    def set_collision_map(self, cmap: CollisionMap) -> None:
        self._collision_map = cmap

    def set_trigger_callback(self, callback: Callable[[str], None]) -> None:
        self._trigger_callback = callback

    def register_entity(self, entity_id: str, x: int, y: int) -> None:
        self._entity_positions[entity_id] = (x, y)

    def unregister_entity(self, entity_id: str) -> None:
        self._entity_positions.pop(entity_id, None)

    def update_entity_position(self, entity_id: str, x: int, y: int) -> None:
        self._entity_positions[entity_id] = (x, y)

    def can_move_to(self, x: int, y: int, ignore_entity: str = "") -> bool:
        if not self._collision_map:
            return True
        if not self._collision_map.is_walkable(x, y):
            return False
        for eid, pos in self._entity_positions.items():
            if eid != ignore_entity and pos == (x, y):
                return False
        return True

    def check_and_fire_triggers(self, x: int, y: int) -> None:
        if not self._collision_map:
            return
        event_id = self._collision_map.check_triggers(x, y)
        if event_id and self._trigger_callback:
            self._trigger_callback(event_id)

    def get_entities_at(self, x: int, y: int) -> list[str]:
        result = []
        for eid, pos in self._entity_positions.items():
            if pos == (x, y):
                result.append(eid)
        return result
