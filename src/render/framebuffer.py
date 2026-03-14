"""
ALT_LAS Engine - Framebuffer System
ModernGL Framebuffer Object wrapper for post-processing.

Görev: GFX-002 Post-Processing (Bloom)
Sorumlu: Kemal (GPU/Shader)
"""

from typing import Optional, List, Tuple, Any
from dataclasses import dataclass
from enum import Enum, auto

try:
    import moderngl
    HAS_MODERNGL = True
except ImportError:
    HAS_MODERNGL = False
    moderngl = None


class FramebufferAttachment(Enum):
    """Types of framebuffer attachments."""
    COLOR = auto()
    DEPTH = auto()
    STENCIL = auto()
    DEPTH_STENCIL = auto()


@dataclass
class FramebufferConfig:
    """Framebuffer configuration."""
    width: int
    height: int
    color_attachments: int = 1
    has_depth: bool = False
    has_stencil: bool = False
    float_precision: bool = True  # RGBA16F vs RGBA8
    filter_linear: bool = True
    generate_mipmaps: bool = False


class Framebuffer:
    """
    ModernGL Framebuffer Object wrapper.
    
    Features:
    - Multiple color attachments (MRT support)
    - Depth/stencil buffer
    - Easy resize
    - Texture access for post-processing
    
    Usage:
        fbo = Framebuffer(ctx, 1920, 1080, color_attachments=2)
        
        # Render to FBO
        fbo.bind()
        # ... render scene ...
        fbo.unbind()
        
        # Use textures in post-process
        color_texture = fbo.get_color_attachment(0)
    
    """
    
    def __init__(
        self,
        ctx,
        width: int,
        height: int,
        color_attachments: int = 1,
        has_depth: bool = False,
        has_stencil: bool = False,
        float_precision: bool = True,
        filter_linear: bool = True
    ):
        if not HAS_MODERNGL:
            raise ImportError("ModernGL required for Framebuffer")
        
        self.ctx = ctx
        self.width = width
        self.height = height
        
        # Settings
        self._color_count = color_attachments
        self._has_depth = has_depth
        self._has_stencil = has_stencil
        self._float_precision = float_precision
        self._filter_linear = filter_linear
        
        # Resources
        self._fbo: Optional[Any] = None
        self._color_textures: List[Any] = []
        self._depth_texture: Optional[Any] = None
        
        # Create FBO
        self._create()
    
    def _create(self) -> None:
        """Create framebuffer with attachments."""
        # Determine texture format
        if self._float_precision:
            color_format = 'RGBA16F'
        else:
            color_format = 'RGBA8'
        
        # Filter mode
        filter_mode = moderngl.LINEAR if self._filter_linear else moderngl.NEAREST
        
        # Create color attachments
        for i in range(self._color_count):
            texture = self.ctx.texture(
                (self.width, self.height),
                4,  # RGBA
                components=4,
                dtype='f2' if self._float_precision else 'f1'
            )
            texture.filter = (filter_mode, filter_mode)
            texture.repeat_x = False
            texture.repeat_y = False
            self._color_textures.append(texture)
        
        # Create depth/stencil if needed
        depth_attachment = None
        if self._has_depth and self._has_stencil:
            self._depth_texture = self.ctx.depth_texture(
                (self.width, self.height),
                samples=0
            )
            depth_attachment = self._depth_texture
        elif self._has_depth:
            self._depth_texture = self.ctx.depth_texture(
                (self.width, self.height),
                samples=0
            )
            depth_attachment = self._depth_texture
        
        # Create framebuffer
        self._fbo = self.ctx.framebuffer(
            color_attachments=self._color_textures,
            depth_attachment=depth_attachment
        )
    
    @property
    def fbo(self):
        """Get the ModernGL framebuffer object."""
        return self._fbo
    
    @property
    def size(self) -> Tuple[int, int]:
        """Get framebuffer size."""
        return (self.width, self.height)
    
    def bind(self) -> None:
        """Bind framebuffer for rendering."""
        if self._fbo:
            self._fbo.use()
    
    def unbind(self) -> None:
        """Unbind, return to default framebuffer."""
        self.ctx.screen.use()
    
    def clear(self, r: float = 0.0, g: float = 0.0, b: float = 0.0, a: float = 1.0) -> None:
        """Clear framebuffer."""
        if self._fbo:
            self._fbo.clear(r, g, b, a)
    
    def get_color_attachment(self, index: int = 0):
        """
        Get color texture by index.
        
        Args:
            index: Color attachment index (0-based)
        
        Returns:
            ModernGL Texture or None
        """
        if 0 <= index < len(self._color_textures):
            return self._color_textures[index]
        return None
    
    def get_depth_texture(self):
        """Get depth texture."""
        return self._depth_texture
    
    def resize(self, width: int, height: int) -> None:
        """
        Resize framebuffer textures.
        
        Args:
            width: New width
            height: New height
        """
        if width == self.width and height == self.height:
            return
        
        self.width = width
        self.height = height
        
        # Release old resources
        self.release()
        
        # Recreate with new size
        self._create()
    
    def read_pixels(self, x: int = 0, y: int = 0, width: int = None, height: int = None) -> bytes:
        """
        Read pixels from framebuffer.
        
        Args:
            x, y: Start position
            width, height: Region size (default: full framebuffer)
        
        Returns:
            Pixel data as bytes
        """
        if width is None:
            width = self.width
        if height is None:
            height = self.height
        
        return self._fbo.read(
            viewport=(x, y, width, height),
            components=4
        )
    
    def blit_to_screen(self) -> None:
        """Blit framebuffer to default framebuffer (screen)."""
        # Note: ModernGL doesn't have direct blit, need custom implementation
        pass
    
    def release(self) -> None:
        """Release OpenGL resources."""
        for texture in self._color_textures:
            if texture:
                texture.release()
        self._color_textures.clear()
        
        if self._depth_texture:
            self._depth_texture.release()
            self._depth_texture = None
        
        if self._fbo:
            self._fbo.release()
            self._fbo = None
    
    def __enter__(self):
        """Context manager entry."""
        self.bind()
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit."""
        self.unbind()
        return False


class FramebufferPool:
    """
    Pool of framebuffers for ping-pong operations.
    
    Useful for multi-pass effects like blur.
    """
    
    def __init__(self, ctx, width: int, height: int, pool_size: int = 2):
        self.ctx = ctx
        self.width = width
        self.height = height
        self._pool: List[Framebuffer] = []
        self._current = 0
        
        for _ in range(pool_size):
            self._pool.append(Framebuffer(ctx, width, height))
    
    def get_current(self) -> Framebuffer:
        """Get current framebuffer."""
        return self._pool[self._current]
    
    def get_next(self) -> Framebuffer:
        """Get next framebuffer and advance index."""
        self._current = (self._current + 1) % len(self._pool)
        return self._pool[self._current]
    
    def swap(self) -> Tuple[Framebuffer, Framebuffer]:
        """
        Swap and return (source, destination) pair.
        
        Returns:
            Tuple of (current source, current destination)
        """
        src = self._pool[self._current]
        self._current = (self._current + 1) % len(self._pool)
        dst = self._pool[self._current]
        return (src, dst)
    
    def resize(self, width: int, height: int) -> None:
        """Resize all framebuffers in pool."""
        self.width = width
        self.height = height
        for fbo in self._pool:
            fbo.resize(width, height)
    
    def release(self) -> None:
        """Release all framebuffers."""
        for fbo in self._pool:
            fbo.release()
        self._pool.clear()


# Convenience functions
def create_framebuffer(ctx, width: int, height: int, **kwargs) -> Framebuffer:
    """Create a framebuffer with default settings."""
    return Framebuffer(ctx, width, height, **kwargs)


def create_ping_pong_pair(ctx, width: int, height: int) -> Tuple[Framebuffer, Framebuffer]:
    """Create a pair of framebuffers for ping-pong rendering."""
    return (
        Framebuffer(ctx, width, height),
        Framebuffer(ctx, width, height)
    )
