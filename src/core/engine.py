"""
ALT_LAS Engine - Core Game Engine
Main game loop with dual mode support:
- Terminal Mode: ASCII/ANSI rendering in terminal
- Window Mode: OpenGL rendering in real window

Usage:
    # Terminal mode (default)
    python main.py
    
    # Window mode
    python main_window.py
"""

import json
import time
import os
import sys
from typing import Optional, TYPE_CHECKING
from enum import Enum, auto

# Platform-specific imports for terminal mode
if sys.platform == 'win32':
    import msvcrt
    HAS_TERMIOS = False
else:
    import select
    import termios
    import tty
    HAS_TERMIOS = True

from src.render.layers import create_layer_manager, get_layer_manager
from src.render.terminal_detect import get_terminal_capability, RenderMode

if TYPE_CHECKING:
    from src.core.state import StateManager

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


class EngineMode(Enum):
    """Available engine modes."""
    TERMINAL = auto()  # Terminal-based ASCII rendering
    WINDOW = auto()    # Real window with OpenGL


def load_config() -> dict:
    """Load game configuration from JSON file."""
    config_path = os.path.join(BASE_DIR, "config", "settings.json")
    with open(config_path, "r", encoding="utf-8") as f:
        return json.load(f)


def load_keybindings() -> dict:
    """Load keybindings from JSON file."""
    kb_path = os.path.join(BASE_DIR, "config", "keys.json")
    with open(kb_path, "r", encoding="utf-8") as f:
        return json.load(f)


class TerminalInputHandler:
    """Handles keyboard input for terminal mode."""

    def __init__(self):
        self._old_settings = None
        self._key_buffer: list[str] = []

    def initialize(self) -> None:
        """Set up terminal for raw input."""
        if sys.platform == 'win32':
            return
        if sys.stdin.isatty():
            fd = sys.stdin.fileno()
            self._old_settings = termios.tcgetattr(fd)
            tty.setraw(fd)

    def shutdown(self) -> None:
        """Restore terminal settings."""
        if sys.platform == 'win32':
            return
        if self._old_settings and sys.stdin.isatty():
            fd = sys.stdin.fileno()
            termios.tcsetattr(fd, termios.TCSADRAIN, self._old_settings)

    def has_input(self) -> bool:
        """Check if input is available."""
        if sys.platform == 'win32':
            return msvcrt.kbhit() != 0
        if sys.stdin.isatty():
            return select.select([sys.stdin], [], [], 0)[0] != []
        return False

    def read(self) -> str:
        """Read a key press and return key name."""
        if sys.platform == 'win32':
            return self._read_windows()

        if not sys.stdin.isatty():
            return ""

        char = sys.stdin.read(1)

        if char == "\x1b":
            if self.has_input():
                seq = char
                seq += sys.stdin.read(2) if self.has_input() else ""
                if seq == "\x1b[A": return "TK_UP"
                elif seq == "\x1b[B": return "TK_DOWN"
                elif seq == "\x1b[C": return "TK_RIGHT"
                elif seq == "\x1b[D": return "TK_LEFT"
            return "TK_ESCAPE"

        key_map = {
            "\r": "TK_RETURN", "\n": "TK_RETURN", " ": "TK_SPACE",
            "w": "TK_W", "W": "TK_W", "a": "TK_A", "A": "TK_A",
            "s": "TK_S", "S": "TK_S", "d": "TK_D", "D": "TK_D",
            "z": "TK_Z", "Z": "TK_Z", "x": "TK_X", "X": "TK_X",
            "i": "TK_I", "I": "TK_I", "q": "TK_Q", "Q": "TK_Q",
            "e": "TK_E", "E": "TK_E", "\x03": "TK_CLOSE", "\x04": "TK_CLOSE",
        }
        return key_map.get(char, f"TK_{ord(char)}" if char.isprintable() else "")

    def _read_windows(self) -> str:
        """Read a key on Windows using msvcrt."""
        if not msvcrt.kbhit():
            return ""
        char = msvcrt.getch()

        if char == b'\x00' or char == b'\xe0':
            if msvcrt.kbhit():
                special = msvcrt.getch()
                special_map = {
                    b'H': "TK_UP", b'P': "TK_DOWN",
                    b'M': "TK_RIGHT", b'K': "TK_LEFT",
                    b';': "TK_F1", b'<': "TK_F2", b'=': "TK_F3",
                }
                return special_map.get(special, "TK_UNKNOWN")
            return "TK_UNKNOWN"

        try:
            decoded = char.decode('utf-8')
        except UnicodeDecodeError:
            return "TK_UNKNOWN"

        key_map = {
            '\r': "TK_RETURN", '\n': "TK_RETURN", ' ': "TK_SPACE",
            'w': "TK_W", 'W': "TK_W", 'a': "TK_A", 'A': "TK_A",
            's': "TK_S", 'S': "TK_S", 'd': "TK_D", 'D': "TK_D",
            'z': "TK_Z", 'Z': "TK_Z", 'x': "TK_X", 'X': "TK_X",
            'i': "TK_I", 'I': "TK_I", 'q': "TK_Q", 'Q': "TK_Q",
            'e': "TK_E", 'E': "TK_E", '\x1b': "TK_ESCAPE", '\x03': "TK_CLOSE",
        }
        return key_map.get(decoded, f"TK_{ord(decoded)}" if decoded.isprintable() else "TK_UNKNOWN")


class GameEngine:
    """
    ALT_LAS Engine - Dual Mode Game Engine.
    
    Supports:
    - Terminal Mode: ASCII/ANSI rendering (default)
    - Window Mode: OpenGL rendering (via main_window.py)
    """

    def __init__(self, mode: EngineMode = EngineMode.TERMINAL):
        self.mode = mode
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
        self._render_mode: Optional[RenderMode] = None
        
        # Mode-specific components
        self._input_handler = None
        self._layer_manager = None
        self._window_manager = None

    def initialize(self) -> None:
        """Initialize engine systems based on mode."""
        if self.mode == EngineMode.TERMINAL:
            self._init_terminal_mode()
        else:
            self._init_window_mode()
        
        self.is_running = True
        self._last_time = time.time()

    def _init_terminal_mode(self) -> None:
        """Initialize terminal-based rendering."""
        print("[Engine] Initializing Terminal Mode...")
        
        capability = get_terminal_capability()
        self._render_mode = capability.render_mode

        term_size = capability.get_terminal_size()
        self.width = min(self.width, term_size[0])
        self.height = min(self.height, term_size[1])

        self._layer_manager = create_layer_manager(self.width, self.height)
        self._layer_manager.initialize()
        
        self._input_handler = TerminalInputHandler()
        self._input_handler.initialize()

        sys.stdout.write(f"\x1b]0;{self.title}\x07")
        sys.stdout.flush()
        
        print(f"[Engine] Terminal: {self.width}x{self.height}, Mode: {self._render_mode.value}")

    def _init_window_mode(self) -> None:
        """Initialize window-based rendering."""
        print("[Engine] Initializing Window Mode...")
        
        try:
            from src.window.manager import get_window_manager
            from src.window.input import get_window_input_handler
            
            self._window_manager = get_window_manager()
            if not self._window_manager.initialize():
                print("[Engine] Window init failed, falling back to terminal")
                self.mode = EngineMode.TERMINAL
                self._init_terminal_mode()
                return
            
            self.width = self._window_manager.width
            self.height = self._window_manager.height
            
            self._input_handler = get_window_input_handler()
            self._input_handler.initialize()
            
            print(f"[Engine] Window: {self.width}x{self.height}")
            
        except ImportError as e:
            print(f"[Engine] Window modules not available: {e}")
            print("[Engine] Falling back to terminal mode")
            self.mode = EngineMode.TERMINAL
            self._init_terminal_mode()

    def set_state_manager(self, state_manager: "StateManager") -> None:
        """Set the state manager for scene management."""
        self.state_manager = state_manager

    def handle_input(self) -> None:
        """Process all pending input."""
        if self.mode == EngineMode.WINDOW and self._window_manager:
            self._handle_window_input()
        else:
            self._handle_terminal_input()

    def _handle_terminal_input(self) -> None:
        """Process terminal input."""
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

    def _handle_window_input(self) -> None:
        """Process window input."""
        from src.window.input import InputKey
        
        self._input_handler.poll()
        
        if self._input_handler.is_key_just_pressed(InputKey.ESCAPE):
            self.is_running = False
            return
        
        if self._input_handler.is_key_just_pressed(InputKey.F1):
            self.debug_mode = not self.debug_mode
        
        # Forward to scene
        if self.state_manager and self.state_manager.current_scene:
            for key in self._input_handler.get_pressed_keys():
                key_name = self._input_handler.get_legacy_key_name(key)
                self.state_manager.current_scene.handle_input(key_name)

    def update(self) -> None:
        """Update game state."""
        now = time.time()
        self.delta_time = now - self._last_time
        self._last_time = now

        if self.state_manager and self.state_manager.current_scene:
            self.state_manager.current_scene.update(self.delta_time)

    def render(self) -> None:
        """Render the current scene."""
        if self.mode == EngineMode.WINDOW and self._window_manager:
            self._render_window()
        else:
            self._render_terminal()

    def _render_terminal(self) -> None:
        """Render to terminal."""
        self._layer_manager.clear_all()

        if self.state_manager and self.state_manager.current_scene:
            self.state_manager.current_scene.render()

        if self.debug_mode:
            fps_display = int(1.0 / self.delta_time) if self.delta_time > 0 else 0
            from src.render.layers import Layer
            self._layer_manager.draw_text(
                0, 0,
                f"FPS:{fps_display} DT:{self.delta_time:.3f} Mode:{self._render_mode.value}",
                color="yellow", layer=Layer.UI
            )

        self._layer_manager.render()

    def _render_window(self) -> None:
        """Render to window."""
        self._window_manager.clear(0.0, 0.0, 0.05, 1.0)
        
        if self.state_manager and self.state_manager.current_scene:
            self.state_manager.current_scene.render()
        
        if self.debug_mode:
            fps = int(1.0 / self.delta_time) if self.delta_time > 0 else 0
            self._window_manager.set_title(f"ALT_LAS Engine - FPS: {fps}")
        
        self._window_manager.swap_buffers()

    def run(self) -> None:
        """Main game loop."""
        self.initialize()
        try:
            while self.is_running:
                # Window mode: check for close
                if self.mode == EngineMode.WINDOW and self._window_manager:
                    if self._window_manager.should_close:
                        break
                
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
        
        if self.mode == EngineMode.WINDOW and self._window_manager:
            self._window_manager.shutdown()
        elif self._layer_manager:
            self._layer_manager.shutdown()
        
        if self._input_handler:
            self._input_handler.shutdown()
