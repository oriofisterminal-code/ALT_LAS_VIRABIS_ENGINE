"""ALT_LAS Engine - Battle Entities - Actors, projectiles, bullet patterns."""
import random, math
from typing import Optional

from src.core.event_bus import (
    get_event_bus, DamageTakenEvent, HealedEvent, DeathEvent,
)


class BattleActor:
    """Combat participant (player or enemy) with event-driven damage/heal."""

    def __init__(self, name: str, hp: int, attack: int, defense: int, sprite: str = ""):
        self.name, self.max_hp, self.hp = name, hp, hp
        self.attack, self.defense, self.sprite = attack, defense, sprite
        self.status_effects: list = []
        self.team_id: Optional[int] = None
        self.tags: set[str] = set()
        self.crit_chance: float = 0.0
        self.crit_multiplier: float = 2.0

    @property
    def is_alive(self) -> bool:
        return self.hp > 0

    def take_damage(self, amount: int) -> int:
        """Apply damage (simple: base - defense, min 1) and publish DamageTakenEvent."""
        actual = max(1, amount - self.defense)
        previous_hp = self.hp
        self.hp = max(0, self.hp - actual)
        bus = get_event_bus()
        bus.publish(DamageTakenEvent(
            target=self, attacker=None, damage=actual,
            previous_health=previous_hp, current_health=self.hp,
        ))
        if self.hp <= 0:
            bus.publish(DeathEvent(entity=self, final_damage=actual))
        return actual

    def apply_damage(self, amount: float) -> None:
        """Raw damage application used by DamageSystem pipeline (no defense calc)."""
        previous_hp = self.hp
        self.hp = max(0, self.hp - amount)
        bus = get_event_bus()
        bus.publish(DamageTakenEvent(
            target=self, attacker=None, damage=amount,
            previous_health=previous_hp, current_health=self.hp,
        ))
        if self.hp <= 0:
            bus.publish(DeathEvent(entity=self, final_damage=amount))

    def heal(self, amount: int) -> int:
        """Heal and publish HealedEvent."""
        old = self.hp
        self.hp = min(self.max_hp, self.hp + amount)
        healed = self.hp - old
        if healed > 0:
            get_event_bus().publish(HealedEvent(
                target=self, healer=None, amount=healed,
                previous_health=old, current_health=self.hp,
            ))
        return healed


class Projectile:
    """A bullet in the dodge phase."""

    def __init__(self, x: float, y: float, vx: float, vy: float, char: str = "*", color: str = "red"):
        self.x, self.y, self.vx, self.vy = x, y, vx, vy
        self.char, self.color, self.active = char, color, True

    def update(self, dt: float) -> None:
        self.x += self.vx * dt
        self.y += self.vy * dt


class BulletPattern:
    """Generates projectile patterns."""

    @staticmethod
    def rain(arena_x: int, arena_w: int, y: float, count: int, speed: float) -> list:
        return [Projectile(float(random.randint(arena_x, arena_x + arena_w - 1)), y, 0, speed, "|", "red") for _ in range(count)]

    @staticmethod
    def spiral(cx: float, cy: float, count: int, speed: float, offset: float = 0) -> list:
        bullets = []
        for i in range(count):
            angle = offset + (2 * math.pi / count) * i
            bullets.append(Projectile(cx, cy, math.cos(angle) * speed, math.sin(angle) * speed, "*", "orange"))
        return bullets

    @staticmethod
    def wave(arena_x: int, arena_w: int, y: float, count: int, speed: float) -> list:
        spacing = arena_w / max(count, 1)
        return [Projectile(arena_x + spacing * i, y, 0, speed, "~", "cyan") for i in range(count)]

    @staticmethod
    def aimed(x: float, y: float, tx: float, ty: float, speed: float, count: int = 1, spread: float = 0) -> list:
        dx, dy = tx - x, ty - y
        dist = math.sqrt(dx * dx + dy * dy)
        if dist == 0:
            return []
        base = math.atan2(dy, dx)
        bullets = []
        for i in range(count):
            offset = spread * ((i / max(count - 1, 1)) - 0.5) if count > 1 else 0
            angle = base + offset
            bullets.append(Projectile(x, y, math.cos(angle) * speed, math.sin(angle) * speed, "O", "yellow"))
        return bullets
