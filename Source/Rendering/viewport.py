"""
ALT_LAS Engine - Viewport / Camera System
Handles camera following, smooth scrolling, and world-to-screen coordinate mapping.
Works with both graphics and ASCII rendering modes.
"""

from typing import Optional, Tuple
from Source.Rendering.layer_manager import LayerManager, get_layer_manager


class Camera:
    """Tracks a target position and provides world-to-screen offset."""

    def __init__(self, view_width: int, view_height: int):
        self.view_width = view_width
        self.view_height = view_height
        self.x = 0.0
        self.y = 0.0
        self.target_x = 0.0
        self.target_y = 0.0
        self.smoothing = 0.15
        self._bounds: Optional[Tuple[int, int]] = None

    def set_bounds(self, map_width: int, map_height: int) -> None:
        """Set map boundaries for camera clamping."""
        self._bounds = (map_width, map_height)

    def follow(self, target_x: int, target_y: int) -> None:
        """Set camera to follow a target position."""
        self.target_x = float(target_x) - self.view_width // 2
        self.target_y = float(target_y) - self.view_height // 2

    def update(self, dt: float) -> None:
        """Update camera position with smooth interpolation."""
        self.x += (self.target_x - self.x) * self.smoothing
        self.y += (self.target_y - self.y) * self.smoothing

        if self._bounds:
            mw, mh = self._bounds
            max_x = max(0, mw - self.view_width)
            max_y = max(0, mh - self.view_height)
            self.x = max(0, min(self.x, max_x))
            self.y = max(0, min(self.y, max_y))

    def world_to_screen(self, wx: int, wy: int) -> Tuple[int, int]:
        """Convert world coordinates to screen coordinates."""
        sx = wx - int(self.x)
        sy = wy - int(self.y)
        return sx, sy

    def screen_to_world(self, sx: int, sy: int) -> Tuple[int, int]:
        """Convert screen coordinates to world coordinates."""
        wx = sx + int(self.x)
        wy = sy + int(self.y)
        return wx, wy

    def is_visible(self, wx: int, wy: int) -> bool:
        """Check if a world position is visible on screen."""
        sx, sy = self.world_to_screen(wx, wy)
        return 0 <= sx < self.view_width and 0 <= sy < self.view_height

    def snap_to_target(self) -> None:
        """Instantly snap to target position."""
        self.x = self.target_x
        self.y = self.target_y

        if self._bounds:
            mw, mh = self._bounds
            max_x = max(0, mw - self.view_width)
            max_y = max(0, mh - self.view_height)
            self.x = max(0, min(self.x, max_x))
            self.y = max(0, min(self.y, max_y))


class Viewport:
    """High-level viewport combining camera with render area constraints."""

    def __init__(self, engine: Optional["GameEngine"] = None):
        if engine:
            ui_height = 5
            self.render_width = engine.width
            self.render_height = engine.height - ui_height
        else:
            self.render_width = 80
            self.render_height = 20

        self.camera = Camera(self.render_width, self.render_height)
        self._layer_manager: Optional[LayerManager] = None

    def set_layer_manager(self, manager: LayerManager) -> None:
        """Set the layer manager for rendering."""
        self._layer_manager = manager

    def update(self, dt: float) -> None:
        """Update camera position."""
        self.camera.update(dt)

    def follow(self, x: int, y: int) -> None:
        """Set camera to follow a position."""
        self.camera.follow(x, y)

    def world_to_screen(self, wx: int, wy: int) -> Tuple[int, int]:
        """Convert world to screen coordinates."""
        return self.camera.world_to_screen(wx, wy)

    def screen_to_world(self, sx: int, sy: int) -> Tuple[int, int]:
        """Convert screen to world coordinates."""
        return self.camera.screen_to_world(sx, sy)

    def is_visible(self, wx: int, wy: int) -> bool:
        """Check if world position is visible."""
        return self.camera.is_visible(wx, wy)

    def set_map_bounds(self, w: int, h: int) -> None:
        """Set map boundaries for camera."""
        self.camera.set_bounds(w, h)

    def render(self) -> None:
        """Render all layers through the layer manager."""
        if self._layer_manager:
            self._layer_manager.render()
