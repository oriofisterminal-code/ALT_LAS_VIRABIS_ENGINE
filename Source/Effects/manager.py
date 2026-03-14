"""
ALT_LAS Engine - Effects Manager
Central manager for all visual effects.
Coordinates lights, particles, and post-processing.
"""

import time
from typing import Optional, List, Dict, Callable
from enum import Enum, auto
from dataclasses import dataclass, field


class EffectType(Enum):
    """Available effect types."""
    LIGHT_POINT = auto()
    LIGHT_AMBIENT = auto()
    GLOW_BLOOM = auto()
    WATER_WAVE = auto()
    PARTICLE_EMITITTER = auto()
    SCREEN_SHAKE = auto()
    COLOR_TINT = auto()
    VIGNETTE = auto()


@dataclass
class EffectConfig:
    """Configuration for an effect."""
    effect_type: EffectType
    enabled: bool = True
    intensity: float = 1.0
    duration: Optional[float] = None  # None = permanent
    elapsed: float = 0.0
    params: Dict = field(default_factory=dict)


class EffectsManager:
    """
    Central manager for all visual effects.
    Handles effect lifecycle, priority, and GPU coordination.
    """

    def __init__(self):
        self._effects: Dict[str, EffectConfig] = {}
        self._active_lights: List = []
        self._particles: List = []
        self._last_time = time.time()
        self._global_intensity = 1.0

        # Effect state
        self._ambient_color = (0.3, 0.3, 0.4)
        self._glow_enabled = False
        self._water_enabled = False
        self._water_speed = 1.0
        self._shake_offset = (0.0, 0.0)

    def update(self, dt: Optional[float] = None) -> None:
        """Update all active effects."""
        if dt is None:
            now = time.time()
            dt = now - self._last_time
            self._last_time = now

        # Update timed effects
        to_remove = []
        for name, effect in self._effects.items():
            if effect.duration is not None:
                effect.elapsed += dt
                if effect.elapsed >= effect.duration:
                    to_remove.append(name)

        # Remove expired effects
        for name in to_remove:
            del self._effects[name]

        # Update screen shake
        if "screen_shake" in self._effects:
            self._update_shake(dt)

        # Update particles
        self._update_particles(dt)

    def _update_shake(self, dt: float) -> None:
        """Update screen shake effect."""
        import random
        effect = self._effects.get("screen_shake")
        if effect and effect.enabled:
            intensity = effect.params.get("intensity", 5.0)
            self._shake_offset = (
                random.uniform(-intensity, intensity),
                random.uniform(-intensity, intensity)
            )
        else:
            self._shake_offset = (0.0, 0.0)

    def _update_particles(self, dt: float) -> None:
        """Update all particles."""
        for p in self._particles:
            p.update(dt)
        self._particles = [p for p in self._particles if p.is_alive]

    # --- Effect Registration ---

    def register_effect(self, name: str, config: EffectConfig) -> None:
        """Register a new effect."""
        self._effects[name] = config

    def remove_effect(self, name: str) -> None:
        """Remove an effect by name."""
        if name in self._effects:
            del self._effects[name]

    def set_effect_enabled(self, name: str, enabled: bool) -> None:
        """Enable or disable an effect."""
        if name in self._effects:
            self._effects[name].enabled = enabled

    def get_effect(self, name: str) -> Optional[EffectConfig]:
        """Get effect configuration by name."""
        return self._effects.get(name)

    # --- Lighting ---

    def set_ambient_light(self, r: float, g: float, b: float) -> None:
        """Set global ambient light color."""
        self._ambient_color = (r, g, b)
        self.register_effect("ambient", EffectConfig(
            effect_type=EffectType.LIGHT_AMBIENT,
            params={"color": self._ambient_color}
        ))

    def add_point_light(self, x: float, y: float, radius: float,
                        color: tuple = (1.0, 1.0, 1.0),
                        intensity: float = 1.0,
                        name: Optional[str] = None) -> str:
        """Add a point light. Returns light identifier."""
        import uuid
        light_id = name or f"light_{uuid.uuid4().hex[:8]}"

        self.register_effect(light_id, EffectConfig(
            effect_type=EffectType.LIGHT_POINT,
            params={
                "x": x,
                "y": y,
                "radius": radius,
                "color": color,
                "intensity": intensity
            }
        ))

        return light_id

    def move_light(self, light_id: str, x: float, y: float) -> None:
        """Move a point light to new position."""
        effect = self._effects.get(light_id)
        if effect and effect.effect_type == EffectType.LIGHT_POINT:
            effect.params["x"] = x
            effect.params["y"] = y

    def get_lights(self) -> List[Dict]:
        """Get all active point lights."""
        lights = []
        for name, effect in self._effects.items():
            if effect.effect_type == EffectType.LIGHT_POINT and effect.enabled:
                lights.append({
                    "id": name,
                    **effect.params
                })
        return lights

    # --- Glow/Bloom ---

    def set_glow_enabled(self, enabled: bool, intensity: float = 1.0) -> None:
        """Enable/disable glow effect."""
        self._glow_enabled = enabled
        if enabled:
            self.register_effect("glow", EffectConfig(
                effect_type=EffectType.GLOW_BLOOM,
                params={"intensity": intensity}
            ))
        else:
            self.remove_effect("glow")

    # --- Water Effect ---

    def set_water_enabled(self, enabled: bool, speed: float = 1.0) -> None:
        """Enable/disable water wave effect."""
        self._water_enabled = enabled
        self._water_speed = speed
        if enabled:
            self.register_effect("water", EffectConfig(
                effect_type=EffectType.WATER_WAVE,
                params={"speed": speed}
            ))
        else:
            self.remove_effect("water")

    # --- Screen Shake ---

    def start_shake(self, intensity: float = 5.0, duration: float = 0.5) -> None:
        """Start screen shake effect."""
        self.register_effect("screen_shake", EffectConfig(
            effect_type=EffectType.SCREEN_SHAKE,
            duration=duration,
            params={"intensity": intensity}
        ))

    @property
    def shake_offset(self) -> tuple:
        """Get current shake offset for rendering."""
        return self._shake_offset

    # --- Particles ---

    def spawn_particle(self, x: float, y: float, **kwargs) -> None:
        """Spawn a single particle."""
        from Source.Effects.particles import Particle
        p = Particle(x, y, **kwargs)
        self._particles.append(p)

    def spawn_particles(self, x: float, y: float, count: int, **kwargs) -> None:
        """Spawn multiple particles."""
        for _ in range(count):
            self.spawn_particle(x, y, **kwargs)

    @property
    def particles(self) -> List:
        """Get active particles."""
        return self._particles

    # --- Utility ---

    @property
    def global_intensity(self) -> float:
        """Get global effect intensity multiplier."""
        return self._global_intensity

    @global_intensity.setter
    def global_intensity(self, value: float) -> None:
        """Set global effect intensity multiplier."""
        self._global_intensity = max(0.0, min(2.0, value))

    def clear_all(self) -> None:
        """Remove all effects."""
        self._effects.clear()
        self._particles.clear()
        self._shake_offset = (0.0, 0.0)

    def get_shader_params(self) -> Dict:
        """Get parameters for shader uniforms."""
        return {
            "ambient": self._ambient_color,
            "lights": self.get_lights(),
            "glow_enabled": self._glow_enabled,
            "water_enabled": self._water_enabled,
            "water_speed": self._water_speed,
            "shake_offset": self._shake_offset,
        }
