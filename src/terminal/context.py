"""
ALT_LAS Engine - OpenGL Context
Headless OpenGL context for GPU rendering without display.
Uses moderngl for modern OpenGL API and glcontext for headless support.
"""

import os
import sys
from typing import Optional, Tuple
import struct
import io

# Try to import moderngl, fallback to None if unavailable
try:
    import moderngl
    MODERNGL_AVAILABLE = True
except ImportError:
    MODERNGL_AVAILABLE = False
    moderngl = None

# Try to import PIL for image output
try:
    from PIL import Image
    PIL_AVAILABLE = True
except ImportError:
    PIL_AVAILABLE = False
    Image = None


class GLContext:
    """Manages OpenGL context for headless GPU rendering."""

    def __init__(self, width: int = 960, height: int = 544):
        self.width = width
        self.height = height
        self._ctx: Optional[moderngl.Context] = None
        self._framebuffer: Optional[moderngl.Framebuffer] = None
        self._texture: Optional[moderngl.Texture] = None
        self._depth: Optional[moderngl.Texture] = None
        self._initialized = False
        self._error: Optional[str] = None

    @property
    def is_available(self) -> bool:
        """Check if GPU rendering is available."""
        return MODERNGL_AVAILABLE and self._initialized

    @property
    def error(self) -> Optional[str]:
        """Get last error message."""
        return self._error

    def initialize(self) -> bool:
        """Initialize headless OpenGL context."""
        if not MODERNGL_AVAILABLE:
            self._error = "moderngl not installed"
            return False

        try:
            # Try to create standalone context (works on most systems)
            self._ctx = moderngl.create_context(standalone=True)
            self._setup_framebuffer()
            self._initialized = True
            return True
        except Exception as e:
            self._error = f"Context creation failed: {str(e)}"
            return False

    def _setup_framebuffer(self) -> None:
        """Setup framebuffer for offscreen rendering."""
        if not self._ctx:
            return

        # Create color texture
        self._texture = self._ctx.texture(
            (self.width, self.height),
            components=4,
            dtype='f1'  # unsigned byte
        )

        # Create depth texture for proper 3D rendering
        self._depth = self._ctx.depth_texture((self.width, self.height))

        # Create framebuffer
        self._framebuffer = self._ctx.framebuffer(
            color_attachments=[self._texture],
            depth_attachment=self._depth
        )

    def make_current(self) -> None:
        """Make this context current."""
        if self._ctx and self._framebuffer:
            self._framebuffer.use()

    def clear(self, r: float = 0.0, g: float = 0.0, b: float = 0.0, a: float = 1.0) -> None:
        """Clear the framebuffer with specified color."""
        if self._ctx:
            self._ctx.clear(r, g, b, a)

    def read_pixels(self) -> Optional[bytes]:
        """Read framebuffer pixels as RGBA bytes."""
        if not self._framebuffer:
            return None

        try:
            return self._framebuffer.read(components=4, dtype='f1')
        except Exception:
            return None

    def to_pil_image(self) -> Optional["Image.Image"]:
        """Convert framebuffer to PIL Image."""
        if not PIL_AVAILABLE or not self._framebuffer:
            return None

        try:
            data = self.read_pixels()
            if data:
                img = Image.frombytes('RGBA', (self.width, self.height), data)
                # OpenGL origin is bottom-left, flip to top-left
                return img.transpose(Image.FLIP_TOP_BOTTOM)
        except Exception:
            pass
        return None

    def to_png_bytes(self) -> Optional[bytes]:
        """Convert framebuffer to PNG bytes for Kitty protocol."""
        img = self.to_pil_image()
        if img:
            buffer = io.BytesIO()
            img.save(buffer, format='PNG')
            return buffer.getvalue()
        return None

    def resize(self, width: int, height: int) -> None:
        """Resize the framebuffer."""
        if width == self.width and height == self.height:
            return

        self.width = width
        self.height = height

        if self._ctx:
            # Release old resources
            if self._framebuffer:
                self._framebuffer.release()
            if self._texture:
                self._texture.release()
            if self._depth:
                self._depth.release()

            # Create new framebuffer
            self._setup_framebuffer()

    def shutdown(self) -> None:
        """Release OpenGL resources."""
        if self._framebuffer:
            self._framebuffer.release()
            self._framebuffer = None
        if self._texture:
            self._texture.release()
            self._texture = None
        if self._depth:
            self._depth.release()
            self._depth = None
        # Note: moderngl context is released on GC
        self._ctx = None
        self._initialized = False


# Global context instance
_gl_context: Optional[GLContext] = None


def create_headless_context(width: int = 960, height: int = 544) -> GLContext:
    """Create or get global headless OpenGL context."""
    global _gl_context
    if _gl_context is None:
        _gl_context = GLContext(width, height)
    elif _gl_context.width != width or _gl_context.height != height:
        _gl_context.resize(width, height)
    return _gl_context


def get_gl_context() -> Optional[GLContext]:
    """Get current GL context without creating new one."""
    return _gl_context


def is_gpu_available() -> bool:
    """Check if GPU rendering is available."""
    if not MODERNGL_AVAILABLE:
        return False
    ctx = create_headless_context()
    return ctx.initialize() and ctx.is_available
