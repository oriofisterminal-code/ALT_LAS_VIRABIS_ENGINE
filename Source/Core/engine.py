"""
ALT_LAS Engine - Core Game Engine
Main game loop with 30 FPS lock, input handling, and scene management.
Uses terminal-based rendering with Sixel/Kitty graphics and ASCII fallback.
"""

import json
import time
import os
import sys
import select
import termios
import tty
from typing import Optional

from Source.Rendering.layer_manager import create_layer_manager, get_layer_manager
from Source.Rendering.terminal_detect import get_terminal_capability, RenderMode

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def load_config() -> dict:
    """Load game configuration from JSON file."""
    config_path = os.path.join(BASE_DIR, "Config", "config.json")
    with open(config_path, "r", encoding="utf-8") as f:
        return json.load(f)


def load_keybindings() -> dict:
    """Load keybindings from JSON file."""
    kb_path = os.path.join(BASE_DIR, "Config", "keybindings.json")
    with open(kb_path, "r", encoding="utf-8") as f:
        return json.load(f)


class InputHandler:
    """Handles keyboard input without external dependencies."""

    def __init__(self):
        self._old_settings = None
        self._key_buffer: list[str] = []

    def initialize(self) -> None:
        """Set up terminal for raw input."""
        if sys.stdin.isatty():
            fd = sys.stdin.fileno()
            self._old_settings = termios.tcgetattr(fd)
            tty.setraw(fd)

    def shutdown(self) -> None:
        """Restore terminal settings."""
        if self._old_settings and sys.stdin.isatty():
            fd = sys.stdin.fileno()
            termios.tcsetattr(fd, termios.TCSADRAIN, self._old_settings)

    def has_input(self) -> bool:
        """Check if input is available."""
        if sys.stdin.isatty():
            return select.select([sys.stdin], [], [], 0)[0] != []
        return False

    def read(self) -> str:
        """Read a key press and return key name."""
        if not sys.stdin.isatty():
            return ""

        char = sys.stdin.read(1)

        if char == "\x1b":
            if self.has_input():
                seq = char
                seq += sys.stdin.read(2) if self.has_input() else ""
                if seq == "\x1b[A":
                    return "TK_UP"
                elif seq == "\x1b[B":
                    return "TK_DOWN"
                elif seq == "\x1b[C":
                    return "TK_RIGHT"
                elif seq == "\x1b[D":
                    return "TK_LEFT"
                elif seq.startswith("\x1b["):
                    if len(seq) > 2:
                        code = seq[2:]
                        if code == "11~":
                            return "TK_F1"
                        elif code == "12~":
                            return "TK_F2"
                        elif code == "13~":
                            return "TK_F3"
                        elif code == "15~":
                            return "TK_F5"
                        elif code == "17~":
                            return "TK_F6"
                        elif code == "18~":
                            return "TK_F7"
                        elif code == "19~":
                            return "TK_F8"
                        elif code == "20~":
                            return "TK_F9"
                        elif code == "21~":
                            return "TK_F10"
            return "TK_ESCAPE"

        key_map = {
            "\r": "TK_RETURN",
            "\n": "TK_RETURN",
            " ": "TK_SPACE",
            "w": "TK_W", "W": "TK_W",
            "a": "TK_A", "A": "TK_A",
            "s": "TK_S", "S": "TK_S",
            "d": "TK_D", "D": "TK_D",
            "z": "TK_Z", "Z": "TK_Z",
            "x": "TK_X", "X": "TK_X",
            "i": "TK_I", "I": "TK_I",
            "q": "TK_Q", "Q": "TK_Q",
            "e": "TK_E", "E": "TK_E",
            "\x03": "TK_CLOSE",
            "\x04": "TK_CLOSE",
        }

        return key_map.get(char, f"TK_{ord(char)}" if char.isprintable() else "")


class GameEngine:
    """Heart of ALT_LAS. Runs game loop, delegates to scenes."""

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
        self._input_handler = InputHandler()
        self._layer_manager = None
        self._render_mode: Optional[RenderMode] = None

    def initialize(self) -> None:
        """Initialize terminal and rendering systems."""
        capability = get_terminal_capability()
        self._render_mode = capability.render_mode

        term_size = capability.get_terminal_size()
        self.width = min(self.width, term_size[0])
        self.height = min(self.height, term_size[1])

        self._layer_manager = create_layer_manager(self.width, self.height)
        self._layer_manager.initialize()
        self._input_handler.initialize()

        sys.stdout.write(f"\x1b]0;{self.title}\x07")
        sys.stdout.flush()

        self.is_running = True
        self._last_time = time.time()

    def set_state_manager(self, state_manager: "StateManager") -> None:
        """Set the state manager for scene management."""
        self.state_manager = state_manager

    def handle_input(self) -> None:
        """Process all pending input."""
        while self._input_handler.has_input():
            key = self._input_handler.read()

            if key == "TK_CLOSE":
                self.is_running = False
                return

            if key == self.keybindings["debug"]["toggle_debug"]:
                self.debug_mode = not self.debug_mode
                continue

            if self.state_manager and self.state_manager.current_scene:
                self.state_manager.current_scene.handle_input(key)

    def update(self) -> None:
        """Update game state."""
        now = time.time()
        self.delta_time = now - self._last_time
        self._last_time = now

        if self.state_manager and self.state_manager.current_scene:
            self.state_manager.current_scene.update(self.delta_time)

    def render(self) -> None:
        """Render the current scene."""
        self._layer_manager.clear_all()

        if self.state_manager and self.state_manager.current_scene:
            self.state_manager.current_scene.render()

        if self.debug_mode:
            fps_display = int(1.0 / self.delta_time) if self.delta_time > 0 else 0
            from Source.Rendering.layer_manager import Layer
            self._layer_manager.draw_text(
                0, 0,
                f"FPS:{fps_display} DT:{self.delta_time:.3f} Mode:{self._render_mode.value}",
                color="yellow", layer=Layer.UI
            )

        self._layer_manager.render()

    def run(self) -> None:
        """Main game loop."""
        self.initialize()
        try:
            while self.is_running:
                self.handle_input()
                self.update()
                self.render()
                time.sleep(1.0 / self.fps)
        except KeyboardInterrupt:
            pass
        finally:
            self.shutdown()

    def shutdown(self) -> None:
        """Clean up and restore terminal."""
        self.is_running = False
        if self._layer_manager:
            self._layer_manager.shutdown()
        self._input_handler.shutdown()
