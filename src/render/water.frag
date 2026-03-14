#version 330 core
/**
 * ALT_LAS Engine - Water Wave Fragment Shader
 * Creates animated water wave distortion effect.
 * Perfect for lakes, rivers, and underwater scenes.
 */

// Input from vertex shader
in vec2 v_texcoord;

// Output
out vec4 frag_color;

// Uniforms
uniform sampler2D u_texture;
uniform float u_time;
uniform float u_wave_speed = 1.0;
uniform float u_wave_amplitude = 0.02;
uniform float u_wave_frequency = 10.0;

/**
 * Calculate wave offset using multiple sine waves.
 * Creates more natural-looking water movement.
 */
vec2 calculate_wave_offset(vec2 uv, float time) {
    // Primary wave (horizontal movement)
    float wave1 = sin(uv.y * u_wave_frequency + time * u_wave_speed);

    // Secondary wave (vertical movement)
    float wave2 = sin(uv.x * u_wave_frequency * 0.7 + time * u_wave_speed * 0.8);

    // Tertiary wave (diagonal)
    float wave3 = sin((uv.x + uv.y) * u_wave_frequency * 0.5 + time * u_wave_speed * 1.2);

    // Combine waves
    float combined = (wave1 + wave2 * 0.5 + wave3 * 0.3) / 1.8;

    return vec2(combined, combined * 0.7) * u_wave_amplitude;
}

void main() {
    // Calculate wave distortion
    vec2 offset = calculate_wave_offset(v_texcoord, u_time);

    // Apply distortion to UV coordinates
    vec2 distorted_uv = v_texcoord + offset;

    // Clamp to valid UV range
    distorted_uv = clamp(distorted_uv, 0.0, 1.0);

    // Sample texture with distortion
    vec4 tex_color = texture(u_texture, distorted_uv);

    // Water color tint (subtle blue-green)
    vec3 water_tint = vec3(0.85, 0.92, 1.0);

    // Mix original color with water tint
    vec3 result = mix(tex_color.rgb, tex_color.rgb * water_tint, 0.25);

    // Add shimmer effect
    float shimmer_base = sin(u_time * 3.0 + v_texcoord.x * 20.0 + v_texcoord.y * 15.0);
    float shimmer = shimmer_base * 0.08 + 0.92;
    result *= shimmer;

    // Add subtle caustics simulation
    float caustic1 = sin(v_texcoord.x * 30.0 + u_time * 2.0) * sin(v_texcoord.y * 25.0 - u_time * 1.5);
    float caustic2 = sin(v_texcoord.x * 20.0 - u_time * 1.8) * sin(v_texcoord.y * 35.0 + u_time * 2.2);
    float caustics = (caustic1 + caustic2) * 0.03;
    result += vec3(caustics * 0.8, caustics, caustics * 1.2);

    // Output with original alpha
    frag_color = vec4(result, tex_color.a);

    // Discard transparent pixels
    if (frag_color.a < 0.01) {
        discard;
    }
}
