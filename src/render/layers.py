"""
ALT_LAS Engine - Layer Manager
Rendering layers with ASCII/Sixel/Kitty fallback and GPU support.
Integrates with RenderAdapter for shader effects.
"""
import sys
import os
from typing import Optional, Dict, List
from enum import IntEnum
from src.render.terminal_detect import RenderMode, get_terminal_capability
from src.render.sprites import Sprite, get_sprite_loader

# Windows terminal setup
if sys.platform == 'win32':
    try:
        from src.terminal.windows import init_windows_terminal, clear_screen, is_vt_enabled
        _windows_terminal_initialized = init_windows_terminal()
    except ImportError:
        _windows_terminal_initialized = False
else:
    _windows_terminal_initialized = True
    def clear_screen():
        sys.stdout.write("\x1b[2J\x1b[H")
        sys.stdout.flush()


class Layer(IntEnum):
    """Render layer priorities."""
    MAP = 0
    ENTITIES = 1
    EFFECTS = 2
    UI = 3


LAYER_MAP, LAYER_ENTITIES, LAYER_EFFECTS, LAYER_UI = Layer.MAP, Layer.ENTITIES, Layer.EFFECTS, Layer.UI


class LayerBuffer:
    """Buffer for a single render layer."""

    def __init__(self, width: int, height: int):
        self.width, self.height = width, height
        self._char_buffer = [[(" ", "default") for _ in range(width)] for _ in range(height)]
        self._sprite_buffer = [[None for _ in range(width)] for _ in range(height)]
        self._dirty = True

    def set_char(self, x: int, y: int, char: str, color: str = "default") -> None:
        if 0 <= x < self.width and 0 <= y < self.height:
            self._char_buffer[y][x] = (char, color)
            self._dirty = True

    def set_sprite(self, x: int, y: int, sprite: Optional[Sprite]) -> None:
        if 0 <= x < self.width and 0 <= y < self.height:
            self._sprite_buffer[y][x] = sprite
            self._dirty = True

    def get_char(self, x: int, y: int) -> tuple:
        if 0 <= x < self.width and 0 <= y < self.height:
            return self._char_buffer[y][x]
        return (" ", "default")

    def get_sprite(self, x: int, y: int) -> Optional[Sprite]:
        if 0 <= x < self.width and 0 <= y < self.height:
            return self._sprite_buffer[y][x]
        return None

    def clear(self) -> None:
        for y in range(self.height):
            for x in range(self.width):
                self._char_buffer[y][x] = (" ", "default")
                self._sprite_buffer[y][x] = None
        self._dirty = True


class LayerManager:
    """Manages all render layers and outputs to terminal. Supports GPU rendering."""

    def __init__(self, width: int = 80, height: int = 25):
        self.width, self.height = width, height
        self._capability = get_terminal_capability()
        self._render_mode = self._capability.render_mode
        self._sprite_loader = get_sprite_loader()
        self._layers: Dict[int, LayerBuffer] = {
            Layer.MAP: LayerBuffer(width, height),
            Layer.ENTITIES: LayerBuffer(width, height),
            Layer.EFFECTS: LayerBuffer(width, height),
            Layer.UI: LayerBuffer(width, height)
        }
        self._cursor_hidden, self._initialized = False, False
        self._render_adapter, self._use_gpu, self._effects_manager = None, False, None

    def initialize(self) -> None:
        if self._initialized:
            return
        # Hide cursor, clear screen, move to home
        sys.stdout.write("\x1b[?25l\x1b[2J\x1b[H")
        sys.stdout.flush()
        self._cursor_hidden = True
        self._try_init_gpu()
        self._initialized = True
        # Print terminal info
        if sys.platform == 'win32':
            print(f"[ALT_LAS] Windows Terminal initialized (VT: {_windows_terminal_initialized})")
        print(f"[ALT_LAS] Render mode: {self._render_mode.value}, Color depth: {self._capability.color_depth}")

    def _try_init_gpu(self) -> None:
        try:
            from src.terminal.adapter import get_render_adapter
            self._render_adapter = get_render_adapter()
            if self._render_adapter.initialize():
                self._use_gpu = self._render_adapter.is_gpu_active
                if self._use_gpu:
                    from src.render.manager import EffectsManager
                    self._effects_manager = EffectsManager()
        except ImportError:
            self._use_gpu, self._render_adapter = False, None

    def shutdown(self) -> None:
        if self._cursor_hidden:
            sys.stdout.write("\x1b[?25h\x1b[0m")
            sys.stdout.flush()
            self._cursor_hidden = False
        if self._render_adapter:
            self._render_adapter.shutdown()
        self._initialized = False

    def resize(self, width: int, height: int) -> None:
        self.width, self.height = width, height
        self._layers = {lid: LayerBuffer(width, height) for lid in self._layers}

    def clear_layer(self, layer: int) -> None:
        if layer in self._layers:
            self._layers[layer].clear()

    def clear_all(self) -> None:
        for lb in self._layers.values():
            lb.clear()

    def draw_char(self, x: int, y: int, char: str, color: str = "white", layer: int = Layer.ENTITIES) -> None:
        if layer in self._layers:
            self._layers[layer].set_char(x, y, char, color)

    def draw_sprite_at(self, x: int, y: int, sprite_name: str, layer: int = Layer.ENTITIES) -> bool:
        sprite = self._sprite_loader.load(sprite_name)
        if sprite:
            self._layers[layer].set_sprite(x, y, sprite)
            return True
        return False

    def draw_text(self, x: int, y: int, text: str, color: str = "white", layer: int = Layer.UI) -> None:
        for i, char in enumerate(text):
            if 0 <= x + i < self.width:
                self._layers[layer].set_char(x + i, y, char, color)

    def draw_box(self, x: int, y: int, w: int, h: int, border_color: str = "#aaaaaa",
                 fill_color: str = "#111111", layer: int = Layer.UI) -> None:
        for bx in range(x, x + w):
            for by in range(y, y + h):
                if bx == x or bx == x + w - 1 or by == y or by == y + h - 1:
                    ch = "+" if (bx in (x, x + w - 1) and by in (y, y + h - 1)) else ("-" if by in (y, y + h - 1) else "|")
                    self._layers[layer].set_char(bx, by, ch, border_color)
                else:
                    self._layers[layer].set_char(bx, by, " ", fill_color)

    def draw_bar(self, x: int, y: int, width: int, value: int, max_value: int,
                 filled_color: str = "red", empty_color: str = "#333333", layer: int = Layer.UI) -> None:
        if max_value <= 0:
            return
        filled = max(0, min(int((value / max_value) * width), width))
        for i in range(width):
            self._layers[layer].set_char(x + i, y, "|", filled_color if i < filled else empty_color)

    def render(self) -> None:
        if not self._initialized:
            self.initialize()
        if self._effects_manager:
            self._effects_manager.update()
        if self._use_gpu and self._render_mode in (RenderMode.KITTY, RenderMode.SIXEL):
            self._render_gpu()
        else:
            self._render_ascii()
        sys.stdout.flush()

    def _render_ascii(self) -> None:
        # Move cursor to home position (works with VT mode)
        sys.stdout.write("\x1b[H")
        for y in range(self.height):
            for x in range(self.width):
                char, color = self._get_composite_at(x, y)
                if self._capability.supports_truecolor and color != "default":
                    sys.stdout.write(f"\x1b[38;2;{self._normalize_color(color)}m{char}")
                else:
                    sys.stdout.write(char)
            if y < self.height - 1:
                sys.stdout.write("\n")
        sys.stdout.write("\x1b[0m")

    def _render_gpu(self) -> None:
        if not self._render_adapter:
            self._render_ascii()
            return
        self._render_adapter.begin_frame()
        if self._effects_manager:
            for light in self._effects_manager.get_lights():
                self._render_adapter.add_point_light(light["x"], light["y"], light["radius"], tuple(light["color"]), light["intensity"])
        self._render_ascii()
        self._render_adapter.end_frame()
        image_data = self._render_adapter.render_frame()
        if image_data:
            self._render_adapter.output_to_terminal(image_data)

    def _get_composite_at(self, x: int, y: int) -> tuple:
        for lid in [Layer.UI, Layer.EFFECTS, Layer.ENTITIES, Layer.MAP]:
            layer = self._layers.get(lid)
            if layer:
                sprite = layer.get_sprite(x, y)
                if sprite:
                    return (sprite.ascii_char, sprite.ascii_color)
                char, color = layer.get_char(x, y)
                if char != " ":
                    return (char, color)
        return (" ", "default")

    def _normalize_color(self, color: str) -> str:
        if color == "default":
            return "255;255;255"
        if color.startswith("#") and len(color) == 7:
            return f"{int(color[1:3],16)};{int(color[3:5],16)};{int(color[5:7],16)}"
        return {"white": "255;255;255", "black": "0;0;0", "red": "255;0;0", "green": "0;255;0",
                "blue": "0;0;255", "yellow": "255;255;0", "cyan": "0;255;255", "magenta": "255;0;255"}.get(color.lower(), "255;255;255")

    @property
    def effects(self):
        return self._effects_manager

    @property
    def is_gpu_active(self) -> bool:
        return self._use_gpu

    def set_ambient_light(self, r: float, g: float, b: float) -> None:
        if self._effects_manager:
            self._effects_manager.set_ambient_light(r, g, b)
        if self._render_adapter:
            self._render_adapter.set_ambient_light(r, g, b)

    def add_point_light(self, x: float, y: float, radius: float = 150, color: tuple = (1.0, 1.0, 1.0), intensity: float = 1.0) -> str:
        light_id = self._effects_manager.add_point_light(x, y, radius, color, intensity) if self._effects_manager else None
        if self._render_adapter:
            self._render_adapter.add_point_light(x, y, radius, color, intensity)
        return light_id or "temp_light"

    def spawn_particles(self, x: float, y: float, count: int = 10, **kwargs) -> None:
        if self._effects_manager:
            self._effects_manager.spawn_particles(x, y, count, **kwargs)


_layer_manager: Optional[LayerManager] = None


def get_layer_manager() -> LayerManager:
    global _layer_manager
    if _layer_manager is None:
        _layer_manager = LayerManager()
    return _layer_manager


def create_layer_manager(width: int = 80, height: int = 25) -> LayerManager:
    global _layer_manager
    _layer_manager = LayerManager(width, height)
    return _layer_manager


# Convenience functions
draw_char = lambda x, y, c, col="white", l=Layer.ENTITIES: get_layer_manager().draw_char(x, y, c, col, l)
draw_sprite = lambda x, y, n, l=Layer.ENTITIES: get_layer_manager().draw_sprite_at(x, y, n, l)
draw_text = lambda x, y, t, c="white", l=Layer.UI: get_layer_manager().draw_text(x, y, t, c, l)
draw_box = lambda x, y, w, h, b="#aaaaaa", f="#111111", l=Layer.UI: get_layer_manager().draw_box(x, y, w, h, b, f, l)
draw_bar = lambda x, y, w, v, m, fc="red", ec="#333333", l=Layer.UI: get_layer_manager().draw_bar(x, y, w, v, m, fc, ec, l)
clear_layer = lambda l: get_layer_manager().clear_layer(l)
