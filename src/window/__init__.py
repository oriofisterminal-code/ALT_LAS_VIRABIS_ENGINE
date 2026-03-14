"""
ALT_LAS Engine - Window Module
Real window system with OpenGL context using moderngl-window.

This module provides a cross-platform window system that works on:
- Windows (SDL2, GLFW, Pygame)
- Linux (SDL2, GLFW, Pygame)
- macOS (SDL2, GLFW, Pygame)

Features:
- OpenGL 3.3+ context
- Keyboard and mouse input
- Window management (resize, fullscreen, vsync)
- Multi-backend support with automatic fallback
"""

from src.window.manager import (
    WindowManager,
    WindowBackend,
    WindowConfig,
    get_window_manager,
    create_window_manager,
)

from src.window.input import (
    WindowInputHandler,
    InputKey,
    get_window_input_handler,
)

from src.window.config import (
    WindowSettings,
    DEFAULT_SETTINGS,
    PRESETS,
)

__all__ = [
    # Window Manager
    'WindowManager',
    'WindowBackend',
    'WindowConfig',
    'get_window_manager',
    'create_window_manager',
    
    # Input
    'WindowInputHandler',
    'InputKey',
    'get_window_input_handler',
    
    # Config
    'WindowSettings',
    'DEFAULT_SETTINGS',
    'PRESETS',
]
