"""
ALT_LAS Engine - Window Manager
Real window system using moderngl-window.
Supports SDL2, GLFW, Pygame backends with automatic fallback.
"""

import sys
import os
from typing import Optional, Callable, Dict, Any
from enum import Enum, auto

# Try to import moderngl_window
try:
    import moderngl_window as mglw
    from moderngl_window import WindowConfig, get_local_window_cls
    from moderngl_window.context.base import BaseWindow
    MGLW_AVAILABLE = True
except ImportError:
    MGLW_AVAILABLE = False
    mglw = None
    WindowConfig = object
    BaseWindow = object

# Try to import moderngl
try:
    import moderngl
    MODERNGL_AVAILABLE = True
except ImportError:
    MODERNGL_AVAILABLE = False
    moderngl = None


class WindowBackend(Enum):
    """Available window backends."""
    SDL2 = auto()
    GLFW = auto()
    PYGAME = auto()
    HEADLESS = auto()


class WindowConfig:
    """Window configuration settings."""
    
    def __init__(self):
        self.title = "ALT_LAS Engine"
        self.width = 960
        self.height = 544
        self.fullscreen = False
        self.resizable = True
        self.vsync = True
        self.gl_version = (3, 3)
        self.samples = 0  # MSAA samples (0 = disabled)


class WindowManager:
    """
    Manages real window with OpenGL context.
    Uses moderngl-window for cross-platform support.
    """
    
    # Backend priority (try in order)
    BACKEND_PRIORITY = [
        WindowBackend.SDL2,
        WindowBackend.GLFW,
        WindowBackend.PYGAME,
        WindowBackend.HEADLESS,
    ]
    
    def __init__(self, config: Optional[WindowConfig] = None):
        self.config = config or WindowConfig()
        self._window: Optional[Any] = None
        self._ctx: Optional[Any] = None
        self._backend: Optional[WindowBackend] = None
        self._initialized = False
        self._should_close = False
        
        # Input state
        self._keys_pressed: Dict[str, bool] = {}
        self._mouse_pos = (0, 0)
        self._mouse_buttons: Dict[int, bool] = {}
        
        # Callbacks
        self._on_key_press: Optional[Callable] = None
        self._on_key_release: Optional[Callable] = None
        self._on_mouse_press: Optional[Callable] = None
        self._on_mouse_release: Optional[Callable] = None
        self._on_resize: Optional[Callable] = None
    
    @property
    def is_available(self) -> bool:
        """Check if window system is available."""
        return MGLW_AVAILABLE and MODERNGL_AVAILABLE
    
    @property
    def width(self) -> int:
        """Get window width."""
        if self._window:
            return self._window.size[0]
        return self.config.width
    
    @property
    def height(self) -> int:
        """Get window height."""
        if self._window:
            return self._window.size[1]
        return self.config.height
    
    @property
    def ctx(self):
        """Get OpenGL context."""
        return self._ctx
    
    @property
    def backend(self) -> Optional[WindowBackend]:
        """Get current backend."""
        return self._backend
    
    @property
    def should_close(self) -> bool:
        """Check if window should close."""
        return self._should_close
    
    def initialize(self) -> bool:
        """Initialize window with best available backend."""
        if self._initialized:
            return True
        
        if not self.is_available:
            print("[Window] moderngl-window not available, using headless mode")
            return self._init_headless()
        
        # Try each backend in priority order
        for backend in self.BACKEND_PRIORITY:
            if backend == WindowBackend.HEADLESS:
                continue  # Try this last
            
            if self._try_backend(backend):
                self._backend = backend
                self._initialized = True
                print(f"[Window] Initialized {backend.name} backend ({self.width}x{self.height})")
                return True
        
        # Fallback to headless
        return self._init_headless()
    
    def _try_backend(self, backend: WindowBackend) -> bool:
        """Try to initialize a specific backend."""
        try:
            backend_name = {
                WindowBackend.SDL2: 'sdl2',
                WindowBackend.GLFW: 'glfw',
                WindowBackend.PYGAME: 'pygame2',
            }.get(backend)
            
            if not backend_name:
                return False
            
            # Create window using moderngl-window
            window_cls = get_local_window_cls(backend_name)
            
            self._window = window_cls(
                title=self.config.title,
                size=(self.config.width, self.config.height),
                fullscreen=self.config.fullscreen,
                resizable=self.config.resizable,
                vsync=self.config.vsync,
                gl_version=self.config.gl_version,
                samples=self.config.samples,
            )
            
            # Get context
            self._ctx = self._window.ctx
            
            # Set up input callbacks
            self._window.key_event_func = self._handle_key_event
            self._window.mouse_position_event_func = self._handle_mouse_position
            self._window.mouse_press_event_func = self._handle_mouse_press
            self._window.resize_func = self._handle_resize
            self._window.close_func = self._handle_close
            
            return True
            
        except Exception as e:
            print(f"[Window] Backend {backend.name} failed: {e}")
            return False
    
    def _init_headless(self) -> bool:
        """Initialize headless context (no window)."""
        try:
            if MODERNGL_AVAILABLE:
                self._ctx = moderngl.create_context(standalone=True)
                self._backend = WindowBackend.HEADLESS
                self._initialized = True
                print("[Window] Headless mode initialized")
                return True
        except Exception as e:
            print(f"[Window] Headless mode failed: {e}")
        return False
    
    def _handle_key_event(self, key, action, modifiers):
        """Handle keyboard events."""
        key_name = self._normalize_key_name(key)
        
        if action == 'PRESS':
            self._keys_pressed[key_name] = True
            if self._on_key_press:
                self._on_key_press(key_name, modifiers)
        elif action == 'RELEASE':
            self._keys_pressed[key_name] = False
            if self._on_key_release:
                self._on_key_release(key_name, modifiers)
    
    def _handle_mouse_position(self, x, y):
        """Handle mouse position events."""
        self._mouse_pos = (x, y)
    
    def _handle_mouse_press(self, x, y, button):
        """Handle mouse button press."""
        self._mouse_buttons[button] = True
        if self._on_mouse_press:
            self._on_mouse_press(x, y, button)
    
    def _handle_mouse_release(self, x, y, button):
        """Handle mouse button release."""
        self._mouse_buttons[button] = False
        if self._on_mouse_release:
            self._on_mouse_release(x, y, button)
    
    def _handle_resize(self, width, height):
        """Handle window resize."""
        self.config.width = width
        self.config.height = height
        if self._on_resize:
            self._on_resize(width, height)
    
    def _handle_close(self):
        """Handle window close request."""
        self._should_close = True
    
    def _normalize_key_name(self, key) -> str:
        """Normalize key name to engine format."""
        # Map common keys to TK_ format
        key_map = {
            'UP': 'TK_UP',
            'DOWN': 'TK_DOWN',
            'LEFT': 'TK_LEFT',
            'RIGHT': 'TK_RIGHT',
            'ENTER': 'TK_RETURN',
            'RETURN': 'TK_RETURN',
            'ESCAPE': 'TK_ESCAPE',
            'SPACE': 'TK_SPACE',
            'W': 'TK_W',
            'A': 'TK_A',
            'S': 'TK_S',
            'D': 'TK_D',
            'Z': 'TK_Z',
            'X': 'TK_X',
            'Q': 'TK_Q',
            'E': 'TK_E',
            'I': 'TK_I',
            'F1': 'TK_F1',
            'F2': 'TK_F2',
            'F3': 'TK_F3',
            'F5': 'TK_F5',
            'F6': 'TK_F6',
            'F7': 'TK_F7',
            'F8': 'TK_F8',
            'F9': 'TK_F9',
            'F10': 'TK_F10',
        }
        
        key_str = str(key).upper().replace('KEY.', '')
        return key_map.get(key_str, f'TK_{key_str}')
    
    def poll_events(self) -> None:
        """Poll for window events."""
        if self._window:
            self._window.poll_events()
    
    def swap_buffers(self) -> None:
        """Swap front and back buffers."""
        if self._window:
            self._window.swap_buffers()
    
    def clear(self, r=0.0, g=0.0, b=0.05, a=1.0) -> None:
        """Clear the screen."""
        if self._ctx:
            self._ctx.clear(r, g, b, a)
    
    def is_key_pressed(self, key: str) -> bool:
        """Check if a key is currently pressed."""
        return self._keys_pressed.get(key, False)
    
    def get_mouse_position(self) -> tuple:
        """Get current mouse position."""
        return self._mouse_pos
    
    def is_mouse_button_pressed(self, button: int) -> bool:
        """Check if mouse button is pressed (0=left, 1=right, 2=middle)."""
        return self._mouse_buttons.get(button, False)
    
    def set_title(self, title: str) -> None:
        """Set window title."""
        if self._window:
            self._window.title = title
    
    def set_vsync(self, vsync: bool) -> None:
        """Set vsync mode."""
        if self._window:
            self._window.vsync = vsync
    
    def set_fullscreen(self, fullscreen: bool) -> None:
        """Set fullscreen mode."""
        if self._window:
            self._window.fullscreen = fullscreen
    
    def set_callbacks(self, 
                      on_key_press: Callable = None,
                      on_key_release: Callable = None,
                      on_mouse_press: Callable = None,
                      on_mouse_release: Callable = None,
                      on_resize: Callable = None) -> None:
        """Set event callbacks."""
        self._on_key_press = on_key_press
        self._on_key_release = on_key_release
        self._on_mouse_press = on_mouse_press
        self._on_mouse_release = on_mouse_release
        self._on_resize = on_resize
    
    def close(self) -> None:
        """Request window close."""
        self._should_close = True
        if self._window:
            self._window.close = True
    
    def shutdown(self) -> None:
        """Release window resources."""
        if self._window:
            try:
                self._window.destroy()
            except Exception:
                pass
        self._window = None
        self._ctx = None
        self._initialized = False


# Global window manager instance
_window_manager: Optional[WindowManager] = None


def get_window_manager() -> WindowManager:
    """Get or create global window manager."""
    global _window_manager
    if _window_manager is None:
        _window_manager = WindowManager()
    return _window_manager


def create_window_manager(config: WindowConfig = None) -> WindowManager:
    """Create a new window manager with specified config."""
    global _window_manager
    _window_manager = WindowManager(config)
    return _window_manager
