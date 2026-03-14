"""
ALT_LAS Engine - Normal Map Generator
2D sprite'lar için normal map üretimi.

Görev: GFX-001 Normal Mapping
Sorumlu: Kemal (GPU/Shader) + Deniz (Grafik Tasarım)
"""

import os
import math
from typing import Optional, Tuple, List
from dataclasses import dataclass

try:
    from PIL import Image
    import numpy as np
    HAS_PIL = True
except ImportError:
    HAS_PIL = False
    Image = None
    np = None


@dataclass
class NormalMapConfig:
    """Normal map generation configuration."""
    strength: float = 1.0
    blur_radius: int = 0
    invert_height: bool = False
    edge_detection: bool = True
    smooth_normals: bool = True


class NormalMapper:
    """
    Convert sprites/heightmaps to normal maps.
    
    Features:
    - Height map to normal map conversion
    - Sprite edge detection for depth
    - Sobel filter gradient calculation
    - Configurable strength and blur
    - Batch processing support
    
    Usage:
        mapper = NormalMapper()
        
        # From sprite (uses alpha as height)
        normal_map = mapper.from_sprite("player.png")
        normal_map.save("player_normal.png")
        
        # From height map
        normal_map = mapper.from_heightmap("height.png", strength=2.0)
    """
    
    def __init__(self, config: Optional[NormalMapConfig] = None):
        self.config = config or NormalMapConfig()
    
    def from_heightmap(
        self,
        heightmap: "Image.Image",
        strength: Optional[float] = None
    ) -> "Image.Image":
        """
        Convert a grayscale height map to a normal map.
        
        The height map represents surface elevation:
        - White (255) = High/raised
        - Black (0) = Low/sunken
        
        Args:
            heightmap: Grayscale PIL Image
            strength: Normal strength multiplier (default from config)
        
        Returns:
            RGB normal map as PIL Image (RGB format, ready for GPU)
        """
        if not HAS_PIL:
            raise ImportError("PIL/Pillow and NumPy required for normal map generation")
        
        strength = strength or self.config.strength
        
        # Convert to grayscale numpy array
        gray = heightmap.convert('L')
        arr = np.array(gray, dtype=np.float32) / 255.0
        
        # Invert if configured
        if self.config.invert_height:
            arr = 1.0 - arr
        
        # Apply blur if configured
        if self.config.blur_radius > 0:
            from scipy import ndimage
            arr = ndimage.gaussian_filter(arr, sigma=self.config.blur_radius)
        
        # Sobel operators for gradient calculation
        # These detect horizontal and vertical edges
        sobel_x = np.array([
            [-1, 0, 1],
            [-2, 0, 2],
            [-1, 0, 1]
        ], dtype=np.float32)
        
        sobel_y = np.array([
            [-1, -2, -1],
            [ 0,  0,  0],
            [ 1,  2,  1]
        ], dtype=np.float32)
        
        # Calculate gradients
        from scipy import ndimage
        dx = ndimage.convolve(arr, sobel_x) * strength
        dy = ndimage.convolve(arr, sobel_y) * strength
        
        # Normal vector components
        # dz is constant (surface facing camera)
        dz = np.ones_like(arr)
        
        # Normalize
        length = np.sqrt(dx**2 + dy**2 + dz**2)
        length = np.maximum(length, 0.0001)  # Avoid division by zero
        
        nx = dx / length
        ny = dy / length
        nz = dz / length
        
        # Convert from [-1, 1] to [0, 255] range for RGB storage
        # In normal maps: RGB = XYZ mapped to 0-255
        nx_rgb = ((nx + 1) * 127.5).astype(np.uint8)
        ny_rgb = ((ny + 1) * 127.5).astype(np.uint8)
        nz_rgb = ((nz + 1) * 127.5).astype(np.uint8)
        
        # Stack into RGB image
        # R = X, G = Y, B = Z (OpenGL convention)
        normal_arr = np.stack([nx_rgb, ny_rgb, nz_rgb], axis=2)
        
        return Image.fromarray(normal_arr, 'RGB')
    
    def from_sprite(
        self,
        sprite: "Image.Image",
        edge_detection: Optional[bool] = None,
        strength: Optional[float] = None
    ) -> "Image.Image":
        """
        Generate normal map from sprite by treating edges as depth.
        
        The alpha channel determines the shape:
        - Opaque pixels = raised surface
        - Transparent pixels = low/no surface
        
        This creates a "beveled" look perfect for 2D game sprites.
        
        Args:
            sprite: RGBA PIL Image
            edge_detection: Use edge detection for depth (default from config)
            strength: Normal strength (default from config)
        
        Returns:
            RGB normal map as PIL Image
        """
        if not HAS_PIL:
            raise ImportError("PIL/Pillow and NumPy required for normal map generation")
        
        edge_detection = edge_detection if edge_detection is not None else self.config.edge_detection
        strength = strength or self.config.strength
        
        # Extract alpha channel
        if sprite.mode == 'RGBA':
            alpha = sprite.split()[-1]
        elif sprite.mode == 'LA':
            alpha = sprite.split()[-1]
        else:
            # No alpha, treat entire image as opaque
            alpha = Image.new('L', sprite.size, 255)
        
        # Convert alpha to height map
        # Opaque = high (255), Transparent = low (0)
        height = alpha
        
        if edge_detection:
            # Edge detection mode: detect alpha edges for bevel effect
            arr = np.array(height, dtype=np.float32)
            
            # Sobel for edge detection
            from scipy import ndimage
            sobel = np.array([
                [-1, -1, -1],
                [-1,  8, -1],
                [-1, -1, -1]
            ], dtype=np.float32)
            
            edges = ndimage.convolve(arr / 255.0, sobel)
            
            # Combine original alpha with edges
            height_arr = arr / 255.0 + edges * 0.3
            height_arr = np.clip(height_arr, 0, 1) * 255
            height = Image.fromarray(height_arr.astype(np.uint8), 'L')
        
        return self.from_heightmap(height, strength)
    
    def from_depth_buffer(
        self,
        depth_buffer: np.ndarray,
        strength: Optional[float] = None
    ) -> "Image.Image":
        """
        Generate normal map from a depth buffer.
        
        Args:
            depth_buffer: 2D numpy array (0-1 range)
            strength: Normal strength
        
        Returns:
            RGB normal map as PIL Image
        """
        if not HAS_PIL:
            raise ImportError("PIL/Pillow and NumPy required")
        
        strength = strength or self.config.strength
        
        # Convert to 0-255 range
        depth_img = Image.fromarray((depth_buffer * 255).astype(np.uint8), 'L')
        
        return self.from_heightmap(depth_img, strength)
    
    def generate_simple(
        self,
        size: Tuple[int, int],
        direction: str = "up"
    ) -> "Image.Image":
        """
        Generate a simple uniform normal map.
        
        Useful for flat surfaces that still need to respond to lighting.
        
        Args:
            size: (width, height) tuple
            direction: Normal direction ("up", "down", "left", "right", "topleft", etc.)
        
        Returns:
            RGB normal map as PIL Image
        """
        if not HAS_PIL:
            raise ImportError("PIL/Pillow required")
        
        # Default normal pointing out of screen (Z+)
        nx, ny, nz = 0.0, 0.0, 1.0
        
        # Direction mapping
        directions = {
            "up": (0.0, -1.0, 0.5),
            "down": (0.0, 1.0, 0.5),
            "left": (-1.0, 0.0, 0.5),
            "right": (1.0, 0.0, 0.5),
            "topleft": (-0.7, -0.7, 0.5),
            "topright": (0.7, -0.7, 0.5),
            "bottomleft": (-0.7, 0.7, 0.5),
            "bottomright": (0.7, 0.7, 0.5),
        }
        
        if direction.lower() in directions:
            nx, ny, nz = directions[direction.lower()]
        
        # Normalize
        length = math.sqrt(nx**2 + ny**2 + nz**2)
        nx, ny, nz = nx/length, ny/length, nz/length
        
        # Convert to RGB
        r = int((nx + 1) * 127.5)
        g = int((ny + 1) * 127.5)
        b = int((nz + 1) * 127.5)
        
        return Image.new('RGB', size, (r, g, b))
    
    def batch_process(
        self,
        input_dir: str,
        output_dir: str,
        pattern: str = "*.png"
    ) -> List[str]:
        """
        Process all matching images in a directory.
        
        Args:
            input_dir: Source directory
            output_dir: Output directory (created if not exists)
            pattern: File pattern to match (default: *.png)
        
        Returns:
            List of generated normal map paths
        """
        if not HAS_PIL:
            raise ImportError("PIL/Pillow required")
        
        import glob
        
        os.makedirs(output_dir, exist_ok=True)
        
        generated = []
        
        for path in glob.glob(os.path.join(input_dir, pattern)):
            try:
                # Load sprite
                sprite = Image.open(path).convert('RGBA')
                
                # Generate normal map
                normal = self.from_sprite(sprite)
                
                # Save with _normal suffix
                name = os.path.splitext(os.path.basename(path))[0]
                output_path = os.path.join(output_dir, f"{name}_normal.png")
                normal.save(output_path)
                
                generated.append(output_path)
                
            except Exception as e:
                print(f"Error processing {path}: {e}")
        
        return generated


# Convenience functions
def create_normal_map(source, **kwargs) -> "Image.Image":
    """
    Quick normal map creation.
    
    Args:
        source: PIL Image or file path
        **kwargs: Options passed to NormalMapper
    
    Returns:
        RGB normal map as PIL Image
    """
    mapper = NormalMapper()
    
    if isinstance(source, str):
        source = Image.open(source)
    
    if source.mode in ('RGBA', 'LA'):
        return mapper.from_sprite(source, **kwargs)
    else:
        return mapper.from_heightmap(source, **kwargs)


if __name__ == "__main__":
    # CLI tool
    import argparse
    
    parser = argparse.ArgumentParser(
        description="Generate normal maps from sprites/heightmaps"
    )
    parser.add_argument("input", help="Input image or directory")
    parser.add_argument("-o", "--output", help="Output file or directory")
    parser.add_argument("-s", "--strength", type=float, default=1.0,
                        help="Normal strength (default: 1.0)")
    parser.add_argument("-b", "--blur", type=int, default=0,
                        help="Blur radius (default: 0)")
    parser.add_argument("--no-edge", action="store_true",
                        help="Disable edge detection")
    parser.add_argument("--invert", action="store_true",
                        help="Invert height map")
    
    args = parser.parse_args()
    
    config = NormalMapConfig(
        strength=args.strength,
        blur_radius=args.blur,
        invert_height=args.invert,
        edge_detection=not args.no_edge
    )
    
    mapper = NormalMapper(config)
    
    if os.path.isdir(args.input):
        output = args.output or args.input + "_normals"
        generated = mapper.batch_process(args.input, output)
        print(f"Generated {len(generated)} normal maps in {output}")
    else:
        sprite = Image.open(args.input).convert('RGBA')
        normal = mapper.from_sprite(sprite)
        
        output = args.output
        if not output:
            name = os.path.splitext(args.input)[0]
            output = f"{name}_normal.png"
        
        normal.save(output)
        print(f"Saved: {output}")
