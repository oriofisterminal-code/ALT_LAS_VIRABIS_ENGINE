"""
Camera System for ALT_LAS Engine
Handles viewport, zoom, and camera movement for terminal rendering.

Part of: ALT_LAS Engine v0.3.0
Author: ALT_LAS Team
"""

from dataclasses import dataclass, field
from typing import Tuple, Optional, List
import math


@dataclass
class CameraConfig:
    """Camera configuration settings."""
    width: int = 60          # Grid width in tiles
    height: int = 34         # Grid height in tiles
    tile_size: int = 16      # Pixels per tile
    zoom_min: float = 0.5
    zoom_max: float = 3.0
    zoom_default: float = 1.0
    smoothing: float = 0.15  # Camera follow smoothing (0=instant, 1=very slow)
    deadzone: Tuple[float, float] = (3.0, 2.0)  # Dead zone before camera moves
    bounds_enabled: bool = True


@dataclass
class CameraState:
    """Current camera state."""
    x: float = 0.0
    y: float = 0.0
    target_x: float = 0.0
    target_y: float = 0.0
    zoom: float = 1.0
    target_zoom: float = 1.0
    rotation: float = 0.0
    shake_intensity: float = 0.0
    shake_duration: float = 0.0
    shake_time: float = 0.0


class Camera:
    """
    Terminal-optimized camera system with smooth following,
    zoom support, and screen shake effects.
    
    Usage:
        camera = Camera(60, 34)
        camera.follow(player)
        camera.update(delta_time)
        visible_tiles = camera.get_visible_area()
    """
    
    def __init__(self, config: CameraConfig = None):
        self.config = config or CameraConfig()
        self.state = CameraState()
        
        # Bounds (map limits)
        self.bounds: Optional[Tuple[float, float, float, float]] = None  # (min_x, min_y, max_x, max_y)
        
        # Follow target
        self._follow_target = None
        self._follow_offset = (0.0, 0.0)
        
        # Screen shake state
        self._shake_offset = (0.0, 0.0)
        
        # Camera layers for parallax
        self._parallax_layers: List[Tuple[float, float]] = []  # (scroll_x_factor, scroll_y_factor)
        
        # Cached visible area
        self._visible_area_cache = None
        self._cache_dirty = True
        
    def set_position(self, x: float, y: float, instant: bool = False):
        """
        Set camera position.
        
        Args:
            x: World X position in tiles
            y: World Y position in tiles
            instant: If True, skip smoothing
        """
        self.state.target_x = x
        self.state.target_y = y
        
        if instant:
            self.state.x = x
            self.state.y = y
            
        self._cache_dirty = True
        
    def set_zoom(self, zoom: float, instant: bool = False):
        """
        Set camera zoom level.
        
        Args:
            zoom: Zoom level (0.5 = zoom out, 2.0 = zoom in)
            instant: If True, skip smoothing
        """
        zoom = max(self.config.zoom_min, min(self.config.zoom_max, zoom))
        self.state.target_zoom = zoom
        
        if instant:
            self.state.zoom = zoom
            
        self._cache_dirty = True
        
    def set_rotation(self, angle: float):
        """
        Set camera rotation in degrees.
        
        Args:
            angle: Rotation angle in degrees
        """
        self.state.rotation = angle % 360
        self._cache_dirty = True
        
    def follow(self, target, offset: Tuple[float, float] = (0.0, 0.0)):
        """
        Set entity for camera to follow.
        
        Args:
            target: Entity with x, y attributes
            offset: Offset from target position
        """
        self._follow_target = target
        self._follow_offset = offset
        self._cache_dirty = True
        
    def stop_following(self):
        """Stop following any target."""
        self._follow_target = None
        
    def set_bounds(self, min_x: float, min_y: float, max_x: float, max_y: float):
        """
        Set camera movement bounds (map limits).
        
        Args:
            min_x: Minimum X position
            min_y: Minimum Y position
            max_x: Maximum X position
            max_y: Maximum Y position
        """
        self.bounds = (min_x, min_y, max_x, max_y)
        self._cache_dirty = True
        
    def clear_bounds(self):
        """Remove camera bounds."""
        self.bounds = None
        
    def shake(self, intensity: float = 0.5, duration: float = 0.3):
        """
        Apply screen shake effect.
        
        Args:
            intensity: Shake intensity in tiles
            duration: Shake duration in seconds
        """
        self.state.shake_intensity = intensity
        self.state.shake_duration = duration
        self.state.shake_time = 0.0
        
    def add_parallax_layer(self, scroll_x: float, scroll_y: float):
        """
        Add a parallax layer.
        
        Args:
            scroll_x: Horizontal scroll factor (0.5 = half speed)
            scroll_y: Vertical scroll factor
        """
        self._parallax_layers.append((scroll_x, scroll_y))
        
    def update(self, delta_time: float):
        """
        Update camera position and effects.
        
        Args:
            delta_time: Time since last update in seconds
        """
        smoothing = self.config.smoothing
        deadzone = self.config.deadzone
        
        # Follow target if set
        if self._follow_target is not None:
            try:
                target_x = self._follow_target.x + self._follow_offset[0]
                target_y = self._follow_target.y + self._follow_offset[1]
                
                # Apply deadzone
                dx = abs(target_x - self.state.target_x)
                dy = abs(target_y - self.state.target_y)
                
                if dx > deadzone[0]:
                    self.state.target_x = target_x
                if dy > deadzone[1]:
                    self.state.target_y = target_y
                    
            except AttributeError:
                pass  # Target has no x, y attributes
                
        # Smooth position interpolation
        self.state.x += (self.state.target_x - self.state.x) * smoothing
        self.state.y += (self.state.target_y - self.state.y) * smoothing
        
        # Smooth zoom interpolation
        self.state.zoom += (self.state.target_zoom - self.state.zoom) * smoothing
        
        # Apply bounds
        if self.bounds and self.config.bounds_enabled:
            half_width = (self.config.width / 2) / self.state.zoom
            half_height = (self.config.height / 2) / self.state.zoom
            
            self.state.x = max(self.bounds[0] + half_width, 
                               min(self.bounds[2] - half_width, self.state.x))
            self.state.y = max(self.bounds[1] + half_height,
                               min(self.bounds[3] - half_height, self.state.y))
                               
        # Update screen shake
        if self.state.shake_time < self.state.shake_duration:
            self.state.shake_time += delta_time
            progress = self.state.shake_time / self.state.shake_duration
            
            # Decay intensity over time
            current_intensity = self.state.shake_intensity * (1 - progress)
            
            # Random shake offset
            import random
            angle = random.random() * math.pi * 2
            self._shake_offset = (
                math.cos(angle) * current_intensity,
                math.sin(angle) * current_intensity
            )
        else:
            self._shake_offset = (0.0, 0.0)
            
        self._cache_dirty = True
        
    def get_visible_area(self) -> Tuple[int, int, int, int]:
        """
        Get the currently visible tile area.
        
        Returns:
            Tuple of (start_x, start_y, end_x, end_y)
        """
        if not self._cache_dirty and self._visible_area_cache:
            return self._visible_area_cache
            
        zoom = self.state.zoom
        half_width = (self.config.width / 2) / zoom
        half_height = (self.config.height / 2) / zoom
        
        # Apply shake offset
        x = self.state.x + self._shake_offset[0]
        y = self.state.y + self._shake_offset[1]
        
        start_x = int(x - half_width)
        start_y = int(y - half_height)
        end_x = int(x + half_width) + 1
        end_y = int(y + half_height) + 1
        
        self._visible_area_cache = (start_x, start_y, end_x, end_y)
        self._cache_dirty = False
        
        return self._visible_area_cache
        
    def is_tile_visible(self, tile_x: int, tile_y: int) -> bool:
        """
        Check if a tile is currently visible.
        
        Args:
            tile_x: Tile X position
            tile_y: Tile Y position
            
        Returns:
            True if tile is visible
        """
        start_x, start_y, end_x, end_y = self.get_visible_area()
        return start_x <= tile_x < end_x and start_y <= tile_y < end_y
        
    def world_to_screen(self, world_x: float, world_y: float) -> Tuple[float, float]:
        """
        Convert world position to screen position.
        
        Args:
            world_x: World X position
            world_y: World Y position
            
        Returns:
            Screen position (x, y)
        """
        zoom = self.state.zoom
        half_width = self.config.width / 2
        half_height = self.config.height / 2
        
        # Apply camera position and shake
        x = self.state.x + self._shake_offset[0]
        y = self.state.y + self._shake_offset[1]
        
        screen_x = (world_x - x) * zoom + half_width
        screen_y = (world_y - y) * zoom + half_height
        
        # Apply rotation if needed
        if self.state.rotation != 0:
            rad = math.radians(self.state.rotation)
            cos_r, sin_r = math.cos(rad), math.sin(rad)
            screen_x -= half_width
            screen_y -= half_height
            new_x = screen_x * cos_r - screen_y * sin_r
            new_y = screen_x * sin_r + screen_y * cos_r
            screen_x = new_x + half_width
            screen_y = new_y + half_height
            
        return (screen_x, screen_y)
        
    def screen_to_world(self, screen_x: float, screen_y: float) -> Tuple[float, float]:
        """
        Convert screen position to world position.
        
        Args:
            screen_x: Screen X position
            screen_y: Screen Y position
            
        Returns:
            World position (x, y)
        """
        zoom = self.state.zoom
        half_width = self.config.width / 2
        half_height = self.config.height / 2
        
        # Reverse rotation if needed
        if self.state.rotation != 0:
            rad = math.radians(-self.state.rotation)
            cos_r, sin_r = math.cos(rad), math.sin(rad)
            sx = screen_x - half_width
            sy = screen_y - half_height
            screen_x = sx * cos_r - sy * sin_r + half_width
            screen_y = sx * sin_r + sy * cos_r + half_height
            
        # Apply camera position and shake
        x = self.state.x + self._shake_offset[0]
        y = self.state.y + self._shake_offset[1]
        
        world_x = (screen_x - half_width) / zoom + x
        world_y = (screen_y - half_height) / zoom + y
        
        return (world_x, world_y)
        
    def get_parallax_offset(self, layer_index: int) -> Tuple[float, float]:
        """
        Get parallax offset for a specific layer.
        
        Args:
            layer_index: Index of parallax layer
            
        Returns:
            Parallax offset (x, y)
        """
        if layer_index >= len(self._parallax_layers):
            return (0.0, 0.0)
            
        scroll_x, scroll_y = self._parallax_layers[layer_index]
        
        return (
            self.state.x * (1 - scroll_x) * self.config.tile_size,
            self.state.y * (1 - scroll_y) * self.config.tile_size
        )
        
    def center_on(self, x: float, y: float, instant: bool = False):
        """
        Center camera on a specific position.
        
        Args:
            x: World X position
            y: World Y position
            instant: If True, skip smoothing
        """
        self.set_position(x, y, instant)
        
    def move(self, dx: float, dy: float):
        """
        Move camera by a delta amount.
        
        Args:
            dx: X movement
            dy: Y movement
        """
        self.state.target_x += dx
        self.state.target_y += dy
        self._cache_dirty = True
        
    def zoom_in(self, amount: float = 0.1):
        """Zoom camera in."""
        self.set_zoom(self.state.target_zoom + amount)
        
    def zoom_out(self, amount: float = 0.1):
        """Zoom camera out."""
        self.set_zoom(self.state.target_zoom - amount)
        
    def reset_zoom(self):
        """Reset zoom to default."""
        self.set_zoom(self.config.zoom_default)
        
    @property
    def position(self) -> Tuple[float, float]:
        """Get current camera position."""
        return (self.state.x, self.state.y)
        
    @property
    def target_position(self) -> Tuple[float, float]:
        """Get target camera position."""
        return (self.state.target_x, self.state.target_y)
        
    @property
    def current_zoom(self) -> float:
        """Get current zoom level."""
        return self.state.zoom


class CameraManager:
    """
    Manages multiple cameras for different rendering contexts.
    """
    
    def __init__(self):
        self.cameras: dict = {}
        self.active_camera: Optional[str] = None
        
    def create_camera(self, name: str, config: CameraConfig = None) -> Camera:
        """Create a new named camera."""
        camera = Camera(config)
        self.cameras[name] = camera
        
        if self.active_camera is None:
            self.active_camera = name
            
        return camera
        
    def get_camera(self, name: str) -> Optional[Camera]:
        """Get a camera by name."""
        return self.cameras.get(name)
        
    def set_active(self, name: str):
        """Set the active camera."""
        if name in self.cameras:
            self.active_camera = name
            
    def get_active(self) -> Optional[Camera]:
        """Get the active camera."""
        if self.active_camera:
            return self.cameras.get(self.active_camera)
        return None
        
    def update_all(self, delta_time: float):
        """Update all cameras."""
        for camera in self.cameras.values():
            camera.update(delta_time)
