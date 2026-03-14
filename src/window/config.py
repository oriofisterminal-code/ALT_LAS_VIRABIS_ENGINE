"""
ALT_LAS Engine - Window Configuration
Settings for window creation and display.
"""

from dataclasses import dataclass
from typing import Tuple, Optional


@dataclass
class WindowSettings:
    """Window creation settings."""
    
    # Window dimensions
    width: int = 960
    height: int = 544
    
    # Window properties
    title: str = "ALT_LAS Engine"
    fullscreen: bool = False
    resizable: bool = True
    borderless: bool = False
    
    # Display settings
    vsync: bool = True
    refresh_rate: int = 60  # Target refresh rate
    samples: int = 0  # MSAA samples (0, 2, 4, 8, 16)
    
    # OpenGL settings
    gl_version: Tuple[int, int] = (3, 3)
    double_buffer: bool = True
    depth_size: int = 24
    stencil_size: int = 8
    
    # Backend preference
    backend: str = "auto"  # "sdl2", "glfw", "pygame2", "auto"
    
    # Cursor
    cursor_visible: bool = True
    cursor_grabbed: bool = False
    
    # Position (for windowed mode)
    position: Optional[Tuple[int, int]] = None  # None = centered
    
    def to_dict(self) -> dict:
        """Convert to dictionary."""
        return {
            "width": self.width,
            "height": self.height,
            "title": self.title,
            "fullscreen": self.fullscreen,
            "resizable": self.resizable,
            "borderless": self.borderless,
            "vsync": self.vsync,
            "refresh_rate": self.refresh_rate,
            "samples": self.samples,
            "gl_version": list(self.gl_version),
            "double_buffer": self.double_buffer,
            "depth_size": self.depth_size,
            "stencil_size": self.stencil_size,
            "backend": self.backend,
            "cursor_visible": self.cursor_visible,
            "cursor_grabbed": self.cursor_grabbed,
            "position": list(self.position) if self.position else None,
        }
    
    @classmethod
    def from_dict(cls, data: dict) -> "WindowSettings":
        """Create from dictionary."""
        gl_ver = tuple(data.get("gl_version", [3, 3]))
        pos = data.get("position")
        
        return cls(
            width=data.get("width", 960),
            height=data.get("height", 544),
            title=data.get("title", "ALT_LAS Engine"),
            fullscreen=data.get("fullscreen", False),
            resizable=data.get("resizable", True),
            borderless=data.get("borderless", False),
            vsync=data.get("vsync", True),
            refresh_rate=data.get("refresh_rate", 60),
            samples=data.get("samples", 0),
            gl_version=tuple(gl_ver) if isinstance(gl_ver, list) else gl_ver,
            double_buffer=data.get("double_buffer", True),
            depth_size=data.get("depth_size", 24),
            stencil_size=data.get("stencil_size", 8),
            backend=data.get("backend", "auto"),
            cursor_visible=data.get("cursor_visible", True),
            cursor_grabbed=data.get("cursor_grabbed", False),
            position=tuple(pos) if isinstance(pos, list) else pos,
        )


# Default settings
DEFAULT_SETTINGS = WindowSettings()

# Preset configurations
PRESETS = {
    "default": WindowSettings(),
    
    "performance": WindowSettings(
        width=1280,
        height=720,
        vsync=False,
        samples=0,
    ),
    
    "quality": WindowSettings(
        width=1920,
        height=1080,
        vsync=True,
        samples=4,
    ),
    
    "low_res": WindowSettings(
        width=640,
        height=360,
        vsync=True,
        samples=0,
    ),
    
    "fullscreen": WindowSettings(
        width=1920,
        height=1080,
        fullscreen=True,
        vsync=True,
    ),
    
    "development": WindowSettings(
        width=960,
        height=544,
        resizable=True,
        vsync=False,  # For accurate FPS
        cursor_visible=True,
    ),
}
