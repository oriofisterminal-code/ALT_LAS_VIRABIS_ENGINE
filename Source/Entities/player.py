"""
ALT_LAS Engine - Player Entity
The player character with stats, inventory, and flags.
"""

from Source.Entities.entity import Entity


class Player(Entity):
    """Player character with RPG stats and inventory."""

    def __init__(self, x: int = 0, y: int = 0):
        super().__init__(x=x, y=y, char="@", color="#ffff00", name="player")
        self.blocking = True
        self.hp = 20
        self.max_hp = 20
        self.attack = 5
        self.defense = 2
        self.level = 1
        self.exp = 0
        self.gold = 0
        self.inventory: list[dict] = []
        self.flags: dict[str, bool] = {}

    def add_item(self, item: dict) -> None:
        self.inventory.append(item)

    def remove_item(self, item_name: str) -> bool:
        for i, item in enumerate(self.inventory):
            if item.get("name") == item_name:
                self.inventory.pop(i)
                return True
        return False

    def has_item(self, item_name: str) -> bool:
        return any(item.get("name") == item_name for item in self.inventory)

    def set_flag(self, flag: str, value: bool = True) -> None:
        self.flags[flag] = value

    def get_flag(self, flag: str) -> bool:
        return self.flags.get(flag, False)

    def heal(self, amount: int) -> int:
        old_hp = self.hp
        self.hp = min(self.max_hp, self.hp + amount)
        return self.hp - old_hp

    def to_save_data(self) -> dict:
        return {
            "x": self.x, "y": self.y,
            "hp": self.hp, "max_hp": self.max_hp,
            "attack": self.attack, "defense": self.defense,
            "level": self.level, "exp": self.exp, "gold": self.gold,
            "inventory": self.inventory,
            "flags": self.flags,
        }

    def from_save_data(self, data: dict) -> None:
        self.x = data.get("x", 0)
        self.y = data.get("y", 0)
        self.hp = data.get("hp", 20)
        self.max_hp = data.get("max_hp", 20)
        self.attack = data.get("attack", 5)
        self.defense = data.get("defense", 2)
        self.level = data.get("level", 1)
        self.exp = data.get("exp", 0)
        self.gold = data.get("gold", 0)
        self.inventory = data.get("inventory", [])
        self.flags = data.get("flags", {})
