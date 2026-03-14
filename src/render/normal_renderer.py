"""
ALT_LAS Engine - Normal Map Renderer
Normal map ile dinamik ışıklandırma için render pipeline.

Görev: GFX-001 Normal Mapping
Sorumlu: Kemal (GPU/Shader)
"""

import os
import math
import array
from typing import Optional, Dict, List, Tuple, Any
from dataclasses import dataclass, field

try:
    import moderngl
    HAS_MODERNGL = True
except ImportError:
    HAS_MODERNGL = False
    moderngl = None

try:
    from PIL import Image
    HAS_PIL = True
except ImportError:
    HAS_PIL = False
    Image = None


@dataclass
class Light:
    """Point light for normal mapping."""
    x: float
    y: float
    color: Tuple[float, float, float] = (1.0, 1.0, 1.0)
    intensity: float = 1.0
    radius: float = 200.0
    
    def as_uniform(self) -> Dict[str, Any]:
        """Convert to uniform dict."""
        return {
            'pos': (self.x, self.y),
            'color': self.color,
            'intensity': self.intensity,
            'radius': self.radius
        }


@dataclass
class NormalSprite:
    """Sprite with normal map."""
    name: str
    texture: Any  # moderngl.Texture
    normal_map: Optional[Any] = None
    specular_map: Optional[Any] = None
    width: int = 32
    height: int = 32


class NormalRenderer:
    """
    Normal map renderer for 2D sprites.
    
    Features:
    - Multiple point lights (up to 8)
    - Normal map support
    - Specular highlights
    - Ambient lighting
    - Debug normal visualization
    
    Usage:
        renderer = NormalRenderer(ctx, width, height)
        
        # Load sprites
        renderer.load_sprite("player", "player.png", "player_normal.png")
        
        # Setup lights
        renderer.add_light(Light(x=200, y=150, color=(1, 0.9, 0.8)))
        
        # Render
        renderer.begin_frame()
        renderer.draw_sprite("player", x=100, y=100)
        renderer.end_frame()
    """
    
    MAX_LIGHTS = 8
    
    def __init__(
        self,
        ctx,
        width: int,
        height: int,
        shader_dir: Optional[str] = None
    ):
        if not HAS_MODERNGL:
            raise ImportError("ModernGL required for NormalRenderer")
        
        self.ctx = ctx
        self.width = width
        self.height = height
        
        # Find shader directory
        if shader_dir is None:
            shader_dir = os.path.join(
                os.path.dirname(__file__)
            )
        self.shader_dir = shader_dir
        
        # Resources
        self._program = None
        self._quad_vao = None
        self._quad_buffer = None
        self._sprites: Dict[str, NormalSprite] = {}
        
        # Lights
        self._lights: List[Light] = []
        self._ambient = 0.3
        self._normal_strength = 1.0
        self._global_tint = (1.0, 1.0, 1.0)
        self._debug_normals = False
        
        # Initialize
        self._init_shaders()
        self._init_geometry()
    
    def _init_shaders(self) -> None:
        """Load and compile shaders."""
        # Vertex shader
        vert_path = os.path.join(self.shader_dir, "normal_map.vert")
        frag_path = os.path.join(self.shader_dir, "normal_map.frag")
        
        vertex_source = """
            #version 330
            in vec2 in_pos;
            in vec2 in_tex;
            out vec2 v_tex;
            out vec2 v_world_pos;
            uniform vec2 u_offset;
            uniform vec2 u_screen;
            void main() {
                vec2 pos = (in_pos + u_offset) / u_screen * 2.0 - 1.0;
                gl_Position = vec4(pos.x, -pos.y, 0.0, 1.0);
                v_tex = in_tex;
                v_world_pos = in_pos + u_offset;
            }
        """
        
        fragment_source = """
            #version 330
            in vec2 v_tex;
            in vec2 v_world_pos;
            out vec4 fragColor;
            
            uniform sampler2D u_texture;
            uniform sampler2D u_normal_map;
            uniform vec2 u_light_pos[8];
            uniform vec3 u_light_color[8];
            uniform float u_light_intensity[8];
            uniform float u_light_radius[8];
            uniform int u_light_count;
            uniform float u_ambient;
            uniform float u_normal_strength;
            uniform vec3 u_global_tint;
            uniform bool u_has_normal_map;
            uniform bool u_debug_normals;
            
            void main() {
                vec4 color = texture(u_texture, v_tex);
                if (color.a < 0.1) discard;
                
                vec3 normal;
                if (u_has_normal_map) {
                    normal = texture(u_normal_map, v_tex).rgb;
                    normal = normalize(normal * 2.0 - 1.0);
                    normal.xy *= u_normal_strength;
                    normal = normalize(normal);
                } else {
                    normal = vec3(0.0, 0.0, 1.0);
                }
                
                if (u_debug_normals) {
                    fragColor = vec4(normal * 0.5 + 0.5, 1.0);
                    return;
                }
                
                vec3 final = color.rgb * u_ambient;
                
                for (int i = 0; i < u_light_count && i < 8; i++) {
                    vec2 to_light = u_light_pos[i] - v_world_pos;
                    float dist = length(to_light);
                    float atten = 1.0 - smoothstep(0.0, u_light_radius[i], dist);
                    
                    if (atten > 0.0) {
                        vec3 light_dir = normalize(vec3(normalize(to_light), 0.5));
                        float diff = max(dot(normal, light_dir), 0.0);
                        final += color.rgb * diff * u_light_color[i] * u_light_intensity[i] * atten;
                    }
                }
                
                final *= u_global_tint;
                fragColor = vec4(final, color.a);
            }
        """
        
        # Try loading from files first
        try:
            if os.path.exists(vert_path) and os.path.exists(frag_path):
                with open(vert_path, 'r') as f:
                    vertex_source = f.read()
                with open(frag_path, 'r') as f:
                    fragment_source = f.read()
        except Exception:
            pass
        
        self._program = self.ctx.program(
            vertex_shader=vertex_source,
            fragment_shader=fragment_source
        )
    
    def _init_geometry(self) -> None:
        """Create quad geometry."""
        # Unit quad vertices: x, y, u, v
        vertices = array.array('f', [
            0, 0, 0, 0,
            1, 0, 1, 0,
            1, 1, 1, 1,
            0, 0, 0, 0,
            1, 1, 1, 1,
            0, 1, 0, 1,
        ])
        
        self._quad_buffer = self.ctx.buffer(vertices)
        self._quad_vao = self.ctx.vertex_array(
            self._program,
            [(self._quad_buffer, '2f 2f', 'in_pos', 'in_tex')]
        )
    
    def load_sprite(
        self,
        name: str,
        texture_path: str,
        normal_path: Optional[str] = None,
        specular_path: Optional[str] = None
    ) -> bool:
        """
        Load a sprite with optional normal and specular maps.
        
        Args:
            name: Unique sprite identifier
            texture_path: Path to diffuse texture
            normal_path: Path to normal map (optional)
            specular_path: Path to specular map (optional)
        
        Returns:
            True if successful
        """
        if not HAS_PIL:
            print("PIL required for texture loading")
            return False
        
        try:
            # Load diffuse texture
            diffuse = Image.open(texture_path).convert('RGBA')
            width, height = diffuse.size
            texture_data = diffuse.tobytes()
            
            texture = self.ctx.texture((width, height), 4, texture_data)
            texture.filter = (moderngl.NEAREST, moderngl.NEAREST)
            
            # Load normal map if provided
            normal_map = None
            if normal_path and os.path.exists(normal_path):
                normal_img = Image.open(normal_path).convert('RGB')
                normal_data = normal_img.tobytes()
                normal_map = self.ctx.texture((width, height), 3, normal_data)
                normal_map.filter = (moderngl.NEAREST, moderngl.NEAREST)
            
            # Load specular map if provided
            specular_map = None
            if specular_path and os.path.exists(specular_path):
                spec_img = Image.open(specular_path).convert('L')
                spec_data = spec_img.tobytes()
                specular_map = self.ctx.texture((width, height), 1, spec_data)
            
            self._sprites[name] = NormalSprite(
                name=name,
                texture=texture,
                normal_map=normal_map,
                specular_map=specular_map,
                width=width,
                height=height
            )
            
            return True
            
        except Exception as e:
            print(f"Error loading sprite '{name}': {e}")
            return False
    
    def create_sprite_from_images(
        self,
        name: str,
        diffuse: "Image.Image",
        normal: Optional["Image.Image"] = None
    ) -> bool:
        """Create sprite from PIL Images."""
        try:
            width, height = diffuse.size
            
            # Diffuse texture
            diffuse_rgba = diffuse.convert('RGBA')
            texture = self.ctx.texture((width, height), 4, diffuse_rgba.tobytes())
            texture.filter = (moderngl.NEAREST, moderngl.NEAREST)
            
            # Normal map
            normal_map = None
            if normal:
                normal_rgb = normal.convert('RGB')
                normal_map = self.ctx.texture((width, height), 3, normal_rgb.tobytes())
                normal_map.filter = (moderngl.NEAREST, moderngl.NEAREST)
            
            self._sprites[name] = NormalSprite(
                name=name,
                texture=texture,
                normal_map=normal_map,
                width=width,
                height=height
            )
            
            return True
            
        except Exception as e:
            print(f"Error creating sprite '{name}': {e}")
            return False
    
    def add_light(self, light: Light) -> None:
        """Add a point light."""
        if len(self._lights) < self.MAX_LIGHTS:
            self._lights.append(light)
    
    def remove_light(self, index: int) -> None:
        """Remove a light by index."""
        if 0 <= index < len(self._lights):
            self._lights.pop(index)
    
    def clear_lights(self) -> None:
        """Remove all lights."""
        self._lights.clear()
    
    def set_ambient(self, ambient: float) -> None:
        """Set ambient light level (0.0 - 1.0)."""
        self._ambient = max(0.0, min(1.0, ambient))
    
    def set_normal_strength(self, strength: float) -> None:
        """Set normal map strength."""
        self._normal_strength = strength
    
    def set_global_tint(self, r: float, g: float, b: float) -> None:
        """Set global color tint."""
        self._global_tint = (r, g, b)
    
    def set_debug_normals(self, enabled: bool) -> None:
        """Enable/disable normal visualization."""
        self._debug_normals = enabled
    
    def begin_frame(self) -> None:
        """Prepare for a new frame."""
        pass
    
    def draw_sprite(
        self,
        name: str,
        x: float,
        y: float,
        scale: float = 1.0,
        flip_x: bool = False,
        flip_y: bool = False
    ) -> bool:
        """
        Draw a sprite with normal mapping.
        
        Args:
            name: Sprite identifier
            x: X position (pixels)
            y: Y position (pixels)
            scale: Scale factor
            flip_x: Flip horizontally
            flip_y: Flip vertically
        
        Returns:
            True if successful
        """
        if name not in self._sprites:
            return False
        
        sprite = self._sprites[name]
        
        # Bind program
        self._program.use()
        
        # Set texture uniforms
        sprite.texture.use(0)
        self._program['u_texture'].value = 0
        
        # Normal map
        has_normal = sprite.normal_map is not None
        if has_normal:
            sprite.normal_map.use(1)
            self._program['u_normal_map'].value = 1
        
        self._program['u_has_normal_map'].value = has_normal
        
        # Position and screen uniforms
        scaled_w = sprite.width * scale
        scaled_h = sprite.height * scale
        
        self._program['u_offset'].value = (x, y)
        self._program['u_screen'].value = (self.width, self.height)
        
        # Lighting uniforms
        self._program['u_ambient'].value = self._ambient
        self._program['u_normal_strength'].value = self._normal_strength
        self._program['u_global_tint'].value = self._global_tint
        self._program['u_debug_normals'].value = self._debug_normals
        self._program['u_light_count'].value = len(self._lights)
        
        # Light arrays
        for i, light in enumerate(self._lights):
            self._program[f'u_light_pos[{i}]'].value = (light.x, light.y)
            self._program[f'u_light_color[{i}]'].value = light.color
            self._program[f'u_light_intensity[{i}]'].value = light.intensity
            self._program[f'u_light_radius[{i}]'].value = light.radius
        
        # Update quad buffer with scaled size
        vertices = array.array('f', [
            0, 0, 0, 0,
            scaled_w, 0, 1, 0,
            scaled_w, scaled_h, 1, 1,
            0, 0, 0, 0,
            scaled_w, scaled_h, 1, 1,
            0, scaled_h, 0, 1,
        ])
        
        if flip_x:
            # Flip texture U coordinates
            for i in range(6):
                vertices[i*4 + 2] = 1.0 - vertices[i*4 + 2]
        
        if flip_y:
            # Flip texture V coordinates
            for i in range(6):
                vertices[i*4 + 3] = 1.0 - vertices[i*4 + 3]
        
        self._quad_buffer.write(vertices)
        
        # Draw
        self._quad_vao.render()
        
        return True
    
    def end_frame(self) -> None:
        """Finalize frame."""
        pass
    
    def get_sprite_size(self, name: str) -> Optional[Tuple[int, int]]:
        """Get sprite dimensions."""
        if name in self._sprites:
            sprite = self._sprites[name]
            return (sprite.width, sprite.height)
        return None
    
    def unload_sprite(self, name: str) -> None:
        """Remove a sprite from memory."""
        if name in self._sprites:
            sprite = self._sprites[name]
            sprite.texture.release()
            if sprite.normal_map:
                sprite.normal_map.release()
            if sprite.specular_map:
                sprite.specular_map.release()
            del self._sprites[name]
    
    def shutdown(self) -> None:
        """Release all resources."""
        for name in list(self._sprites.keys()):
            self.unload_sprite(name)
        
        self._quad_buffer.release()
        self._program.release()


# Convenience function
def create_normal_renderer(ctx, width: int, height: int) -> NormalRenderer:
    """Create a NormalRenderer instance."""
    return NormalRenderer(ctx, width, height)
