"""Rendering components."""
from src.render.terminal_detect import TerminalCapability, RenderMode, get_terminal_capability, get_render_mode
from src.render.layers import LayerManager, Layer, get_layer_manager, create_layer_manager
from src.render.adapter import RenderAdapter, RenderBackend, get_render_adapter, create_render_adapter
from src.render.sprites import SpriteLoader, Sprite, get_sprite_loader
from src.render.viewport import Viewport
from src.render.effects import EffectsManager
from src.render.camera import Camera
from src.render.particles import ParticleSystem
from src.render.lights import LightSystem
from src.render.shader_loader import ShaderLoader

# Layer constants
LAYER_MAP = Layer.MAP
LAYER_ENTITIES = Layer.ENTITIES
LAYER_EFFECTS = Layer.EFFECTS
LAYER_UI = Layer.UI

# Convenience functions
from src.render.layers import draw_char, draw_sprite, draw_text, draw_box, draw_bar, clear_layer

__all__ = [
    'TerminalCapability', 'RenderMode', 'get_terminal_capability', 'get_render_mode',
    'LayerManager', 'Layer', 'get_layer_manager', 'create_layer_manager',
    'RenderAdapter', 'RenderBackend', 'get_render_adapter', 'create_render_adapter',
    'SpriteLoader', 'Sprite', 'get_sprite_loader',
    'Viewport', 'EffectsManager', 'Camera', 'ParticleSystem', 'LightSystem', 'ShaderLoader',
    'LAYER_MAP', 'LAYER_ENTITIES', 'LAYER_EFFECTS', 'LAYER_UI',
    'draw_char', 'draw_sprite', 'draw_text', 'draw_box', 'draw_bar', 'clear_layer'
]
