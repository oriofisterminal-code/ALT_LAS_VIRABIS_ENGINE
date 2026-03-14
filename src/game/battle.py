"""
ALT_LAS Engine - Battle System
Turn-based combat with Undertale-style bullet hell dodge mechanic.
Supports both ASCII and sprite-based rendering.
"""

from typing import Optional
from Source.Rendering.layer_manager import (
    draw_text, draw_char, draw_box, draw_bar, draw_sprite,
    LAYER_UI, LAYER_EFFECTS
)
from Source.Rendering.terminal_detect import get_render_mode, RenderMode
from Source.Logic.battle_entities import BattleActor, Projectile, BulletPattern

PHASE_MENU = "menu"
PHASE_DODGE = "dodge"
PHASE_RESULT = "result"


class BattleSystem:
    """Manages turn-based combat with a dodge phase."""

    def __init__(self):
        self.player: Optional[BattleActor] = None
        self.enemy: Optional[BattleActor] = None
        self.phase = PHASE_MENU
        self.menu_options = ["FIGHT", "ACT", "ITEM", "MERCY"]
        self.selected_option = 0
        self.projectiles: list[Projectile] = []
        self.soul_x = 40.0
        self.soul_y = 18.0
        self.soul_speed = 15.0
        self.arena_x = 20
        self.arena_y = 12
        self.arena_w = 40
        self.arena_h = 10
        self.dodge_timer = 0.0
        self.dodge_duration = 4.0
        self._spawn_timer = 0.0
        self._spawn_interval = 0.5
        self._result_text = ""
        self._is_active = False
        self._on_end_callback = None
        self._soul_moving = {"up": False, "down": False, "left": False, "right": False}
        self._use_sprites = False

    @property
    def is_active(self) -> bool:
        return self._is_active

    def set_on_end(self, callback) -> None:
        self._on_end_callback = callback

    def start_battle(self, player: BattleActor, enemy: BattleActor) -> None:
        self.player = player
        self.enemy = enemy
        self.phase = PHASE_MENU
        self.selected_option = 0
        self.projectiles.clear()
        self.soul_x = float(self.arena_x + self.arena_w // 2)
        self.soul_y = float(self.arena_y + self.arena_h - 2)
        self._is_active = True
        self._result_text = ""

        # Check if sprites should be used
        render_mode = get_render_mode()
        self._use_sprites = render_mode in (RenderMode.SIXEL, RenderMode.KITTY)

    def handle_input(self, key: str) -> None:
        if not self._is_active:
            return
        if self.phase == PHASE_MENU:
            self._handle_menu_input(key)
        elif self.phase == PHASE_DODGE:
            self._handle_dodge_input(key)
        elif self.phase == PHASE_RESULT:
            if key in ("TK_Z", "TK_RETURN"):
                self._check_battle_end()

    def _handle_menu_input(self, key: str) -> None:
        if key in ("TK_LEFT", "TK_A"):
            self.selected_option = max(0, self.selected_option - 1)
        elif key in ("TK_RIGHT", "TK_D"):
            self.selected_option = min(len(self.menu_options) - 1, self.selected_option + 1)
        elif key in ("TK_Z", "TK_RETURN"):
            self._execute_menu_action()

    def _handle_dodge_input(self, key: str) -> None:
        if key in ("TK_UP", "TK_W"):
            self._soul_moving["up"] = True
        elif key in ("TK_DOWN", "TK_S"):
            self._soul_moving["down"] = True
        elif key in ("TK_LEFT", "TK_A"):
            self._soul_moving["left"] = True
        elif key in ("TK_RIGHT", "TK_D"):
            self._soul_moving["right"] = True

    def _execute_menu_action(self) -> None:
        action = self.menu_options[self.selected_option]
        if action == "FIGHT":
            damage = self.player.attack if self.player else 1
            if self.enemy:
                actual = self.enemy.take_damage(damage)
                self._result_text = f"You dealt {actual} damage!"
            self._start_dodge_phase()
        elif action == "MERCY":
            self._result_text = "You showed mercy..."
            self._is_active = False
            if self._on_end_callback:
                self._on_end_callback("mercy")
        elif action == "ITEM":
            if self.player:
                heal = min(20, self.player.max_hp - self.player.hp)
                self.player.hp += heal
                self._result_text = f"Healed {heal} HP!"
            self._start_dodge_phase()
        elif action == "ACT":
            self._result_text = "You checked the enemy..."
            self._start_dodge_phase()

    def _start_dodge_phase(self) -> None:
        self.phase = PHASE_DODGE
        self.dodge_timer = 0.0
        self.projectiles.clear()
        self._spawn_timer = 0.0
        self._soul_moving = {d: False for d in self._soul_moving}

    def update(self, dt: float) -> None:
        if not self._is_active:
            return
        if self.phase == PHASE_DODGE:
            self._update_dodge(dt)

    def _update_dodge(self, dt: float) -> None:
        self.dodge_timer += dt
        if self.dodge_timer >= self.dodge_duration:
            self.phase = PHASE_MENU
            self.projectiles.clear()
            self._soul_moving = {d: False for d in self._soul_moving}
            return

        dx, dy = 0.0, 0.0
        if self._soul_moving["up"]:
            dy = -self.soul_speed * dt
        if self._soul_moving["down"]:
            dy = self.soul_speed * dt
        if self._soul_moving["left"]:
            dx = -self.soul_speed * dt
        if self._soul_moving["right"]:
            dx = self.soul_speed * dt

        self.soul_x = max(self.arena_x + 1, min(self.arena_x + self.arena_w - 2, self.soul_x + dx))
        self.soul_y = max(self.arena_y + 1, min(self.arena_y + self.arena_h - 2, self.soul_y + dy))
        self._soul_moving = {d: False for d in self._soul_moving}

        self._spawn_timer += dt
        if self._spawn_timer >= self._spawn_interval:
            self._spawn_timer = 0.0
            new_bullets = BulletPattern.rain(
                self.arena_x + 1, self.arena_w - 2,
                float(self.arena_y), 3, 8.0
            )
            self.projectiles.extend(new_bullets)

        for p in self.projectiles:
            p.update(dt)
            if not (self.arena_x < p.x < self.arena_x + self.arena_w - 1 and
                    self.arena_y < p.y < self.arena_y + self.arena_h - 1):
                p.active = False
            if p.active and abs(p.x - self.soul_x) < 1 and abs(p.y - self.soul_y) < 1:
                if self.player:
                    enemy_atk = self.enemy.attack if self.enemy else 3
                    self.player.take_damage(enemy_atk)
                p.active = False

        self.projectiles = [p for p in self.projectiles if p.active]

    def _check_battle_end(self) -> None:
        if self.enemy and not self.enemy.is_alive:
            self._is_active = False
            if self._on_end_callback:
                self._on_end_callback("victory")
            return
        if self.player and not self.player.is_alive:
            self._is_active = False
            if self._on_end_callback:
                self._on_end_callback("defeat")
            return
        self.phase = PHASE_MENU
        self.selected_option = 0

    def render(self) -> None:
        if not self._is_active:
            return

        # Enemy info
        if self.enemy:
            draw_text(self.arena_x, self.arena_y - 3, f"{self.enemy.name}", color="red", layer=LAYER_UI)
            draw_text(
                self.arena_x, self.arena_y - 2,
                f"HP: {self.enemy.hp}/{self.enemy.max_hp}",
                color="red", layer=LAYER_UI
            )

        # Player HP
        if self.player:
            draw_text(2, 23, f"HP: {self.player.hp}/{self.player.max_hp}", color="green", layer=LAYER_UI)
            draw_bar(
                12, 23, 20, self.player.hp, self.player.max_hp,
                filled_color="green", layer=LAYER_UI
            )

        if self.phase == PHASE_DODGE:
            self._render_arena()
        elif self.phase == PHASE_MENU:
            self._render_menu()

        if self._result_text:
            draw_text(self.arena_x, self.arena_y - 1, self._result_text, color="yellow", layer=LAYER_UI)

    def _render_arena(self) -> None:
        # Draw arena box
        draw_box(
            self.arena_x, self.arena_y, self.arena_w, self.arena_h,
            border_color="white", fill_color="black", layer=LAYER_UI
        )

        # Render projectiles
        for p in self.projectiles:
            if self._use_sprites:
                sprite_name = "Battle/bullet_circle.png"
                success = draw_sprite(int(p.x), int(p.y), sprite_name, layer=LAYER_EFFECTS)
                if success:
                    continue
            draw_char(int(p.x), int(p.y), p.char, color=p.color, layer=LAYER_EFFECTS)

        # Render player soul (heart)
        if self._use_sprites:
            success = draw_sprite(int(self.soul_x), int(self.soul_y), "Battle/player_heart.png", layer=LAYER_EFFECTS)
            if success:
                return
        # Fallback: ASCII heart
        draw_char(int(self.soul_x), int(self.soul_y), "*", color="red", layer=LAYER_EFFECTS)

    def _render_menu(self) -> None:
        spacing = self.arena_w // len(self.menu_options)
        for i, opt in enumerate(self.menu_options):
            x = self.arena_x + spacing * i
            color = "#ffff00" if i == self.selected_option else "white"
            prefix = "[" if i == self.selected_option else " "
            suffix = "]" if i == self.selected_option else " "
            draw_text(x, self.arena_y + self.arena_h + 1, f"{prefix}{opt}{suffix}", color=color, layer=LAYER_UI)
