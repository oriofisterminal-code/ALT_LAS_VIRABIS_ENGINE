"""
ALT_LAS Engine - Sprite Loader
Loads PNG sprites and converts them to terminal-compatible formats.
Supports Sixel, Kitty, and ASCII fallback rendering.
"""

import os
import base64
import zlib
from typing import Optional, Tuple
from dataclasses import dataclass

try:
    from PIL import Image
    PIL_AVAILABLE = True
except ImportError:
    PIL_AVAILABLE = False

from src.render.terminal_detect import RenderMode, get_render_mode

BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
TEXTURES_DIR = os.path.join(BASE_DIR, "Content", "Textures")

SPRITE_CACHE: dict = {}


@dataclass
class Sprite:
    """Represents a loaded sprite with all render formats."""
    name: str
    width: int
    height: int
    pil_image: Optional["Image.Image"]
    sixel_data: Optional[bytes]
    kitty_data: Optional[bytes]
    ascii_char: str
    ascii_color: str
    cell_width: int = 1
    cell_height: int = 1


class SpriteLoader:
    """Loads and caches sprites for terminal rendering."""

    TILE_SIZE = 16

    def __init__(self, textures_dir: str = TEXTURES_DIR):
        self.textures_dir = textures_dir
        self._cache: dict[str, Sprite] = {}

    def load(self, sprite_path: str) -> Optional[Sprite]:
        """Load a sprite from file or cache."""
        if sprite_path in self._cache:
            return self._cache[sprite_path]

        full_path = self._resolve_path(sprite_path)
        if not full_path or not os.path.exists(full_path):
            return None

        if not PIL_AVAILABLE:
            return self._create_fallback_sprite(sprite_path)

        try:
            img = Image.open(full_path)
            if img.mode != "RGBA":
                img = img.convert("RGBA")

            sprite = Sprite(
                name=sprite_path,
                width=img.width,
                height=img.height,
                pil_image=img,
                sixel_data=None,
                kitty_data=None,
                ascii_char=self._extract_ascii_char(img),
                ascii_color=self._extract_dominant_color(img),
                cell_width=max(1, img.width // self.TILE_SIZE),
                cell_height=max(1, img.height // self.TILE_SIZE),
            )

            self._cache[sprite_path] = sprite
            return sprite

        except Exception as e:
            print(f"[SpriteLoader] Error loading {sprite_path}: {e}")
            return self._create_fallback_sprite(sprite_path)

    def _resolve_path(self, sprite_path: str) -> Optional[str]:
        """Resolve sprite path relative to textures directory."""
        if os.path.isabs(sprite_path):
            return sprite_path if os.path.exists(sprite_path) else None

        subdirs = ["Tiles", "NPCs", "Battle", "UI", ""]
        for subdir in subdirs:
            path = os.path.join(self.textures_dir, subdir, sprite_path)
            if os.path.exists(path):
                return path

        return None

    def _create_fallback_sprite(self, name: str) -> Sprite:
        """Create a fallback sprite for missing images."""
        return Sprite(
            name=name,
            width=self.TILE_SIZE,
            height=self.TILE_SIZE,
            pil_image=None,
            sixel_data=None,
            kitty_data=None,
            ascii_char="?",
            ascii_color="#ff00ff",
        )

    def _extract_ascii_char(self, img: "Image.Image") -> str:
        """Extract representative ASCII character from image."""
        if img.width <= 16 and img.height <= 16:
            return "█"
        elif img.width <= 32:
            return "▓"
        else:
            return "░"

    def _extract_dominant_color(self, img: "Image.Image") -> str:
        """Extract dominant color as hex string."""
        if not PIL_AVAILABLE:
            return "#ffffff"

        try:
            img_small = img.resize((1, 1))
            r, g, b, a = img_small.getpixel((0, 0))
            if a < 128:
                return "#333333"
            return f"#{r:02x}{g:02x}{b:02x}"
        except Exception:
            return "#ffffff"

    def get_sixel_data(self, sprite: Sprite) -> Optional[bytes]:
        """Generate Sixel data for a sprite."""
        if sprite.sixel_data:
            return sprite.sixel_data
        if not sprite.pil_image:
            return None

        try:
            sprite.sixel_data = self._image_to_sixel(sprite.pil_image)
            return sprite.sixel_data
        except Exception:
            return None

    def get_kitty_data(self, sprite: Sprite) -> Optional[bytes]:
        """Generate Kitty graphics protocol data for a sprite."""
        if sprite.kitty_data:
            return sprite.kitty_data
        if not sprite.pil_image:
            return None

        try:
            sprite.kitty_data = self._image_to_kitty(sprite.pil_image)
            return sprite.kitty_data
        except Exception:
            return None

    def _image_to_sixel(self, img: "Image.Image") -> bytes:
        """Convert PIL Image to Sixel format."""
        img_rgb = img.convert("RGB")
        width, height = img_rgb.size
        pixels = list(img_rgb.getdata())

        sixel = b"\x1bP0;0;0q"
        sixel += f'"1;1;{width};{height}'.encode()

        colors: dict[tuple, int] = {}
        color_palette: list[tuple] = []

        for pixel in pixels:
            if pixel not in colors:
                colors[pixel] = len(color_palette)
                color_palette.append(pixel)

        for i, (r, g, b) in enumerate(color_palette[:256]):
            sixel += f"#{i};2;{r//25};{g//25};{b//25}".encode()

        for y in range(height):
            for x in range(width):
                pixel = pixels[y * width + x]
                color_idx = colors.get(pixel, 0)
                sixel += f"#{color_idx}".encode()
                sixel += b"~"

            sixel += b"$"
            if y < height - 1:
                sixel += b"-"

        sixel += b"\x1b\\"
        return sixel

    def _image_to_kitty(self, img: "Image.Image") -> bytes:
        """Convert PIL Image to Kitty graphics protocol format."""
        img_png = img.convert("RGBA")

        from io import BytesIO
        buffer = BytesIO()
        img_png.save(buffer, format="PNG")
        png_data = buffer.getvalue()

        encoded = base64.b64encode(png_data).decode("ascii")

        chunks = []
        chunk_size = 4096
        for i in range(0, len(encoded), chunk_size):
            chunk = encoded[i:i + chunk_size]
            if i == 0:
                chunks.append(f"\x1b_Ga=T,f=100,m={1 if i + chunk_size < len(encoded) else 0};")
            else:
                chunks.append(f"\x1b_Gm={1 if i + chunk_size < len(encoded) else 0};")
            chunks.append(chunk)
            chunks.append("\x1b\\")

        return "".join(chunks).encode("utf-8")

    def list_sprites(self) -> list[str]:
        """List all available sprites in textures directory."""
        sprites = []
        if not os.path.exists(self.textures_dir):
            return sprites

        for subdir in ["Tiles", "NPCs", "Battle", "UI"]:
            subdir_path = os.path.join(self.textures_dir, subdir)
            if os.path.exists(subdir_path):
                for filename in os.listdir(subdir_path):
                    if filename.lower().endswith((".png", ".jpg", ".jpeg", ".gif")):
                        sprites.append(f"{subdir}/{filename}")

        return sprites

    def clear_cache(self) -> None:
        """Clear the sprite cache."""
        self._cache.clear()


_sprite_loader: Optional[SpriteLoader] = None


def get_sprite_loader() -> SpriteLoader:
    """Get or create the global sprite loader instance."""
    global _sprite_loader
    if _sprite_loader is None:
        _sprite_loader = SpriteLoader()
    return _sprite_loader
