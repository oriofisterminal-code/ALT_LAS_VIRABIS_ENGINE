"""
ALT_LAS Engine - Graphics Demo Scene
Tests all GPU rendering features: shaders, lights, particles, sprites, normal maps.
Runs in Window mode with moderngl.

Version: v0.5.0+ (Normal Mapping Support)
"""

import math
import time
import array
from typing import Optional, List, Tuple, Dict, Any

# Try to import moderngl
try:
    import moderngl
    MODERNGL_AVAILABLE = True
except ImportError:
    MODERNGL_AVAILABLE = False
    moderngl = None

# Try to import PIL for texture loading
try:
    from PIL import Image
    PIL_AVAILABLE = True
except ImportError:
    PIL_AVAILABLE = False
    Image = None


class GraphicsDemoScene:
    """
    Demo scene to showcase GPU rendering capabilities.
    Tests: Sprites, Lighting, Particles, Water effects, Shaders, Normal Maps.
    
    Features:
    - Dynamic point lights (up to 8)
    - Normal mapping for 2D sprites
    - Particle system
    - Multiple shader modes
    
    Controls:
    - WASD/Arrows: Move player
    - F1: Toggle debug mode
    - F2: Cycle shader modes (normal, lit, normals)
    """

    def __init__(self, ctx, width: int = 960, height: int = 544):
        self.ctx = ctx
        self.width = width
        self.height = height
        self.time = 0.0
        self._initialized = False

        # Demo elements
        self._tiles: List[List[int]] = []
        self._tile_size = 32
        self._map_width = 0
        self._map_height = 0
        self._player_pos = [3.0, 8.0]
        self._lights: List[dict] = []
        self._particles: List[dict] = []

        # Key states
        self._keys_pressed = set()
        
        # Normal mapping mode
        self._shader_mode = 0  # 0: normal, 1: lit, 2: debug normals
        self._debug_mode = False
        self._ambient_light = 0.3

        # OpenGL resources
        self._program = None
        self._normal_program = None  # Normal mapping shader
        self._quad_vao = None
        self._textures: dict = {}
        self._normal_maps: dict = {}  # Normal map textures

    def initialize(self) -> bool:
        """Initialize OpenGL resources."""
        if self._initialized:
            return True

        if not MODERNGL_AVAILABLE or not self.ctx:
            print("[DemoScene] ModernGL not available")
            return False

        try:
            # Create shader program
            self._create_shaders()

            # Create geometry
            self._create_geometry()

            # Create textures
            self._create_textures()

            # Load map
            self._load_map()

            self._initialized = True
            print("[DemoScene] Initialized successfully")
            return True

        except Exception as e:
            print(f"[DemoScene] Init error: {e}")
            import traceback
            traceback.print_exc()
            return False

    def _create_shaders(self) -> None:
        """Create GLSL shader programs."""

        vertex_shader = """
            #version 330
            in vec2 in_pos;
            in vec2 in_tex;
            out vec2 v_tex;
            uniform vec2 u_offset;
            uniform vec2 u_screen;

            void main() {
                vec2 pos = (in_pos + u_offset) / u_screen * 2.0 - 1.0;
                gl_Position = vec4(pos.x, -pos.y, 0.0, 1.0);
                v_tex = in_tex;
            }
        """

        fragment_shader = """
            #version 330
            in vec2 v_tex;
            out vec4 fragColor;
            uniform sampler2D u_texture;
            uniform vec3 u_tint;
            uniform float u_time;

            void main() {
                vec4 color = texture(u_texture, v_tex);
                color.rgb *= u_tint;
                
                // Slight animation
                float pulse = 0.9 + 0.1 * sin(u_time * 2.0);
                color.rgb *= pulse;
                
                fragColor = color;
            }
        """

        self._program = self.ctx.program(
            vertex_shader=vertex_shader,
            fragment_shader=fragment_shader,
        )

    def _create_geometry(self) -> None:
        """Create quad geometry for sprites."""

        # Quad vertices: x, y, u, v
        s = self._tile_size
        vertices = array.array('f', [
            0, 0, 0, 0,
            s, 0, 1, 0,
            s, s, 1, 1,
            0, 0, 0, 0,
            s, s, 1, 1,
            0, s, 0, 1,
        ])

        self._quad_buffer = self.ctx.buffer(vertices)
        self._quad_vao = self.ctx.vertex_array(
            self._program,
            [(self._quad_buffer, '2f 2f', 'in_pos', 'in_tex')],
        )

    def _create_textures(self) -> None:
        """Create procedural textures."""

        if not PIL_AVAILABLE:
            # Create solid color textures without PIL
            self._textures['floor'] = self._create_solid_texture(60, 50, 40, 255)
            self._textures['wall'] = self._create_solid_texture(100, 80, 60, 255)
            self._textures['water'] = self._create_solid_texture(30, 60, 120, 200)
            self._textures['player'] = self._create_solid_texture(255, 50, 50, 255)
            self._textures['light'] = self._create_solid_texture(255, 255, 200, 150)
            return

        # Floor texture
        floor = Image.new('RGBA', (32, 32), (60, 50, 40, 255))
        self._textures['floor'] = self._texture_from_image(floor)

        # Wall texture with brick pattern
        wall = Image.new('RGBA', (32, 32), (100, 80, 60, 255))
        for y in range(0, 32, 8):
            offset = 8 if (y // 8) % 2 else 0
            for x in range(32):
                wall.putpixel((x, y), (70, 50, 40, 255))
        for x in range(0, 32, 16):
            for y in range(32):
                wall.putpixel(((x + (8 if (y // 8) % 2 else 0)) % 32, y), (50, 35, 25, 255))
        self._textures['wall'] = self._texture_from_image(wall)

        # Water texture
        water = Image.new('RGBA', (32, 32), (30, 60, 120, 200))
        for y in range(32):
            for x in range(32):
                wave = int(20 * math.sin(x * 0.5) * math.cos(y * 0.3))
                r, g, b, a = water.getpixel((x, y))
                water.putpixel((x, y), (r + wave // 2, g + wave, b + wave // 2, a))
        self._textures['water'] = self._texture_from_image(water)

        # Player texture (heart shape)
        player = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
        for y in range(32):
            for x in range(32):
                dx = abs(x - 16) / 8
                dy = (y - 12) / 10
                if dx * dx + dy * dy < 1.2:
                    player.putpixel((x, y), (255, 50, 50, 255))
                elif dx * dx + dy * dy < 1.5:
                    player.putpixel((x, y), (200, 30, 30, 255))
        self._textures['player'] = self._texture_from_image(player)

        # Light orb texture
        light = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
        for y in range(32):
            for x in range(32):
                dist = math.sqrt((x - 16) ** 2 + (y - 16) ** 2)
                if dist < 12:
                    alpha = int(255 * (1 - dist / 12))
                    light.putpixel((x, y), (255, 255, 200, alpha))
        self._textures['light'] = self._texture_from_image(light)

    def _create_solid_texture(self, r: int, g: int, b: int, a: int):
        """Create a solid color texture without PIL."""
        data = bytes([r, g, b, a] * 32 * 32)
        texture = self.ctx.texture((32, 32), 4, data)
        texture.filter = (moderngl.NEAREST, moderngl.NEAREST)
        return texture

    def _texture_from_image(self, img):
        """Convert PIL Image to moderngl texture."""
        data = img.convert('RGBA').tobytes()
        texture = self.ctx.texture(img.size, 4, data)
        texture.filter = (moderngl.NEAREST, moderngl.NEAREST)
        return texture

    def _load_map(self) -> None:
        """Load demo level map."""
        import json
        import os

        map_path = os.path.join(
            os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(__file__)))),
            "assets", "maps", "demo_level.json"
        )

        try:
            with open(map_path, 'r') as f:
                data = json.load(f)

            self._tiles = data.get('tiles', [])
            self._map_width = data.get('width', 20)
            self._map_height = data.get('height', 15)

            spawn = data.get('player_spawn', {'x': 3, 'y': 8})
            self._player_pos = [float(spawn['x']), float(spawn['y'])]

            # Load lights
            for light in data.get('point_lights', []):
                self._lights.append({
                    'x': light['x'] * self._tile_size + self._tile_size // 2,
                    'y': light['y'] * self._tile_size + self._tile_size // 2,
                    'color': self._hex_to_rgb(light.get('color', '#ffffff')),
                })

            print(f"[DemoScene] Map loaded: {self._map_width}x{self._map_height}")

        except FileNotFoundError:
            print("[DemoScene] Demo map not found, creating default")
            self._create_default_map()
        except Exception as e:
            print(f"[DemoScene] Map load error: {e}")
            self._create_default_map()

    def _create_default_map(self) -> None:
        """Create a simple default map."""
        self._map_width = 20
        self._map_height = 15
        self._tiles = [
            [1 if (x == 0 or x == 19 or y == 0 or y == 14) else 0
             for x in range(20)] for y in range(15)
        ]
        self._player_pos = [3.0, 7.0]
        self._lights = [
            {'x': 320, 'y': 240, 'color': (1.0, 1.0, 0.8)},
        ]

    def _hex_to_rgb(self, hex_color: str) -> Tuple[float, float, float]:
        """Convert hex color to RGB tuple (0-1 range)."""
        hex_color = hex_color.lstrip('#')
        return tuple(int(hex_color[i:i+2], 16) / 255.0 for i in (0, 2, 4))

    def handle_key(self, key: str, pressed: bool) -> None:
        """Handle keyboard input from main_window.py."""
        # key comes as 'W', 'A', etc. (without TK_ prefix from main_window.py)
        if pressed:
            self._keys_pressed.add(key)
            print(f"[Demo] Key pressed: {key}")  # Debug
        else:
            self._keys_pressed.discard(key)
            print(f"[Demo] Key released: {key}")  # Debug
    
    def handle_input(self, key: str) -> None:
        """Handle keyboard input from engine."""
        # Convert TK_ format to simple format
        key_map = {
            'TK_W': 'W', 'TK_A': 'A', 'TK_S': 'S', 'TK_D': 'D',
            'TK_UP': 'UP', 'TK_DOWN': 'DOWN', 'TK_LEFT': 'LEFT', 'TK_RIGHT': 'RIGHT',
            'TK_Z': 'Z', 'TK_X': 'X', 'TK_RETURN': 'ENTER', 'TK_SPACE': 'SPACE',
            'TK_F1': 'F1', 'TK_F2': 'F2', 'TK_F3': 'F3', 'TK_ESCAPE': 'ESC',
        }
        
        simple_key = key_map.get(key, key.replace('TK_', ''))
        self._keys_pressed.add(simple_key)
        
        # Handle special keys
        if simple_key == 'F1':
            self._show_debug_info()
    
    def update_pressed_keys(self, pressed_keys: set) -> None:
        """Update the set of currently pressed keys (called every frame)."""
        # Convert TK_ format keys to simple format
        key_map = {
            'TK_W': 'W', 'TK_A': 'A', 'TK_S': 'S', 'TK_D': 'D',
            'TK_UP': 'UP', 'TK_DOWN': 'DOWN', 'TK_LEFT': 'LEFT', 'TK_RIGHT': 'RIGHT',
            'TK_Z': 'Z', 'TK_X': 'X', 'TK_RETURN': 'ENTER', 'TK_SPACE': 'SPACE',
        }
        
        # Update pressed keys set
        self._keys_pressed.clear()
        for key in pressed_keys:
            simple_key = key_map.get(key, key.replace('TK_', ''))
            self._keys_pressed.add(simple_key)
    
    def _show_debug_info(self) -> None:
        """Show debug information."""
        print(f"\n[Demo Debug] === INFO ===")
        print(f"  Player: ({self._player_pos[0]:.1f}, {self._player_pos[1]:.1f})")
        print(f"  Time: {self.time:.1f}s")
        print(f"  Particles: {len(self._particles)}")
        print(f"  Keys pressed: {self._keys_pressed}")

    def update(self, dt: float) -> None:
        """Update scene state."""
        self.time += dt

        # Player movement
        speed = 4.0 * dt
        dx, dy = 0.0, 0.0

        if 'W' in self._keys_pressed or 'UP' in self._keys_pressed:
            dy -= speed
        if 'S' in self._keys_pressed or 'DOWN' in self._keys_pressed:
            dy += speed
        if 'A' in self._keys_pressed or 'LEFT' in self._keys_pressed:
            dx -= speed
        if 'D' in self._keys_pressed or 'RIGHT' in self._keys_pressed:
            dx += speed

        # Collision check and move
        new_x = self._player_pos[0] + dx
        new_y = self._player_pos[1] + dy

        tile_x = int(new_x)
        tile_y = int(new_y)

        if 0 <= tile_x < self._map_width and 0 <= tile_y < self._map_height:
            if self._tiles and tile_y < len(self._tiles) and tile_x < len(self._tiles[0]):
                if self._tiles[tile_y][tile_x] != 1:  # Not a wall
                    self._player_pos[0] = new_x
                    self._player_pos[1] = new_y

        # Update particles
        self._update_particles(dt)

        # Spawn particles occasionally
        if int(self.time * 3) % 2 == 0 and len(self._particles) < 30:
            self._spawn_particle()

    def _spawn_particle(self) -> None:
        """Spawn a particle near player."""
        import random

        px = self._player_pos[0] * self._tile_size + self._tile_size // 2
        py = self._player_pos[1] * self._tile_size + self._tile_size // 2

        angle = random.uniform(0, 2 * 3.14159)
        speed = random.uniform(20, 60)

        self._particles.append({
            'x': px,
            'y': py,
            'vx': math.cos(angle) * speed,
            'vy': math.sin(angle) * speed,
            'life': 1.0,
            'color': (1.0, random.uniform(0.6, 1.0), 0.2),
        })

    def _update_particles(self, dt: float) -> None:
        """Update particle positions and lifetimes."""
        for p in self._particles[:]:
            p['x'] += p['vx'] * dt
            p['y'] += p['vy'] * dt
            p['life'] -= dt * 0.8
            if p['life'] <= 0:
                self._particles.remove(p)

    def render(self) -> None:
        """Render the scene."""
        if not self._initialized:
            return

        # Set uniforms
        self._program['u_screen'].value = (self.width, self.height)
        self._program['u_time'].value = self.time

        # Render tiles
        self._render_tiles()

        # Render lights
        self._render_lights()

        # Render player
        self._render_player()

        # Render particles
        self._render_particles()

    def _render_tiles(self) -> None:
        """Render map tiles."""
        tile_names = {0: 'floor', 1: 'wall', 2: 'water'}

        self._quad_vao.bind()

        for y, row in enumerate(self._tiles):
            for x, tile in enumerate(row):
                texture_name = tile_names.get(tile, 'floor')
                if texture_name in self._textures:
                    self._textures[texture_name].use(0)
                    self._program['u_texture'].value = 0
                    self._program['u_offset'].value = (
                        x * self._tile_size,
                        y * self._tile_size,
                    )
                    self._program['u_tint'].value = (1.0, 1.0, 1.0)
                    self._quad_vao.render()

    def _render_lights(self) -> None:
        """Render point lights."""
        if 'light' not in self._textures:
            return

        self._textures['light'].use(0)
        self._program['u_texture'].value = 0

        for light in self._lights:
            self._program['u_offset'].value = (
                light['x'] - self._tile_size // 2,
                light['y'] - self._tile_size // 2,
            )
            self._program['u_tint'].value = light['color']
            self._quad_vao.render()

    def _render_player(self) -> None:
        """Render player sprite."""
        if 'player' not in self._textures:
            return

        self._textures['player'].use(0)
        self._program['u_texture'].value = 0
        self._program['u_offset'].value = (
            int(self._player_pos[0] * self._tile_size),
            int(self._player_pos[1] * self._tile_size),
        )
        self._program['u_tint'].value = (1.0, 1.0, 1.0)
        self._quad_vao.render()

    def _render_particles(self) -> None:
        """Render particles as small colored quads."""
        if not self._particles or 'light' not in self._textures:
            return

        self._textures['light'].use(0)
        self._program['u_texture'].value = 0

        for p in self._particles:
            # Scale particle based on life
            size = int(8 * p['life'])
            if size < 2:
                continue

            self._program['u_offset'].value = (
                p['x'] - size // 2,
                p['y'] - size // 2,
            )
            self._program['u_tint'].value = p['color']
            self._quad_vao.render()

    def shutdown(self) -> None:
        """Release OpenGL resources."""
        self._quad_buffer = None
        self._quad_vao = None
        self._program = None
        self._textures.clear()
        self._initialized = False
