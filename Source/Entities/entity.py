"""
ALT_LAS Engine - Base Entity
Foundation for all game objects: players, NPCs, items.
"""

from typing import Optional


class Entity:
    """Base class for all world entities."""

    _next_id = 0

    def __init__(self, x: int = 0, y: int = 0, char: str = "?",
                 color: str = "white", name: str = ""):
        Entity._next_id += 1
        self.entity_id = f"entity_{Entity._next_id}"
        self.x = x
        self.y = y
        self.char = char
        self.color = color
        self.name = name or self.entity_id
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
