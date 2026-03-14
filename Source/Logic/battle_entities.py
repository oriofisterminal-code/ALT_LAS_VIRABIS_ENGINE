"""
ALT_LAS Engine - Battle Entities
Data classes for combat: actors, projectiles, bullet patterns.
"""

import random
import math


class BattleActor:
    """Combat participant (player or enemy)."""

    def __init__(self, name: str, hp: int, attack: int, defense: int):
        self.name = name
        self.max_hp = hp
        self.hp = hp
        self.attack = attack
        self.defense = defense

    @property
    def is_alive(self) -> bool:
        return self.hp > 0

    def take_damage(self, amount: int) -> int:
        actual = max(1, amount - self.defense)
        self.hp = max(0, self.hp - actual)
        return actual


class Projectile:
    """A bullet in the dodge phase."""

    def __init__(self, x: float, y: float, vx: float, vy: float,
                 char: str = "*", color: str = "red"):
        self.x = x
        self.y = y
        self.vx = vx
        self.vy = vy
        self.char = char
        self.color = color
        self.active = True

    def update(self, dt: float) -> None:
        self.x += self.vx * dt
        self.y += self.vy * dt


class BulletPattern:
    """Generates projectile patterns for enemy attacks."""

    @staticmethod
    def rain(arena_x: int, arena_w: int, y: float,
             count: int, speed: float) -> list[Projectile]:
        bullets = []
        for _ in range(count):
            bx = random.randint(arena_x, arena_x + arena_w - 1)
            bullets.append(Projectile(float(bx), y, 0, speed, "|", "red"))
        return bullets

    @staticmethod
    def spiral(cx: float, cy: float, count: int,
               speed: float, offset: float) -> list[Projectile]:
        bullets = []
        for i in range(count):
            angle = offset + (2 * math.pi / count) * i
            vx = math.cos(angle) * speed
            vy = math.sin(angle) * speed
            bullets.append(Projectile(cx, cy, vx, vy, "*", "orange"))
        return bullets

    @staticmethod
    def wave(arena_x: int, arena_w: int, y: float,
             count: int, speed: float) -> list[Projectile]:
        bullets = []
        spacing = arena_w / max(count, 1)
        for i in range(count):
            bx = arena_x + spacing * i
            bullets.append(Projectile(bx, y, 0, speed, "~", "cyan"))
        return bullets
