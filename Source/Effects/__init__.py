"""Effects components."""
try:
    from src.render.effects import EffectsManager
    from src.render.particles import Particle, ParticleSystem
except ImportError:
    EffectsManager = None
    Particle = None
    ParticleSystem = None

__all__ = ['EffectsManager', 'Particle', 'ParticleSystem']
