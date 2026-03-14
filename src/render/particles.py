"""
ALT_LAS Engine - Particle System
Simple particle effects for visual polish.
"""

import random
import math
from typing import Optional, Tuple, List, Callable
from dataclasses import dataclass, field


@dataclass
class Particle:
    """A single particle with position, velocity, and lifetime."""

    x: float
    y: float
    vx: float = 0.0
    vy: float = 0.0
    ax: float = 0.0  # Acceleration
    ay: float = 0.0

    lifetime: float = 1.0
    age: float = 0.0

    size: float = 4.0
    size_end: float = 0.0

    color: Tuple[int, int, int, int] = (255, 255, 255, 255)
    color_end: Optional[Tuple[int, int, int, int]] = None

    # Display
    char: str = "*"
    use_char: bool = True  # False = use texture

    # Callbacks
    _on_death: Optional[Callable] = field(default=None, repr=False)

    def __post_init__(self):
        """Initialize particle."""
        self._initial_size = self.size
        self._initial_color = self.color
        self.is_alive = True

    def update(self, dt: float) -> None:
        """Update particle state."""
        if not self.is_alive:
            return

        self.age += dt
        if self.age >= self.lifetime:
            self.is_alive = False
            if self._on_death:
                self._on_death(self)
            return

        # Apply acceleration
        self.vx += self.ax * dt
        self.vy += self.ay * dt

        # Apply velocity
        self.x += self.vx * dt
        self.y += self.vy * dt

    def get_progress(self) -> float:
        """Get lifetime progress (0.0 - 1.0)."""
        return min(1.0, self.age / self.lifetime) if self.lifetime > 0 else 1.0

    def get_current_size(self) -> float:
        """Get interpolated size."""
        t = self.get_progress()
        return self._initial_size + (self.size_end - self._initial_size) * t

    def get_current_color(self) -> Tuple[int, int, int, int]:
        """Get interpolated color."""
        if self.color_end is None:
            return self.color

        t = self.get_progress()
        return (
            int(self._initial_color[0] + (self.color_end[0] - self._initial_color[0]) * t),
            int(self._initial_color[1] + (self.color_end[1] - self._initial_color[1]) * t),
            int(self._initial_color[2] + (self.color_end[2] - self._initial_color[2]) * t),
            int(self._initial_color[3] + (self.color_end[3] - self._initial_color[3]) * t),
        )


class ParticleEmitter:
    """Emits particles with configurable patterns."""

    def __init__(self, x: float, y: float):
        self.x = x
        self.y = y
        self.active = True

        # Emission settings
        self.rate: float = 10.0  # Particles per second
        self.burst: int = 0  # Burst count (0 = continuous)
        self._emit_timer: float = 0.0
        self._burst_remaining: int = 0

        # Particle defaults
        self.particle_lifetime: float = 1.0
        self.particle_lifetime_variance: float = 0.0

        self.particle_speed: float = 50.0
        self.particle_speed_variance: float = 0.0

        self.particle_angle: float = -90.0  # Up
        self.particle_angle_spread: float = 30.0  # Degrees

        self.particle_size: float = 4.0
        self.particle_size_end: float = 0.0

        self.particle_color: Tuple[int, int, int, int] = (255, 255, 255, 255)
        self.particle_color_end: Optional[Tuple[int, int, int, int]] = None

        self.particle_gravity: float = 0.0

    def update(self, dt: float, spawn_callback: Callable[[Particle], None]) -> None:
        """Update emitter and spawn particles."""
        if not self.active:
            return

        if self.burst > 0 and self._burst_remaining <= 0:
            return

        self._emit_timer += dt
        emit_interval = 1.0 / self.rate if self.rate > 0 else 0

        while self._emit_timer >= emit_interval:
            self._emit_timer -= emit_interval

            particle = self._create_particle()
            spawn_callback(particle)

            if self.burst > 0:
                self._burst_remaining -= 1
                if self._burst_remaining <= 0:
                    break

    def _create_particle(self) -> Particle:
        """Create a particle with emitter settings."""
        # Random angle
        angle = self.particle_angle + random.uniform(
            -self.particle_angle_spread / 2,
            self.particle_angle_spread / 2
        )
        angle_rad = math.radians(angle)

        # Random speed
        speed = self.particle_speed + random.uniform(
            -self.particle_speed_variance,
            self.particle_speed_variance
        )

        # Velocity from angle and speed
        vx = math.cos(angle_rad) * speed
        vy = math.sin(angle_rad) * speed

        # Random lifetime
        lifetime = self.particle_lifetime + random.uniform(
            -self.particle_lifetime_variance,
            self.particle_lifetime_variance
        )

        return Particle(
            x=self.x,
            y=self.y,
            vx=vx,
            vy=vy,
            ay=self.particle_gravity,
            lifetime=lifetime,
            size=self.particle_size,
            size_end=self.particle_size_end,
            color=self.particle_color,
            color_end=self.particle_color_end
        )

    def trigger_burst(self, count: Optional[int] = None) -> None:
        """Trigger a burst of particles."""
        self._burst_remaining = count or self.burst or 1
        self._emit_timer = 1.0 / self.rate  # Immediate emission


class ParticleSystem:
    """Manages multiple particle emitters and particles."""

    def __init__(self, max_particles: int = 500):
        self._max_particles = max_particles
        self._particles: List[Particle] = []
        self._emitters: List[ParticleEmitter] = []

    def update(self, dt: float) -> None:
        """Update all particles and emitters."""
        # Update emitters
        for emitter in self._emitters:
            emitter.update(dt, self._spawn_particle)

        # Update particles
        for particle in self._particles:
            particle.update(dt)

        # Remove dead particles
        self._particles = [p for p in self._particles if p.is_alive]

    def _spawn_particle(self, particle: Particle) -> None:
        """Spawn a particle (with limit check)."""
        if len(self._particles) < self._max_particles:
            self._particles.append(particle)

    def spawn(self, particle: Particle) -> None:
        """Manually spawn a particle."""
        self._spawn_particle(particle)

    def spawn_burst(self, x: float, y: float, count: int = 10, **kwargs) -> None:
        """Spawn a burst of particles at position."""
        for _ in range(count):
            p = Particle(x=x, y=y, **kwargs)
            self._spawn_particle(p)

    def add_emitter(self, emitter: ParticleEmitter) -> None:
        """Add a particle emitter."""
        self._emitters.append(emitter)

    def remove_emitter(self, emitter: ParticleEmitter) -> None:
        """Remove a particle emitter."""
        if emitter in self._emitters:
            self._emitters.remove(emitter)

    def clear(self) -> None:
        """Clear all particles and emitters."""
        self._particles.clear()
        self._emitters.clear()

    @property
    def particles(self) -> List[Particle]:
        """Get active particles."""
        return self._particles

    @property
    def particle_count(self) -> int:
        """Get number of active particles."""
        return len(self._particles)
