#version 330 core
/**
 * ALT_LAS Engine - Glow/Bloom Fragment Shader
 * Creates bloom effect on bright areas.
 * Post-processing effect for added visual polish.
 */

// Input from vertex shader
in vec2 v_texcoord;

// Output
out vec4 frag_color;

// Uniforms
uniform sampler2D u_texture;       // Base texture
uniform sampler2D u_glow_texture;  // Pre-rendered glow sources
uniform float u_glow_intensity = 1.0;
uniform float u_glow_radius = 0.01;
uniform float u_time;

/**
 * Apply Gaussian-like blur for glow effect.
 */
vec4 sample_blur(sampler2D tex, vec2 uv, float radius) {
    vec4 color = vec4(0.0);
    float total = 0.0;

    // 5x5 kernel for blur
    for (float x = -2.0; x <= 2.0; x += 1.0) {
        for (float y = -2.0; y <= 2.0; y += 1.0) {
            vec2 offset = vec2(x, y) * radius;
            float weight = 1.0 / (1.0 + abs(x) + abs(y));
            color += texture(tex, uv + offset) * weight;
            total += weight;
        }
    }

    return color / total;
}

void main() {
    // Get base color
    vec4 base_color = texture(u_texture, v_texcoord);

    // Sample glow with blur
    vec4 glow = sample_blur(u_glow_texture, v_texcoord, u_glow_radius);

    // Threshold for bloom (only bright areas)
    float brightness = dot(glow.rgb, vec3(0.2126, 0.7152, 0.0722));
    if (brightness < 0.5) {
        glow.rgb *= 0.0;
    }

    // Additive blending for bloom
    vec3 result = base_color.rgb + glow.rgb * u_glow_intensity;

    // Soft pulsing animation
    float pulse = sin(u_time * 2.0) * 0.1 + 1.0;
    result *= pulse;

    // Output with original alpha
    frag_color = vec4(result, base_color.a);

    // Discard transparent pixels
    if (frag_color.a < 0.01) {
        discard;
    }
}
