"""
ALT_LAS Engine - Shaders Module
GLSL shaders for GPU-accelerated rendering.
Provides lighting, glow, and water effects.
"""

import os
from typing import Optional, Dict

# Shader directory path
SHADER_DIR = os.path.dirname(os.path.abspath(__file__))


def load_shader(filename: str) -> str:
    """Load shader source from file."""
    path = os.path.join(SHADER_DIR, filename)
    if os.path.exists(path):
        with open(path, 'r', encoding='utf-8') as f:
            return f.read()
    return ""


def get_vertex_shader() -> str:
    """Get base vertex shader source."""
    return load_shader("base.vert")


def get_fragment_shader() -> str:
    """Get base fragment shader source."""
    return load_shader("base.frag")


def get_glow_shader() -> str:
    """Get glow/bloom fragment shader."""
    return load_shader("glow.frag")


def get_light_shader() -> str:
    """Get point light fragment shader."""
    return load_shader("light.frag")


def get_water_shader() -> str:
    """Get water wave fragment shader."""
    return load_shader("water.frag")


# Embedded shader sources (fallback if files don't exist)
DEFAULT_VERTEX = """
#version 330 core

in vec2 in_position;
in vec2 in_texcoord;

out vec2 v_texcoord;

uniform mat4 u_projection;
uniform mat4 u_model;

void main() {
    v_texcoord = in_texcoord;
    gl_Position = u_projection * u_model * vec4(in_position, 0.0, 1.0);
}
"""

DEFAULT_FRAGMENT = """
#version 330 core

in vec2 v_texcoord;
out vec4 frag_color;

uniform sampler2D u_texture;
uniform vec4 u_color;
uniform float u_time;

void main() {
    vec4 tex_color = texture(u_texture, v_texcoord);
    frag_color = tex_color * u_color;
}
"""

LIGHT_FRAGMENT = """
#version 330 core

in vec2 v_texcoord;
out vec4 frag_color;

uniform sampler2D u_texture;
uniform vec4 u_color;
uniform float u_time;

// Ambient light
uniform vec3 u_ambient = vec3(0.3, 0.3, 0.4);

// Point lights (max 8)
uniform int u_light_count = 0;
uniform vec2 u_light_pos[8];
uniform vec3 u_light_color[8];
uniform float u_light_radius[8];
uniform float u_light_intensity[8];

void main() {
    vec4 tex_color = texture(u_texture, v_texcoord);

    // Start with ambient light
    vec3 lighting = u_ambient;

    // Add point lights
    for (int i = 0; i < u_light_count && i < 8; i++) {
        float dist = distance(v_texcoord, u_light_pos[i]);
        float attenuation = 1.0 - smoothstep(0.0, u_light_radius[i], dist);
        attenuation *= u_light_intensity[i];
        lighting += u_light_color[i] * attenuation;
    }

    // Clamp lighting
    lighting = clamp(lighting, 0.0, 1.0);

    // Apply lighting to texture
    frag_color = vec4(tex_color.rgb * lighting, tex_color.a);
}
"""

GLOW_FRAGMENT = """
#version 330 core

in vec2 v_texcoord;
out vec4 frag_color;

uniform sampler2D u_texture;
uniform sampler2D u_glow_texture;
uniform float u_glow_intensity = 1.0;
uniform float u_glow_radius = 0.01;

// Simple glow/bloom effect
void main() {
    vec4 base_color = texture(u_texture, v_texcoord);

    // Sample glow texture with blur
    vec4 glow = vec4(0.0);
    float samples = 0.0;

    for (float x = -2.0; x <= 2.0; x += 1.0) {
        for (float y = -2.0; y <= 2.0; y += 1.0) {
            vec2 offset = vec2(x, y) * u_glow_radius;
            glow += texture(u_glow_texture, v_texcoord + offset);
            samples += 1.0;
        }
    }
    glow /= samples;

    // Additive blend for bloom
    vec3 result = base_color.rgb + glow.rgb * u_glow_intensity;

    frag_color = vec4(result, base_color.a);
}
"""

WATER_FRAGMENT = """
#version 330 core

in vec2 v_texcoord;
out vec4 frag_color;

uniform sampler2D u_texture;
uniform float u_time;
uniform float u_wave_speed = 1.0;
uniform float u_wave_amplitude = 0.02;
uniform float u_wave_frequency = 10.0;

void main() {
    // Calculate wave distortion
    float wave = sin(v_texcoord.y * u_wave_frequency + u_time * u_wave_speed);
    wave += sin(v_texcoord.x * u_wave_frequency * 0.5 + u_time * u_wave_speed * 0.7);
    wave *= u_wave_amplitude;

    // Apply distortion to UV
    vec2 distorted_uv = v_texcoord + vec2(wave, wave * 0.5);

    // Sample texture with distortion
    vec4 tex_color = texture(u_texture, distorted_uv);

    // Add subtle blue tint for water
    vec3 water_tint = vec3(0.9, 0.95, 1.0);
    vec3 result = mix(tex_color.rgb, tex_color.rgb * water_tint, 0.3);

    // Add shimmer
    float shimmer = sin(u_time * 3.0 + v_texcoord.x * 20.0) * 0.1 + 0.9;
    result *= shimmer;

    frag_color = vec4(result, tex_color.a);
}
"""


# Shader cache
_shader_cache: Dict[str, str] = {}


def get_shader(name: str, use_embedded: bool = True) -> str:
    """
    Get shader source by name.
    Falls back to embedded defaults if file not found.
    """
    if name in _shader_cache:
        return _shader_cache[name]

    # Try to load from file
    source = load_shader(name)
    if source:
        _shader_cache[name] = source
        return source

    # Use embedded defaults
    if use_embedded:
        embedded = {
            "base.vert": DEFAULT_VERTEX,
            "base.frag": DEFAULT_FRAGMENT,
            "light.frag": LIGHT_FRAGMENT,
            "glow.frag": GLOW_FRAGMENT,
            "water.frag": WATER_FRAGMENT,
        }
        if name in embedded:
            _shader_cache[name] = embedded[name]
            return embedded[name]

    return ""
