"""
ALT_LAS Engine - Rendering Module
Terminal-based rendering with Sixel/Kitty graphics and ASCII fallback.
"""

from Source.Rendering.terminal_detect import (
    TerminalCapability,
    RenderMode,
    get_terminal_capability,
    get_render_mode,
)
from Source.Rendering.sprite_loader import (
    Sprite,
    SpriteLoader,
    get_sprite_loader,
)
from Source.Rendering.layer_manager import (
    Layer,
    LayerManager,
    LayerBuffer,
    create_layer_manager,
    get_layer_manager,
    draw_char,
    draw_sprite,
    draw_text,
    draw_box,
    draw_bar,
    clear_layer,
)
from Source.Rendering.viewport import (
    Camera,
    Viewport,
)

LAYER_MAP = Layer.MAP
LAYER_ENTITIES = Layer.ENTITIES
LAYER_EFFECTS = Layer.EFFECTS
LAYER_UI = Layer.UI

__all__ = [
    "TerminalCapability",
    "RenderMode",
    "get_terminal_capability",
    "get_render_mode",
    "Sprite",
    "SpriteLoader",
    "get_sprite_loader",
    "Layer",
    "LayerManager",
    "LayerBuffer",
    "create_layer_manager",
    "get_layer_manager",
    "draw_char",
    "draw_sprite",
    "draw_text",
    "draw_box",
    "draw_bar",
    "clear_layer",
    "Camera",
    "Viewport",
    "LAYER_MAP",
    "LAYER_ENTITIES",
    "LAYER_EFFECTS",
    "LAYER_UI",
]
