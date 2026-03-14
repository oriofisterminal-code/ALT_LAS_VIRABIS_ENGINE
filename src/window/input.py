"""
ALT_LAS Engine - Window Input Adapter
Bridges moderngl-window input to engine's InputHandler interface.
"""

from typing import Set, Dict, Optional
from enum import Enum, auto


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


class WindowInputHandler:
    """
    Input handler that uses real window events.
    Works with SDL2, GLFW, Pygame backends via moderngl-window.
    """
    
    # TK_ format to InputKey mapping
    KEY_MAP = {
        "TK_UP": InputKey.UP,
        "TK_DOWN": InputKey.DOWN,
        "TK_LEFT": InputKey.LEFT,
        "TK_RIGHT": InputKey.RIGHT,
        "TK_RETURN": InputKey.ENTER,
        "TK_ESCAPE": InputKey.ESCAPE,
        "TK_SPACE": InputKey.SPACE,
        "TK_W": InputKey.W,
        "TK_A": InputKey.A,
        "TK_S": InputKey.S,
        "TK_D": InputKey.D,
        "TK_Z": InputKey.Z,
        "TK_X": InputKey.X,
        "TK_Q": InputKey.Q,
        "TK_E": InputKey.E,
        "TK_I": InputKey.I,
        "TK_F1": InputKey.F1,
        "TK_F2": InputKey.F2,
        "TK_F3": InputKey.F3,
        "TK_F5": InputKey.F5,
        "TK_F6": InputKey.F6,
        "TK_F7": InputKey.F7,
        "TK_F8": InputKey.F8,
        "TK_F9": InputKey.F9,
        "TK_F10": InputKey.F10,
        "TK_CLOSE": InputKey.CLOSE,
    }
    
    def __init__(self):
        self._key_states: Set[InputKey] = set()
        self._key_just_pressed: Set[InputKey] = set()
        self._key_just_released: Set[InputKey] = set()
        self._initialized = False
        self._window_manager = None
    
    def initialize(self) -> bool:
        """Initialize input handler."""
        if self._initialized:
            return True
        
        try:
            from src.window.manager import get_window_manager
            self._window_manager = get_window_manager()
            
            # Set up callbacks
            self._window_manager.set_callbacks(
                on_key_press=self._on_key_press,
                on_key_release=self._on_key_release,
            )
            
            self._initialized = True
            return True
        except Exception as e:
            print(f"[Input] Failed to initialize: {e}")
            return False
    
    def _on_key_press(self, key_name: str, modifiers) -> None:
        """Handle key press from window."""
        key = self.KEY_MAP.get(key_name, InputKey.UNKNOWN)
        if key != InputKey.UNKNOWN:
            self._key_states.add(key)
            self._key_just_pressed.add(key)
    
    def _on_key_release(self, key_name: str, modifiers) -> None:
        """Handle key release from window."""
        key = self.KEY_MAP.get(key_name, InputKey.UNKNOWN)
        if key != InputKey.UNKNOWN:
            self._key_states.discard(key)
            self._key_just_released.add(key)
    
    def poll(self) -> None:
        """Poll for events and update state. Call once per frame."""
        # Clear just pressed/released from last frame
        self._key_just_pressed.clear()
        self._key_just_released.clear()
        
        if self._window_manager:
            try:
                self._window_manager.poll_events()
                
                # Check for close
                if self._window_manager.should_close:
                    self._key_states.add(InputKey.CLOSE)
                    self._key_just_pressed.add(InputKey.CLOSE)
            except Exception:
                pass
    
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
        """Manually release a key."""
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
    
    def shutdown(self) -> None:
        """Shutdown input handler."""
        self._initialized = False
        self._key_states.clear()
        self._key_just_pressed.clear()
        self._key_just_released.clear()


# Global instance
_window_input_handler: Optional[WindowInputHandler] = None


def get_window_input_handler() -> WindowInputHandler:
    """Get or create global window input handler."""
    global _window_input_handler
    if _window_input_handler is None:
        _window_input_handler = WindowInputHandler()
    return _window_input_handler
