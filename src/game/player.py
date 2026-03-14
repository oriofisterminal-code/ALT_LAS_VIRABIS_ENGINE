"""
ALT_LAS Engine - Player Entity
The player character with stats, inventory, flags, and sprite support.
"""

from Source.Entities.entity import Entity


class Player(Entity):
    """Player character with RPG stats, inventory, and sprite support."""

    def __init__(self, x: int = 0, y: int = 0):
        super().__init__(
            x=x, y=y,
            char="@",
            color="#ffff00",
            name="player",
            sprite="Battle/player.png"  # Default sprite
        )
        self.blocking = True

        # Core stats
        self.hp = 20
        self.max_hp = 20
        self.attack = 5
        self.defense = 2
        self.level = 1
        self.exp = 0
        self.gold = 0

        # Inventory and flags
        self.inventory: list[dict] = []
        self.flags: dict[str, bool] = {}

        # Movement
        self.facing = "down"
        self.move_speed = 1.0

        # Battle stats
        self.soul_color = "red"  # Default soul color

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

    def get_item(self, item_name: str) -> dict | None:
        for item in self.inventory:
            if item.get("name") == item_name:
                return item
        return None

    def set_flag(self, flag: str, value: bool = True) -> None:
        self.flags[flag] = value

    def get_flag(self, flag: str) -> bool:
        return self.flags.get(flag, False)

    def heal(self, amount: int) -> int:
        old_hp = self.hp
        self.hp = min(self.max_hp, self.hp + amount)
        return self.hp - old_hp

    def take_damage(self, amount: int) -> int:
        actual = max(1, amount - self.defense)
        self.hp = max(0, self.hp - actual)
        return actual

    def gain_exp(self, amount: int) -> bool:
        """Gain experience and check for level up. Returns True if leveled up."""
        self.exp += amount
        exp_needed = self.level * 10
        if self.exp >= exp_needed:
            self.level_up()
            return True
        return False

    def level_up(self) -> None:
        self.level += 1
        self.max_hp += 4
        self.hp = self.max_hp
        self.attack += 2
        self.defense += 1
        self.exp = 0

    def set_sprite(self, sprite_path: str) -> None:
        """Set the player's sprite."""
        self.sprite = sprite_path

    def set_soul_color(self, color: str) -> None:
        """Set the soul color for battle mode."""
        self.soul_color = color

    def update(self, dt: float) -> None:
        pass

    def to_save_data(self) -> dict:
        return {
            "x": self.x, "y": self.y,
            "hp": self.hp, "max_hp": self.max_hp,
            "attack": self.attack, "defense": self.defense,
            "level": self.level, "exp": self.exp, "gold": self.gold,
            "inventory": self.inventory,
            "flags": self.flags,
            "sprite": self.sprite,
            "soul_color": self.soul_color,
            "facing": self.facing,
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
        self.sprite = data.get("sprite", "Battle/player.png")
        self.soul_color = data.get("soul_color", "red")
        self.facing = data.get("facing", "down")
