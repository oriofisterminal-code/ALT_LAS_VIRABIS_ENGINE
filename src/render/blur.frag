#version 330

// ALT_LAS Engine - Gaussian Blur Shader
// Two-pass separable Gaussian blur for bloom
// Version: GFX-002
// Reference: https://www.rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/

in vec2 v_tex;
out vec4 fragColor;

// Input texture
uniform sampler2D u_texture;
uniform vec2 u_direction;     // (1, 0) for horizontal, (0, 1) for vertical
uniform vec2 u_texel_size;    // 1.0 / texture_size

// Quality settings
uniform int u_quality;        // 0: low (5 samples), 1: medium (9), 2: high (13)

// Gaussian kernel weights
// Pre-calculated for sigma = 4.0
// 9-tap kernel covers ~3 sigma

// 5-tap (Low quality)
const float weights_5[3] = float[](
    0.2270270270,  // Center
    0.3162162162,  // ±1
    0.0702702703   // ±2
);
const float offsets_5[2] = float[](1.3846153846, 3.2307692308);

// 9-tap (Medium quality) - Optimized with linear sampling
const float weights_9[5] = float[](
    0.2270270270,  // Center
    0.1945945946,  // ±1
    0.1216216216,  // ±2
    0.0540540541,  // ±3
    0.0162162162   // ±4
);
const float offsets_9[4] = float[](1.0, 2.0, 3.0, 4.0);

// Optimized 9-tap with hardware linear filtering
// Combines samples for better cache performance
const float weights_opt_9[3] = float[](
    0.2270270270,  // Center
    0.3162162162,  // Combined ±1.38
    0.0702702703   // Combined ±3.23
);
const float offsets_opt_9[2] = float[](1.3846153846, 3.2307692308);

vec4 blur_5_tap(vec2 uv) {
    vec4 color = texture(u_texture, uv) * weights_5[0];
    vec2 texel_offset = u_direction * u_texel_size;
    
    for (int i = 1; i < 3; i++) {
        color += texture(u_texture, uv + texel_offset * offsets_5[i-1]) * weights_5[i];
        color += texture(u_texture, uv - texel_offset * offsets_5[i-1]) * weights_5[i];
    }
    
    return color;
}

vec4 blur_9_tap(vec2 uv) {
    vec4 color = texture(u_texture, uv) * weights_9[0];
    vec2 texel_offset = u_direction * u_texel_size;
    
    for (int i = 1; i < 5; i++) {
        color += texture(u_texture, uv + texel_offset * offsets_9[i-1]) * weights_9[i];
        color += texture(u_texture, uv - texel_offset * offsets_9[i-1]) * weights_9[i];
    }
    
    return color;
}

vec4 blur_9_optimized(vec2 uv) {
    // Optimized version using hardware linear filtering
    // Samples at offset positions that combine two texels each
    vec4 color = texture(u_texture, uv) * weights_opt_9[0];
    vec2 texel_offset = u_direction * u_texel_size;
    
    for (int i = 1; i < 3; i++) {
        color += texture(u_texture, uv + texel_offset * offsets_opt_9[i-1]) * weights_opt_9[i];
        color += texture(u_texture, uv - texel_offset * offsets_opt_9[i-1]) * weights_opt_9[i];
    }
    
    return color;
}

void main() {
    vec4 result;
    
    if (u_quality == 0) {
        // Low quality (5 samples)
        result = blur_5_tap(v_tex);
    } else if (u_quality == 1) {
        // Medium quality (9 samples, optimized)
        result = blur_9_optimized(v_tex);
    } else {
        // High quality (9 samples, full)
        result = blur_9_tap(v_tex);
    }
    
    fragColor = result;
}
