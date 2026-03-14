#version 330 core
/**
 * ALT_LAS Engine - Point Light Fragment Shader
 * Implements point light + ambient lighting system.
 * Supports up to 8 dynamic point lights.
 */

// Input from vertex shader
in vec2 v_texcoord;

// Output
out vec4 frag_color;

// Uniforms
uniform sampler2D u_texture;
uniform vec4 u_color;
uniform float u_time;

// Ambient light settings
uniform vec3 u_ambient = vec3(0.3, 0.3, 0.4);

// Point lights (max 8 for performance)
uniform int u_light_count = 0;
uniform vec2 u_light_pos[8];
uniform vec3 u_light_color[8];
uniform float u_light_radius[8];
uniform float u_light_intensity[8];

/**
 * Calculate light contribution for a single point light.
 */
vec3 calculate_point_light(int index, vec2 uv) {
    // Distance from light to current pixel
    float dist = distance(uv, u_light_pos[index]);

    // Smooth falloff using smoothstep
    float attenuation = 1.0 - smoothstep(0.0, u_light_radius[index], dist);

    // Apply intensity
    attenuation *= u_light_intensity[index];

    // Return light contribution
    return u_light_color[index] * attenuation;
}

void main() {
    // Sample base texture
    vec4 tex_color = texture(u_texture, v_texcoord);

    // Start with ambient light
    vec3 lighting = u_ambient;

    // Accumulate point lights
    for (int i = 0; i < u_light_count && i < 8; i++) {
        lighting += calculate_point_light(i, v_texcoord);
    }

    // Clamp lighting to valid range
    lighting = clamp(lighting, 0.0, 2.0);  // Allow slight overbright

    // Apply lighting to texture color
    vec3 lit_color = tex_color.rgb * lighting;

    // Output with original alpha
    frag_color = vec4(lit_color, tex_color.a);

    // Discard transparent pixels
    if (frag_color.a < 0.01) {
        discard;
    }
}
