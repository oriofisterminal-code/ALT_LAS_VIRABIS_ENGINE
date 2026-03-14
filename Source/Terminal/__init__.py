"""
ALT_LAS Engine - Terminal Module
Custom terminal engine with GPU rendering support.
Provides headless OpenGL rendering and Kitty/Sixel output.
"""

from Source.Terminal.adapter import RenderAdapter, RenderBackend
from Source.Terminal.context import GLContext, create_headless_context
from Source.Terminal.input import NativeInputHandler

__all__ = [
    "RenderAdapter",
    "RenderBackend",
    "GLContext",
    "create_headless_context",
    "NativeInputHandler",
]
