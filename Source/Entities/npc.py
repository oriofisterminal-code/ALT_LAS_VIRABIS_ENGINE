"""
ALT_LAS Engine - NPC Entity
Non-player characters with dialogue references and simple AI.
"""

from Source.Entities.entity import Entity


class NPC(Entity):
    """NPC with dialogue and optional patrol behavior."""

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

    def set_dialogue(self, dialogue_id: str) -> None:
        self.dialogue_id = dialogue_id

    def set_patrol(self, path: list[tuple[int, int]], speed: float = 1.0) -> None:
        self.patrol_path = path
        self.patrol_speed = speed
        self._patrol_index = 0

    def update(self, dt: float) -> None:
        if not self.patrol_path:
            return
        self._patrol_timer += dt
        if self._patrol_timer >= self.patrol_speed:
            self._patrol_timer = 0.0
            target = self.patrol_path[self._patrol_index]
            self.x, self.y = target
            self._patrol_index = (self._patrol_index + 1) % len(self.patrol_path)

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
        npc.interactable = data.get("interactable", True)
        return npc
