"""
ALT_LAS Engine - Light System
Point lights, ambient lighting, and light calculations.
"""

import math
from typing import Tuple, List, Optional
from dataclasses import dataclass, field


@dataclass
class Light:
    """A single point light source."""

    x: float
    y: float
    radius: float = 100.0
    color: Tuple[float, float, float] = (1.0, 1.0, 1.0)
    intensity: float = 1.0
    flicker: bool = False
    flicker_speed: float = 1.0

    # Runtime state
    _time: float = field(default=0.0, repr=False)

    def update(self, dt: float) -> None:
        """Update light state."""
        self._time += dt

    def get_effective_intensity(self) -> float:
        """Get intensity with optional flicker."""
        if self.flicker:
            import random
            flicker = math.sin(self._time * self.flicker_speed * 10)
            return self.intensity * (0.8 + flicker * 0.2)
        return self.intensity

    def calculate_contribution(self, px: float, py: float) -> Tuple[float, float, float]:
        """
        Calculate light contribution at a point.
        Returns RGB contribution (0.0 - 1.0+).
        """
        dist = math.sqrt((px - self.x) ** 2 + (py - self.y) ** 2)

        # Smooth falloff
        attenuation = max(0.0, 1.0 - (dist / self.radius))
        attenuation = attenuation ** 2  # Quadratic falloff

        intensity = self.get_effective_intensity()

        return (
            self.color[0] * attenuation * intensity,
            self.color[1] * attenuation * intensity,
            self.color[2] * attenuation * intensity
        )


class LightSystem:
    """
    Manages multiple lights and calculates combined lighting.
    """

    MAX_LIGHTS = 8  # Match shader limit

    def __init__(self):
        self._lights: List[Light] = []
        self._ambient = (0.3, 0.3, 0.4)  # Default dim blue ambient

    @property
    def ambient(self) -> Tuple[float, float, float]:
        """Get ambient light color."""
        return self._ambient

    @ambient.setter
    def ambient(self, value: Tuple[float, float, float]) -> None:
        """Set ambient light color."""
        self._ambient = value

    def add_light(self, light: Light) -> int:
        """Add a light. Returns index."""
        if len(self._lights) >= self.MAX_LIGHTS:
            # Remove oldest light
            self._lights.pop(0)
        self._lights.append(light)
        return len(self._lights) - 1

    def create_light(self, x: float, y: float, radius: float = 100.0,
                     color: Tuple[float, float, float] = (1.0, 1.0, 1.0),
                     intensity: float = 1.0) -> Light:
        """Create and add a new light."""
        light = Light(x=x, y=y, radius=radius, color=color, intensity=intensity)
        self.add_light(light)
        return light

    def remove_light(self, light: Light) -> None:
        """Remove a light."""
        if light in self._lights:
            self._lights.remove(light)

    def remove_light_at(self, index: int) -> None:
        """Remove light by index."""
        if 0 <= index < len(self._lights):
            self._lights.pop(index)

    def clear_lights(self) -> None:
        """Remove all lights."""
        self._lights.clear()

    def update(self, dt: float) -> None:
        """Update all lights."""
        for light in self._lights:
            light.update(dt)

    def calculate_lighting_at(self, x: float, y: float) -> Tuple[float, float, float]:
        """
        Calculate combined lighting at a point.
        Returns RGB light intensity.
        """
        # Start with ambient
        r, g, b = self._ambient

        # Add contributions from all lights
        for light in self._lights:
            lr, lg, lb = light.calculate_contribution(x, y)
            r += lr
            g += lg
            b += lb

        return (r, g, b)

    def get_shader_uniforms(self) -> dict:
        """
        Get light data formatted for shader uniforms.
        Returns dict with arrays for each light property.
        """
        positions = []
        colors = []
        radii = []
        intensities = []

        for light in self._lights[:self.MAX_LIGHTS]:
            positions.append((light.x, light.y))
            colors.append(light.color)
            radii.append(light.radius)
            intensities.append(light.get_effective_intensity())

        # Pad with zeros if needed
        while len(positions) < self.MAX_LIGHTS:
            positions.append((0.0, 0.0))
            colors.append((0.0, 0.0, 0.0))
            radii.append(0.0)
            intensities.append(0.0)

        return {
            "light_count": min(len(self._lights), self.MAX_LIGHTS),
            "light_positions": positions,
            "light_colors": colors,
            "light_radii": radii,
            "light_intensities": intensities,
            "ambient": self._ambient,
        }

    @property
    def lights(self) -> List[Light]:
        """Get list of all lights."""
        return self._lights.copy()

    @property
    def light_count(self) -> int:
        """Get number of active lights."""
        return len(self._lights)
