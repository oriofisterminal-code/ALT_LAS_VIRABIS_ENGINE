"""
ALT_LAS Engine - Base Entity
Foundation for all game objects: players, NPCs, items.
Supports both ASCII and sprite-based rendering.
"""

from typing import Optional


class Entity:
    """Base class for all world entities."""

    _next_id = 0

    def __init__(
        self, x: int = 0, y: int = 0, char: str = "?",
        color: str = "white", name: str = "", sprite: str = ""
    ):
        Entity._next_id += 1
        self.entity_id = f"entity_{Entity._next_id}"
        self.x = x
        self.y = y
        self.char = char
        self.color = color
        self.name = name or self.entity_id
        self.sprite = sprite  # NEW: sprite path for graphical rendering
        self.visible = True
        self.blocking = False
        self.properties: dict = {}

    def update(self, dt: float) -> None:
        pass

    def get_position(self) -> tuple[int, int]:
        return (self.x, self.y)

    def set_position(self, x: int, y: int) -> None:
        self.x = x
        self.y = y

    def get_property(self, key: str, default=None):
        return self.properties.get(key, default)

    def set_property(self, key: str, value) -> None:
        self.properties[key] = value

    def to_dict(self) -> dict:
        """Serialize entity to dictionary."""
        return {
            "entity_id": self.entity_id,
            "x": self.x,
            "y": self.y,
            "char": self.char,
            "color": self.color,
            "name": self.name,
            "sprite": self.sprite,
            "visible": self.visible,
            "blocking": self.blocking,
            "properties": self.properties,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "Entity":
        """Deserialize entity from dictionary."""
        entity = cls(
            x=data.get("x", 0),
            y=data.get("y", 0),
            char=data.get("char", "?"),
            color=data.get("color", "white"),
            name=data.get("name", ""),
            sprite=data.get("sprite", ""),
        )
        entity.visible = data.get("visible", True)
        entity.blocking = data.get("blocking", False)
        entity.properties = data.get("properties", {})
        return entity


class EntityManager:
    """Tracks and manages all entities on the current map."""

    def __init__(self):
        self._entities: dict[str, Entity] = {}

    def add(self, entity: Entity) -> str:
        self._entities[entity.entity_id] = entity
        return entity.entity_id

    def remove(self, entity_id: str) -> Optional[Entity]:
        return self._entities.pop(entity_id, None)

    def get(self, entity_id: str) -> Optional[Entity]:
        return self._entities.get(entity_id)

    def get_at(self, x: int, y: int) -> list[Entity]:
        return [e for e in self._entities.values() if e.x == x and e.y == y]

    def get_by_name(self, name: str) -> Optional[Entity]:
        for e in self._entities.values():
            if e.name == name:
                return e
        return None

    def get_all(self) -> list[Entity]:
        return list(self._entities.values())

    def clear(self) -> None:
        self._entities.clear()

    def update_all(self, dt: float) -> None:
        for entity in self._entities.values():
            entity.update(dt)

    def find_path(self, start: tuple, end: tuple, collision_map) -> list:
        """Simple A* pathfinding for entity AI."""
        if not collision_map:
            return []

        open_set = {start}
        came_from = {}
        g_score = {start: 0}
        f_score = {start: self._heuristic(start, end)}

        while open_set:
            current = min(open_set, key=lambda p: f_score.get(p, float("inf")))

            if current == end:
                path = []
                while current in came_from:
                    path.append(current)
                    current = came_from[current]
                path.reverse()
                return path

            open_set.remove(current)

            for dx, dy in [(0, 1), (0, -1), (1, 0), (-1, 0)]:
                neighbor = (current[0] + dx, current[1] + dy)

                if not collision_map.is_walkable(neighbor[0], neighbor[1]):
                    continue

                tentative_g = g_score.get(current, float("inf")) + 1

                if tentative_g < g_score.get(neighbor, float("inf")):
                    came_from[neighbor] = current
                    g_score[neighbor] = tentative_g
                    f_score[neighbor] = tentative_g + self._heuristic(neighbor, end)
                    open_set.add(neighbor)

        return []

    def _heuristic(self, a: tuple, b: tuple) -> int:
        return abs(a[0] - b[0]) + abs(a[1] - b[1])
