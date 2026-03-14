"""
ALT_LAS Engine - Post-Process Manager
Bloom and post-processing effects pipeline.

Görev: GFX-002 Post-Processing (Bloom)
Sorumlu: Kemal (GPU), Deniz (Grafik), Selin (UX)
"""

import os
import json
from typing import Optional, Dict, Any, Tuple
from dataclasses import dataclass, field

try:
    import moderngl
    HAS_MODERNGL = True
except ImportError:
    HAS_MODERNGL = False
    moderngl = None


@dataclass
class BloomSettings:
    """Bloom effect settings."""
    enabled: bool = True
    intensity: float = 0.8
    threshold: float = 0.7
    soft_threshold: float = 0.1
    blur_passes: int = 2
    quality: int = 1  # 0: low, 1: medium, 2: high
    
    # Tone mapping
    tone_mapping: int = 1  # 0: none, 1: reinhard, 2: filmic, 3: ACES
    exposure: float = 1.0
    
    # Debug
    debug_show_bloom: bool = False


@dataclass 
class PostProcessConfig:
    """Post-process pipeline configuration."""
    bloom: BloomSettings = field(default_factory=BloomSettings)
    
    @classmethod
    def from_json(cls, path: str) -> 'PostProcessConfig':
        """Load configuration from JSON file."""
        config = cls()
        if os.path.exists(path):
            with open(path, 'r') as f:
                data = json.load(f)
            
            if 'bloom' in data:
                bloom_data = data['bloom']
                config.bloom = BloomSettings(
                    enabled=bloom_data.get('enabled', True),
                    intensity=bloom_data.get('intensity', 0.8),
                    threshold=bloom_data.get('threshold', 0.7),
                    soft_threshold=bloom_data.get('soft_threshold', 0.1),
                    blur_passes=bloom_data.get('blur_passes', 2),
                    quality=bloom_data.get('quality', 1),
                    tone_mapping=bloom_data.get('tone_mapping', 1),
                    exposure=bloom_data.get('exposure', 1.0),
                )
        return config


class PostProcessManager:
    """
    Post-processing effects pipeline for ALT_LAS Engine.
    
    Features:
    - Bloom/Glow effect (Celeste-style)
    - Tone mapping (Reinhard, Filmic, ACES)
    - Configurable quality levels
    - Debug visualization
    
    Pipeline:
    1. Render scene to FBO
    2. Extract bright pixels
    3. Blur (ping-pong)
    4. Composite with original
    
    Usage:
        post_fx = PostProcessManager(ctx, width, height)
        
        # Begin frame (captures to FBO)
        post_fx.begin_frame()
        
        # ... render your scene here ...
        
        # End frame (applies post-processing)
        post_fx.end_frame()
    """
    
    def __init__(
        self,
        ctx,
        width: int,
        height: int,
        config: Optional[PostProcessConfig] = None,
        shader_dir: Optional[str] = None
    ):
        if not HAS_MODERNGL:
            raise ImportError("ModernGL required for PostProcessManager")
        
        self.ctx = ctx
        self.width = width
        self.height = height
        
        # Configuration
        self.config = config or PostProcessConfig()
        
        # Find shader directory
        if shader_dir is None:
            shader_dir = os.path.dirname(__file__)
        self.shader_dir = shader_dir
        
        # Framebuffers
        self._scene_fbo = None
        self._bright_fbo = None
        self._ping_fbo = None
        self._pong_fbo = None
        
        # Shaders
        self._bright_program = None
        self._blur_program = None
        self._bloom_program = None
        
        # Geometry
        self._quad_vao = None
        self._quad_buffer = None
        
        # Initialize
        self._init_framebuffers()
        self._init_shaders()
        self._init_geometry()
    
    def _init_framebuffers(self) -> None:
        """Create framebuffers for post-processing."""
        from src.render.framebuffer import Framebuffer
        
        # Scene FBO (full resolution, 16-bit float)
        self._scene_fbo = Framebuffer(
            self.ctx, self.width, self.height,
            float_precision=True
        )
        
        # Bloom FBOs (half resolution for performance)
        bloom_w = self.width // 2
        bloom_h = self.height // 2
        
        # Bright pixel extraction result
        self._bright_fbo = Framebuffer(
            self.ctx, bloom_w, bloom_h,
            float_precision=True
        )
        
        # Ping-pong for blur passes
        self._ping_fbo = Framebuffer(
            self.ctx, bloom_w, bloom_h,
            float_precision=True
        )
        self._pong_fbo = Framebuffer(
            self.ctx, bloom_w, bloom_h,
            float_precision=True
        )
    
    def _init_shaders(self) -> None:
        """Load and compile shaders."""
        # Vertex shader source (shared)
        vertex_source = """
            #version 330
            in vec2 in_pos;
            in vec2 in_tex;
            out vec2 v_tex;
            void main() {
                gl_Position = vec4(in_pos, 0.0, 1.0);
                v_tex = in_tex;
            }
        """
        
        # Brightness extraction shader
        bright_frag = self._load_shader('bright_extract.frag', """
            #version 330
            in vec2 v_tex;
            out vec4 fragColor;
            uniform sampler2D u_texture;
            uniform float u_threshold;
            uniform float u_soft_threshold;
            
            float luminance(vec3 color) {
                return dot(color, vec3(0.2126, 0.7152, 0.0722));
            }
            
            void main() {
                vec4 color = texture(u_texture, v_tex);
                float brightness = luminance(color.rgb);
                
                float soft_knee = u_threshold - u_soft_threshold;
                float contribution = 0.0;
                
                if (brightness > u_threshold) {
                    contribution = 1.0;
                } else if (brightness > soft_knee) {
                    contribution = smoothstep(soft_knee, u_threshold, brightness);
                }
                
                fragColor = color * contribution;
            }
        """)
        
        self._bright_program = self.ctx.program(
            vertex_shader=vertex_source,
            fragment_shader=bright_frag
        )
        
        # Blur shader
        blur_frag = self._load_shader('blur.frag', """
            #version 330
            in vec2 v_tex;
            out vec4 fragColor;
            uniform sampler2D u_texture;
            uniform vec2 u_direction;
            uniform vec2 u_texel_size;
            
            const float weights[3] = float[](
                0.2270270270,
                0.3162162162,
                0.0702702703
            );
            const float offsets[2] = float[](1.3846153846, 3.2307692308);
            
            void main() {
                vec4 color = texture(u_texture, v_tex) * weights[0];
                vec2 texel_offset = u_direction * u_texel_size;
                
                for (int i = 1; i < 3; i++) {
                    color += texture(u_texture, v_tex + texel_offset * offsets[i-1]) * weights[i];
                    color += texture(u_texture, v_tex - texel_offset * offsets[i-1]) * weights[i];
                }
                
                fragColor = color;
            }
        """)
        
        self._blur_program = self.ctx.program(
            vertex_shader=vertex_source,
            fragment_shader=blur_frag
        )
        
        # Bloom composite shader
        bloom_frag = self._load_shader('bloom.frag', """
            #version 330
            in vec2 v_tex;
            out vec4 fragColor;
            uniform sampler2D u_scene;
            uniform sampler2D u_bloom;
            uniform float u_intensity;
            uniform bool u_enabled;
            uniform bool u_debug_bloom;
            uniform float u_exposure;
            uniform int u_tone_mapping;
            
            vec3 reinhard(vec3 color) {
                return color / (color + vec3(1.0));
            }
            
            void main() {
                vec4 scene = texture(u_scene, v_tex);
                
                if (!u_enabled) {
                    fragColor = scene;
                    return;
                }
                
                vec4 bloom = texture(u_bloom, v_tex);
                
                if (u_debug_bloom) {
                    fragColor = vec4(bloom.rgb * u_intensity, 1.0);
                    return;
                }
                
                vec3 result = scene.rgb + bloom.rgb * u_intensity;
                result *= u_exposure;
                
                if (u_tone_mapping == 1) {
                    result = reinhard(result);
                }
                
                result = clamp(result, 0.0, 1.0);
                result = pow(result, vec3(1.0/2.2));
                
                fragColor = vec4(result, scene.a);
            }
        """)
        
        self._bloom_program = self.ctx.program(
            vertex_shader=vertex_source,
            fragment_shader=bloom_frag
        )
    
    def _load_shader(self, filename: str, default_source: str) -> str:
        """Load shader from file or use default."""
        path = os.path.join(self.shader_dir, filename)
        if os.path.exists(path):
            with open(path, 'r') as f:
                return f.read()
        return default_source
    
    def _init_geometry(self) -> None:
        """Create fullscreen quad geometry."""
        import array
        
        # Fullscreen quad: position (x, y), texcoord (u, v)
        # Covers NDC space from -1 to 1
        vertices = array.array('f', [
            -1, -1, 0, 0,  # Bottom-left
             1, -1, 1, 0,  # Bottom-right
             1,  1, 1, 1,  # Top-right
            -1, -1, 0, 0,  # Bottom-left (duplicate for triangle strip)
             1,  1, 1, 1,  # Top-right (duplicate)
            -1,  1, 0, 1,  # Top-left
        ])
        
        self._quad_buffer = self.ctx.buffer(vertices)
        
        # Create VAO for each program
        self._bright_vao = self.ctx.vertex_array(
            self._bright_program,
            [(self._quad_buffer, '2f 2f', 'in_pos', 'in_tex')]
        )
        self._blur_vao = self.ctx.vertex_array(
            self._blur_program,
            [(self._quad_buffer, '2f 2f', 'in_pos', 'in_tex')]
        )
        self._bloom_vao = self.ctx.vertex_array(
            self._bloom_program,
            [(self._quad_buffer, '2f 2f', 'in_pos', 'in_tex')]
        )
    
    def begin_frame(self) -> None:
        """Start frame capture to FBO."""
        self._scene_fbo.bind()
        self._scene_fbo.clear()
    
    def end_frame(self) -> None:
        """Apply post-processing and render to screen."""
        # Unbind scene FBO
        self._scene_fbo.unbind()
        
        if self.config.bloom.enabled:
            self._apply_bloom()
        else:
            # Just render scene to screen
            self._render_scene_only()
    
    def _apply_bloom(self) -> None:
        """Apply bloom effect pipeline."""
        settings = self.config.bloom
        
        # Step 1: Extract bright pixels
        self._extract_bright_pixels()
        
        # Step 2: Blur (ping-pong)
        self._apply_blur(settings.blur_passes)
        
        # Step 3: Composite
        self._composite_bloom()
    
    def _extract_bright_pixels(self) -> None:
        """Extract bright pixels from scene."""
        self._bright_fbo.bind()
        self._bright_fbo.clear()
        
        self._bright_program.use()
        self._scene_fbo.get_color_attachment(0).use(0)
        self._bright_program['u_texture'].value = 0
        self._bright_program['u_threshold'].value = self.config.bloom.threshold
        self._bright_program['u_soft_threshold'].value = self.config.bloom.soft_threshold
        
        self._bright_vao.render()
        self._bright_fbo.unbind()
    
    def _apply_blur(self, passes: int) -> None:
        """Apply Gaussian blur with ping-pong."""
        bloom_w = self.width // 2
        bloom_h = self.height // 2
        texel_size = (1.0 / bloom_w, 1.0 / bloom_h)
        
        self._blur_program.use()
        self._blur_program['u_texel_size'].value = texel_size
        
        # Source: bright FBO
        src_texture = self._bright_fbo.get_color_attachment(0)
        
        for pass_num in range(passes):
            # Horizontal pass
            self._ping_fbo.bind()
            self._ping_fbo.clear()
            
            src_texture.use(0)
            self._blur_program['u_texture'].value = 0
            self._blur_program['u_direction'].value = (1.0, 0.0)
            
            self._blur_vao.render()
            self._ping_fbo.unbind()
            
            # Vertical pass
            self._pong_fbo.bind()
            self._pong_fbo.clear()
            
            self._ping_fbo.get_color_attachment(0).use(0)
            self._blur_program['u_direction'].value = (0.0, 1.0)
            
            self._blur_vao.render()
            self._pong_fbo.unbind()
            
            # Use pong as source for next iteration
            src_texture = self._pong_fbo.get_color_attachment(0)
    
    def _composite_bloom(self) -> None:
        """Composite scene with bloom."""
        self._bloom_program.use()
        
        # Bind textures
        self._scene_fbo.get_color_attachment(0).use(0)
        self._pong_fbo.get_color_attachment(0).use(1)
        
        self._bloom_program['u_scene'].value = 0
        self._bloom_program['u_bloom'].value = 1
        self._bloom_program['u_intensity'].value = self.config.bloom.intensity
        self._bloom_program['u_enabled'].value = self.config.bloom.enabled
        self._bloom_program['u_debug_bloom'].value = self.config.bloom.debug_show_bloom
        self._bloom_program['u_exposure'].value = self.config.bloom.exposure
        self._bloom_program['u_tone_mapping'].value = self.config.bloom.tone_mapping
        
        self._bloom_vao.render()
    
    def _render_scene_only(self) -> None:
        """Render scene without bloom."""
        self._bloom_program.use()
        
        self._scene_fbo.get_color_attachment(0).use(0)
        self._bloom_program['u_scene'].value = 0
        self._bloom_program['u_enabled'].value = False
        self._bloom_program['u_exposure'].value = self.config.bloom.exposure
        
        self._bloom_vao.render()
    
    def resize(self, width: int, height: int) -> None:
        """Resize all framebuffers."""
        self.width = width
        self.height = height
        
        self._scene_fbo.resize(width, height)
        
        bloom_w = width // 2
        bloom_h = height // 2
        
        self._bright_fbo.resize(bloom_w, bloom_h)
        self._ping_fbo.resize(bloom_w, bloom_h)
        self._pong_fbo.resize(bloom_w, bloom_h)
    
    def set_bloom_enabled(self, enabled: bool) -> None:
        """Enable/disable bloom effect."""
        self.config.bloom.enabled = enabled
    
    def set_bloom_intensity(self, intensity: float) -> None:
        """Set bloom intensity (0.0 - 2.0)."""
        self.config.bloom.intensity = max(0.0, min(2.0, intensity))
    
    def set_bloom_threshold(self, threshold: float) -> None:
        """Set brightness threshold (0.0 - 1.0)."""
        self.config.bloom.threshold = max(0.0, min(1.0, threshold))
    
    def set_exposure(self, exposure: float) -> None:
        """Set exposure value."""
        self.config.bloom.exposure = exposure
    
    def get_scene_texture(self):
        """Get scene color texture for custom effects."""
        return self._scene_fbo.get_color_attachment(0)
    
    def shutdown(self) -> None:
        """Release all resources."""
        if self._scene_fbo:
            self._scene_fbo.release()
        if self._bright_fbo:
            self._bright_fbo.release()
        if self._ping_fbo:
            self._ping_fbo.release()
        if self._pong_fbo:
            self._pong_fbo.release()
        
        if self._quad_buffer:
            self._quad_buffer.release()
        
        if self._bright_program:
            self._bright_program.release()
        if self._blur_program:
            self._blur_program.release()
        if self._bloom_program:
            self._bloom_program.release()


def create_post_process_manager(ctx, width: int, height: int) -> PostProcessManager:
    """Create a PostProcessManager instance."""
    return PostProcessManager(ctx, width, height)
