#version 330 core
/**
 * ALT_LAS Engine - Base Fragment Shader
 * Simple texture sampling with color tint.
 */

// Input from vertex shader
in vec2 v_texcoord;

// Output
out vec4 frag_color;

// Uniforms
uniform sampler2D u_texture;  // Sprite texture
uniform vec4 u_color;         // Color tint (RGBA)
uniform float u_time;         // Time for animations

void main() {
    // Sample texture
    vec4 tex_color = texture(u_texture, v_texcoord);

    // Apply color tint
    frag_color = tex_color * u_color;

    // Discard fully transparent pixels
    if (frag_color.a < 0.01) {
        discard;
    }
}
