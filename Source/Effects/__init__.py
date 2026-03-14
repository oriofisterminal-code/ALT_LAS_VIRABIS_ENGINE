"""
ALT_LAS Engine - Effects Module
GPU-accelerated visual effects for terminal games.
Provides lighting, particles, and post-processing.
"""

from Source.Effects.manager import EffectsManager, EffectType
from Source.Effects.lights import Light, LightSystem
from Source.Effects.particles import ParticleSystem, Particle

__all__ = [
    "EffectsManager",
    "EffectType",
    "Light",
    "LightSystem",
    "ParticleSystem",
    "Particle",
]
