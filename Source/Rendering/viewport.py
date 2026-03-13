"""
ALT_LAS Engine - Viewport / Camera System
Handles camera following, smooth scrolling, and world-to-screen coordinate mapping.
"""


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
        self._bounds = None

    def set_bounds(self, map_width: int, map_height: int) -> None:
        self._bounds = (map_width, map_height)

    def follow(self, target_x: int, target_y: int) -> None:
        self.target_x = float(target_x) - self.view_width // 2
        self.target_y = float(target_y) - self.view_height // 2

    def update(self, dt: float) -> None:
        self.x += (self.target_x - self.x) * self.smoothing
        self.y += (self.target_y - self.y) * self.smoothing
        if self._bounds:
            mw, mh = self._bounds
            self.x = max(0, min(self.x, mw - self.view_width))
            self.y = max(0, min(self.y, mh - self.view_height))

    def world_to_screen(self, wx: int, wy: int) -> tuple:
        sx = wx - int(self.x)
        sy = wy - int(self.y)
        return sx, sy

    def screen_to_world(self, sx: int, sy: int) -> tuple:
        wx = sx + int(self.x)
        wy = sy + int(self.y)
        return wx, wy

    def is_visible(self, wx: int, wy: int) -> bool:
        sx, sy = self.world_to_screen(wx, wy)
        return 0 <= sx < self.view_width and 0 <= sy < self.view_height

    def snap_to_target(self) -> None:
        self.x = self.target_x
        self.y = self.target_y
        if self._bounds:
            mw, mh = self._bounds
            self.x = max(0, min(self.x, mw - self.view_width))
            self.y = max(0, min(self.y, mh - self.view_height))


class Viewport:
    """High-level viewport that combines camera with render area constraints."""

    def __init__(self, engine: "GameEngine"):
        ui_height = 5
        self.render_width = engine.width
        self.render_height = engine.height - ui_height
        self.camera = Camera(self.render_width, self.render_height)

    def update(self, dt: float) -> None:
        self.camera.update(dt)

    def follow(self, x: int, y: int) -> None:
        self.camera.follow(x, y)

    def world_to_screen(self, wx: int, wy: int) -> tuple:
        return self.camera.world_to_screen(wx, wy)

    def is_visible(self, wx: int, wy: int) -> bool:
        return self.camera.is_visible(wx, wy)

    def set_map_bounds(self, w: int, h: int) -> None:
        self.camera.set_bounds(w, h)
