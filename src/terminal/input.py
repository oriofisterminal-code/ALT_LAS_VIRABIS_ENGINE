"""
ALT_LAS Engine - Native Input Handler
Platform-specific input handling for custom terminal.
Supports both terminal raw mode and native window input.
"""

import sys
import os
import time
from typing import Optional, Set
from enum import Enum, auto

# Platform-specific imports
if sys.platform == 'win32':
    import msvcrt
    HAS_SELECT = False
else:
    import select
    HAS_SELECT = True


class InputKey(Enum):
    """Normalized input keys."""
    UP = auto()
    DOWN = auto()
    LEFT = auto()
    RIGHT = auto()
    ENTER = auto()
    ESCAPE = auto()
    SPACE = auto()
    W = auto()
    A = auto()
    S = auto()
    D = auto()
    Z = auto()
    X = auto()
    Q = auto()
    E = auto()
    I = auto()
    F1 = auto()
    F2 = auto()
    F3 = auto()
    F5 = auto()
    F6 = auto()
    F7 = auto()
    F8 = auto()
    F9 = auto()
    F10 = auto()
    CLOSE = auto()
    UNKNOWN = auto()


class NativeInputHandler:
    """Handles keyboard input with multi-platform support."""

    def __init__(self):
        self._old_settings = None
        self._key_states: Set[InputKey] = set()
        self._key_just_pressed: Set[InputKey] = set()
        self._key_just_released: Set[InputKey] = set()
        self._initialized = False
        self._window = None  # For SDL2/GLFW window reference

    def initialize(self) -> bool:
        """Initialize terminal for raw input."""
        if self._initialized:
            return True

        if sys.platform == 'win32':
            # Windows: No special terminal setup needed for msvcrt
            self._initialized = True
            return True

        # Unix/Linux: Set up raw terminal mode
        if sys.stdin.isatty():
            try:
                import termios
                import tty
                fd = sys.stdin.fileno()
                self._old_settings = termios.tcgetattr(fd)
                tty.setraw(fd)
            except Exception:
                return False

        self._initialized = True
        return True

    def shutdown(self) -> None:
        """Restore terminal settings."""
        if sys.platform == 'win32':
            # Windows: No special cleanup needed
            self._initialized = False
            return

        # Unix/Linux: Restore terminal settings
        if self._old_settings and sys.stdin.isatty():
            try:
                import termios
                fd = sys.stdin.fileno()
                termios.tcsetattr(fd, termios.TCSADRAIN, self._old_settings)
            except Exception:
                pass
        self._initialized = False

    def set_window(self, window) -> None:
        """Set window reference for native input polling."""
        self._window = window

    def poll(self) -> None:
        """Poll for input events. Call once per frame."""
        # Clear just pressed/released from last frame
        self._key_just_pressed.clear()
        self._key_just_released.clear()

        if self._window:
            # Native window input (SDL2/GLFW)
            self._poll_window()
        else:
            # Terminal input
            self._poll_terminal()

    def _poll_terminal(self) -> None:
        """Poll terminal for input."""
        while self._has_input():
            key = self._read_terminal_key()
            if key != InputKey.UNKNOWN:
                self._key_states.add(key)
                self._key_just_pressed.add(key)

        # Release keys that weren't pressed this frame
        # (Terminal doesn't have key release events, so we emulate)
        # Keys stay pressed until explicitly released

    def _poll_window(self) -> None:
        """Poll native window for input."""
        # This would be implemented with SDL2 or GLFW bindings
        # For now, fallback to terminal input
        self._poll_terminal()

    def _has_input(self) -> bool:
        """Check if input is available."""
        if sys.platform == 'win32':
            # Windows: Use msvcrt.kbhit()
            return msvcrt.kbhit() != 0

        # Unix/Linux: Use select
        if sys.stdin.isatty():
            return select.select([sys.stdin], [], [], 0)[0] != []
        return False

    def _read_terminal_key(self) -> InputKey:
        """Read a key from terminal and normalize."""
        if sys.platform == 'win32':
            return self._read_windows_key()

        if not sys.stdin.isatty():
            return InputKey.UNKNOWN

        char = sys.stdin.read(1)

        # Escape sequences
        if char == "\x1b":
            if self._has_input():
                seq = char
                seq += sys.stdin.read(2) if self._has_input() else ""

                escape_map = {
                    "\x1b[A": InputKey.UP,
                    "\x1b[B": InputKey.DOWN,
                    "\x1b[C": InputKey.RIGHT,
                    "\x1b[D": InputKey.LEFT,
                }

                if seq in escape_map:
                    return escape_map[seq]

                # Function keys
                if seq.startswith("\x1b["):
                    code = seq[2:]
                    fn_map = {
                        "11~": InputKey.F1,
                        "12~": InputKey.F2,
                        "13~": InputKey.F3,
                        "15~": InputKey.F5,
                        "17~": InputKey.F6,
                        "18~": InputKey.F7,
                        "19~": InputKey.F8,
                        "20~": InputKey.F9,
                        "21~": InputKey.F10,
                    }
                    if code in fn_map:
                        return fn_map[code]

            return InputKey.ESCAPE

        # Single character keys
        char_map = {
            "\r": InputKey.ENTER,
            "\n": InputKey.ENTER,
            " ": InputKey.SPACE,
            "w": InputKey.W, "W": InputKey.W,
            "a": InputKey.A, "A": InputKey.A,
            "s": InputKey.S, "S": InputKey.S,
            "d": InputKey.D, "D": InputKey.D,
            "z": InputKey.Z, "Z": InputKey.Z,
            "x": InputKey.X, "X": InputKey.X,
            "q": InputKey.Q, "Q": InputKey.Q,
            "e": InputKey.E, "E": InputKey.E,
            "i": InputKey.I, "I": InputKey.I,
            "\x03": InputKey.CLOSE,  # Ctrl+C
            "\x04": InputKey.CLOSE,  # Ctrl+D
        }

        return char_map.get(char, InputKey.UNKNOWN)

    def _read_windows_key(self) -> InputKey:
        """Read a key on Windows using msvcrt."""
        if not msvcrt.kbhit():
            return InputKey.UNKNOWN

        char = msvcrt.getch()

        # Special keys (arrow keys, function keys)
        if char == b'\x00' or char == b'\xe0':
            if msvcrt.kbhit():
                special = msvcrt.getch()
                # Windows arrow key codes
                special_map = {
                    b'H': InputKey.UP,
                    b'P': InputKey.DOWN,
                    b'M': InputKey.RIGHT,
                    b'K': InputKey.LEFT,
                    b';': InputKey.F1,      # F1
                    b'<': InputKey.F2,      # F2
                    b'=': InputKey.F3,      # F3
                    b'?': InputKey.F5,      # F5
                    b'@': InputKey.F6,      # F6
                    b'A': InputKey.F7,      # F7
                    b'B': InputKey.F8,      # F8
                    b'C': InputKey.F9,      # F9
                    b'D': InputKey.F10,     # F10
                }
                return special_map.get(special, InputKey.UNKNOWN)
            return InputKey.UNKNOWN

        # Regular keys
        try:
            decoded = char.decode('utf-8')
        except UnicodeDecodeError:
            return InputKey.UNKNOWN

        char_map = {
            '\r': InputKey.ENTER,
            '\n': InputKey.ENTER,
            ' ': InputKey.SPACE,
            'w': InputKey.W, 'W': InputKey.W,
            'a': InputKey.A, 'A': InputKey.A,
            's': InputKey.S, 'S': InputKey.S,
            'd': InputKey.D, 'D': InputKey.D,
            'z': InputKey.Z, 'Z': InputKey.Z,
            'x': InputKey.X, 'X': InputKey.X,
            'q': InputKey.Q, 'Q': InputKey.Q,
            'e': InputKey.E, 'E': InputKey.E,
            'i': InputKey.I, 'I': InputKey.I,
            '\x1b': InputKey.ESCAPE,
            '\x03': InputKey.CLOSE,  # Ctrl+C
        }

        return char_map.get(decoded, InputKey.UNKNOWN)

    def is_key_down(self, key: InputKey) -> bool:
        """Check if key is currently held down."""
        return key in self._key_states

    def is_key_just_pressed(self, key: InputKey) -> bool:
        """Check if key was just pressed this frame."""
        return key in self._key_just_pressed

    def is_key_just_released(self, key: InputKey) -> bool:
        """Check if key was just released this frame."""
        return key in self._key_just_released

    def release_key(self, key: InputKey) -> None:
        """Manually release a key (for terminal mode)."""
        if key in self._key_states:
            self._key_states.remove(key)
            self._key_just_released.add(key)

    def release_all(self) -> None:
        """Release all keys."""
        for key in list(self._key_states):
            self._key_just_released.add(key)
        self._key_states.clear()

    def get_pressed_keys(self) -> Set[InputKey]:
        """Get all currently pressed keys."""
        return self._key_states.copy()

    def get_legacy_key_name(self, key: InputKey) -> str:
        """Convert to legacy key name for compatibility."""
        legacy_map = {
            InputKey.UP: "TK_UP",
            InputKey.DOWN: "TK_DOWN",
            InputKey.LEFT: "TK_LEFT",
            InputKey.RIGHT: "TK_RIGHT",
            InputKey.ENTER: "TK_RETURN",
            InputKey.ESCAPE: "TK_ESCAPE",
            InputKey.SPACE: "TK_SPACE",
            InputKey.W: "TK_W",
            InputKey.A: "TK_A",
            InputKey.S: "TK_S",
            InputKey.D: "TK_D",
            InputKey.Z: "TK_Z",
            InputKey.X: "TK_X",
            InputKey.Q: "TK_Q",
            InputKey.E: "TK_E",
            InputKey.I: "TK_I",
            InputKey.F1: "TK_F1",
            InputKey.F2: "TK_F2",
            InputKey.F3: "TK_F3",
            InputKey.F5: "TK_F5",
            InputKey.F6: "TK_F6",
            InputKey.F7: "TK_F7",
            InputKey.F8: "TK_F8",
            InputKey.F9: "TK_F9",
            InputKey.F10: "TK_F10",
            InputKey.CLOSE: "TK_CLOSE",
        }
        return legacy_map.get(key, "TK_UNKNOWN")
