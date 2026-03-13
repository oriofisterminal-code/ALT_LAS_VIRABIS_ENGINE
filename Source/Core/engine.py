"""
ALT_LAS Engine - Core Game Engine
Main game loop with 30 FPS lock, input handling, and scene management.
"""

import json
import time
import os
from bearlibterminal import terminal

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def load_config() -> dict:
    config_path = os.path.join(BASE_DIR, "Config", "config.json")
    with open(config_path, "r", encoding="utf-8") as f:
        return json.load(f)


def load_keybindings() -> dict:
    kb_path = os.path.join(BASE_DIR, "Config", "keybindings.json")
    with open(kb_path, "r", encoding="utf-8") as f:
        return json.load(f)


class GameEngine:
    """Heart of ALT_LAS. Initializes terminal, runs game loop, delegates to scenes."""

    def __init__(self):
        self.config = load_config()
        self.keybindings = load_keybindings()
        self.width = self.config["window"]["width"]
        self.height = self.config["window"]["height"]
        self.title = self.config["window"]["title"]
        self.fps = self.config["window"]["fps"]
        self.is_running = False
        self.state_manager = None
        self.debug_mode = False
        self.delta_time = 0.0
        self._last_time = 0.0

    def initialize(self) -> None:
        terminal.open()
        terminal.set(
            f"window: size={self.width}x{self.height}, "
            f"title='{self.title}'"
        )
        font_cfg = self.config.get("font", {})
        font_name = font_cfg.get("name", "default")
        if font_name != "default":
            font_size = font_cfg.get("size", 16)
            terminal.set(f"font: {font_name}, size={font_size}")
        terminal.set("input.filter=[keyboard, mouse+]")
        terminal.refresh()
        self.is_running = True
        self._last_time = time.time()

    def set_state_manager(self, state_manager: "StateManager") -> None:
        self.state_manager = state_manager

    def resolve_key(self, key: int) -> str:
        """Convert BearLibTerminal key code to a string name."""
        key_map = {
            terminal.TK_UP: "TK_UP", terminal.TK_DOWN: "TK_DOWN",
            terminal.TK_LEFT: "TK_LEFT", terminal.TK_RIGHT: "TK_RIGHT",
            terminal.TK_W: "TK_W", terminal.TK_A: "TK_A",
            terminal.TK_S: "TK_S", terminal.TK_D: "TK_D",
            terminal.TK_Z: "TK_Z", terminal.TK_X: "TK_X",
            terminal.TK_I: "TK_I", terminal.TK_RETURN: "TK_RETURN",
            terminal.TK_ESCAPE: "TK_ESCAPE",
            terminal.TK_F1: "TK_F1", terminal.TK_F2: "TK_F2",
            terminal.TK_F3: "TK_F3", terminal.TK_F5: "TK_F5",
            terminal.TK_F9: "TK_F9",
        }
        return key_map.get(key, f"TK_{key}")

    def handle_input(self) -> None:
        while terminal.has_input():
            key = terminal.read()
            if key == terminal.TK_CLOSE:
                self.is_running = False
                return

            key_name = self.resolve_key(key)

            if key_name == self.keybindings["debug"]["toggle_debug"]:
                self.debug_mode = not self.debug_mode
                continue

            if self.state_manager and self.state_manager.current_scene:
                self.state_manager.current_scene.handle_input(key_name)

    def update(self) -> None:
        now = time.time()
        self.delta_time = now - self._last_time
        self._last_time = now
        if self.state_manager and self.state_manager.current_scene:
            self.state_manager.current_scene.update(self.delta_time)

    def render(self) -> None:
        terminal.clear()
        if self.state_manager and self.state_manager.current_scene:
            self.state_manager.current_scene.render()
        if self.debug_mode:
            fps_display = int(1.0 / self.delta_time) if self.delta_time > 0 else 0
            terminal.layer(3)
            terminal.printf(0, 0, f"[color=yellow]FPS:{fps_display} DT:{self.delta_time:.3f}")
        terminal.refresh()

    def run(self) -> None:
        self.initialize()
        try:
            while self.is_running:
                self.handle_input()
                self.update()
                self.render()
                time.sleep(1.0 / self.fps)
        finally:
            terminal.close()

    def shutdown(self) -> None:
        self.is_running = False
