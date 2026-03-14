"""
ALT_LAS Engine - Render Adapter
Bridges GPU rendering with terminal output.
Implements 3-level fallback: GPU Shader -> Pre-render -> ASCII.
"""

import sys
import os
import base64
import io
from typing import Optional, Tuple, List
from enum import Enum, auto

# Local imports
from src.terminal.context import GLContext, create_headless_context, MODERNGL_AVAILABLE
from src.render.terminal_detect import RenderMode, get_terminal_capability

# Try to import PIL
try:
    from PIL import Image, ImageDraw, ImageFilter, ImageEnhance, ImageChops
    PIL_AVAILABLE = True
except ImportError:
    PIL_AVAILABLE = False
    Image = None
    ImageFilter = None
    ImageEnhance = None
    ImageChops = None


class RenderBackend(Enum):
    """Available render backends."""
    GPU_SHADER = auto()      # ModernGL with GLSL shaders
    GPU_PRERENDER = auto()   # PIL-based pre-rendered effects
    TERMINAL_ASCII = auto()  # Plain ASCII fallback


class RenderAdapter:
    """
    Adapter between GPU renderer and terminal output.
    Handles automatic fallback and format conversion.
    """

    # Kitty graphics protocol limits
    KITTY_CHUNK_SIZE = 4096  # Max bytes per chunk

    def __init__(self, width: int = 960, height: int = 544):
        self.width = width
        self.height = height
        self._backend: RenderBackend = RenderBackend.TERMINAL_ASCII
        self._gl_context: Optional[GLContext] = None
        self._capability = get_terminal_capability()
        self._render_mode = self._capability.render_mode
        self._initialized = False
        self._last_frame: Optional[bytes] = None
        self._frame_count = 0

        # Effect settings
        self._ambient_light = (0.3, 0.3, 0.4)  # Dim blue ambient
        self._point_lights: List[dict] = []
        self._glow_enabled = False
        self._water_effect = False

    @property
    def backend(self) -> RenderBackend:
        """Get current render backend."""
        return self._backend

    @property
    def is_gpu_active(self) -> bool:
        """Check if GPU rendering is active."""
        return self._backend == RenderBackend.GPU_SHADER

    @property
    def supports_graphics(self) -> bool:
        """Check if terminal supports graphics output."""
        return self._render_mode in (RenderMode.KITTY, RenderMode.SIXEL, RenderMode.ITERM2)

    def initialize(self) -> bool:
        """Initialize the best available render backend."""
        if self._initialized:
            return True

        # Try GPU Shader backend first
        if self._try_gpu_backend():
            self._backend = RenderBackend.GPU_SHADER
            self._initialized = True
            return True

        # Fallback to pre-render (PIL effects)
        if PIL_AVAILABLE:
            self._backend = RenderBackend.GPU_PRERENDER
            self._initialized = True
            return True

        # Final fallback to ASCII
        self._backend = RenderBackend.TERMINAL_ASCII
        self._initialized = True
        return True

    def _try_gpu_backend(self) -> bool:
        """Try to initialize GPU rendering backend."""
        if not MODERNGL_AVAILABLE:
            return False

        self._gl_context = create_headless_context(self.width, self.height)
        return self._gl_context.initialize() and self._gl_context.is_available

    def begin_frame(self) -> None:
        """Begin a new render frame."""
        if self._backend == RenderBackend.GPU_SHADER and self._gl_context:
            self._gl_context.make_current()
            self._gl_context.clear(0.0, 0.0, 0.05, 1.0)  # Dark blue background

    def end_frame(self) -> None:
        """End current frame and prepare for output."""
        self._frame_count += 1

    def render_frame(self) -> Optional[bytes]:
        """Render current frame and return image data."""
        if self._backend == RenderBackend.GPU_SHADER and self._gl_context:
            return self._gl_context.to_png_bytes()
        return None

    def output_to_terminal(self, image_data: Optional[bytes] = None) -> None:
        """Output rendered frame to terminal."""
        if not self.supports_graphics:
            return  # ASCII mode handled by layer_manager

        if self._render_mode == RenderMode.KITTY:
            self._output_kitty(image_data)
        elif self._render_mode == RenderMode.SIXEL:
            self._output_sixel(image_data)
        elif self._render_mode == RenderMode.ITERM2:
            self._output_iterm2(image_data)

    def _output_kitty(self, image_data: Optional[bytes]) -> None:
        """Output using Kitty graphics protocol."""
        if not image_data:
            return

        # Encode to base64
        b64_data = base64.b64encode(image_data).decode('ascii')

        # Kitty escape codes
        # ESC_G = start graphics, a=T (transmit), t=d (PNG), f=100 (RGBA)
        # ESC_\ = end graphics

        # Send in chunks for large images
        total = len(b64_data)
        offset = 0
        chunk_num = 0

        while offset < total:
            chunk = b64_data[offset:offset + self.KITTY_CHUNK_SIZE]
            more = 1 if offset + self.KITTY_CHUNK_SIZE < total else 0

            # Build control string
            # a=T: transmit, t=d: PNG format, f=100: RGBA
            # m=1: more data coming, m=0: last chunk
            ctrl = f"a=T,t=d,f=100,m={more}"

            if chunk_num == 0:
                # First chunk: clear previous image
                sys.stdout.write(f"\x1b_G{ctrl};{chunk}\x1b\\")
            else:
                # Continue chunk
                sys.stdout.write(f"\x1b_Gm={more};{chunk}\x1b\\")

            offset += self.KITTY_CHUNK_SIZE
            chunk_num += 1

        sys.stdout.flush()

    def _output_sixel(self, image_data: Optional[bytes]) -> None:
        """Output using Sixel protocol."""
        if not image_data or not PIL_AVAILABLE:
            return

        # Convert PNG to Sixel
        try:
            img = Image.open(io.BytesIO(image_data))
            # Sixel output would go here
            # For now, use Kitty-style output (many terminals support both)
            self._output_kitty(image_data)
        except Exception:
            pass

    def _output_iterm2(self, image_data: Optional[bytes]) -> None:
        """Output using iTerm2 inline images."""
        if not image_data:
            return

        # iTerm2 protocol: ESC ] 1337 ; File = [arguments] : base64_data BEL
        b64_data = base64.b64encode(image_data).decode('ascii')
        sys.stdout.write(f"\x1b]1337;File=inline=1:{b64_data}\x07")
        sys.stdout.flush()

    # --- Light Effects API ---

    def set_ambient_light(self, r: float, g: float, b: float) -> None:
        """Set ambient light color (0.0 - 1.0)."""
        self._ambient_light = (r, g, b)

    def add_point_light(self, x: float, y: float, radius: float,
                        color: Tuple[float, float, float] = (1.0, 1.0, 1.0),
                        intensity: float = 1.0) -> int:
        """Add a point light. Returns light ID."""
        light = {
            "id": len(self._point_lights),
            "x": x,
            "y": y,
            "radius": radius,
            "color": color,
            "intensity": intensity
        }
        self._point_lights.append(light)
        return light["id"]

    def remove_point_light(self, light_id: int) -> None:
        """Remove a point light by ID."""
        self._point_lights = [l for l in self._point_lights if l["id"] != light_id]

    def clear_point_lights(self) -> None:
        """Remove all point lights."""
        self._point_lights.clear()

    def set_glow_enabled(self, enabled: bool) -> None:
        """Enable/disable glow/bloom effect."""
        self._glow_enabled = enabled

    def set_water_effect(self, enabled: bool, speed: float = 1.0) -> None:
        """Enable/disable water wave effect."""
        self._water_effect = enabled

    def apply_pil_effects(self, image: "Image.Image") -> "Image.Image":
        """Apply light/glow effects using PIL (fallback mode)."""
        if not PIL_AVAILABLE or ImageChops is None:
            return image

        result = image.copy()

        # Apply ambient lighting (multiply blend)
        if self._ambient_light != (1.0, 1.0, 1.0):
            overlay = Image.new('RGBA', image.size, (
                int(self._ambient_light[0] * 255),
                int(self._ambient_light[1] * 255),
                int(self._ambient_light[2] * 255),
                255
            ))
            # Multiply blend mode
            result = ImageChops.multiply(result, overlay)

        # Apply glow effect
        if self._glow_enabled and ImageFilter is not None and ImageEnhance is not None:
            # Create bloom by blurring bright areas
            glow_layer = result.filter(ImageFilter.GaussianBlur(radius=5))
            enhancer = ImageEnhance.Brightness(glow_layer)
            glow_layer = enhancer.enhance(1.5)
            # Add glow to original
            result = ImageChops.add(result, glow_layer)

        return result

    def shutdown(self) -> None:
        """Release all resources."""
        if self._gl_context:
            self._gl_context.shutdown()
            self._gl_context = None
        self._initialized = False


# Global adapter instance
_render_adapter: Optional[RenderAdapter] = None


def get_render_adapter() -> RenderAdapter:
    """Get or create global render adapter."""
    global _render_adapter
    if _render_adapter is None:
        _render_adapter = RenderAdapter()
    return _render_adapter


def create_render_adapter(width: int = 960, height: int = 544) -> RenderAdapter:
    """Create a new render adapter with specified dimensions."""
    global _render_adapter
    _render_adapter = RenderAdapter(width, height)
    return _render_adapter


def get_current_backend() -> RenderBackend:
    """Get current render backend."""
    adapter = get_render_adapter()
    if not adapter._initialized:
        adapter.initialize()
    return adapter.backend
