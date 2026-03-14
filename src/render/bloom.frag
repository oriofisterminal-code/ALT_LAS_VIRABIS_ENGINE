#version 330

// ALT_LAS Engine - Bloom Composite Shader
// Combines original scene with blurred bright pixels
// Version: GFX-002
// Reference: Unreal Engine 4 bloom

in vec2 v_tex;
out vec4 fragColor;

// Textures
uniform sampler2D u_scene;      // Original scene (full resolution)
uniform sampler2D u_bloom;      // Bloom texture (blurred bright pixels)

// Settings
uniform float u_intensity;      // Bloom strength multiplier
uniform bool u_enabled;         // Bloom on/off
uniform bool u_debug_bloom;     // Show only bloom for debugging
uniform float u_exposure;       // Exposure adjustment

// Tone mapping settings
uniform int u_tone_mapping;     // 0: None, 1: Reinhard, 2: Filmic, 3: ACES

// Gamma correction
const float GAMMA = 2.2;
const float INV_GAMMA = 1.0 / GAMMA;

// Reinhard tone mapping
vec3 reinhard_tone_mapping(vec3 color) {
    return color / (color + vec3(1.0));
}

// Filmic tone mapping (Uncharted 2)
vec3 filmic_tone_mapping(vec3 color) {
    vec3 x = max(vec3(0.0), color - 0.004);
    return (x * (6.2 * x + 0.5)) / (x * (6.2 * x + 1.7) + 0.06);
}

// ACES Filmic tone mapping (Academy Color Encoding System)
vec3 aces_tone_mapping(vec3 color) {
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    
    return clamp((color * (a * color + b)) / (color * (c * color + d) + e), 0.0, 1.0);
}

// Linear to sRGB (gamma correction)
vec3 linear_to_srgb(vec3 color) {
    return pow(color, vec3(INV_GAMMA));
}

// sRGB to Linear
vec3 srgb_to_linear(vec3 color) {
    return pow(color, vec3(GAMMA));
}

void main() {
    // Sample textures
    vec4 scene_color = texture(u_scene, v_tex);
    
    if (!u_enabled) {
        // Bloom disabled - just apply tone mapping
        vec3 result = scene_color.rgb;
        
        if (u_tone_mapping > 0) {
            result = reinhard_tone_mapping(result * u_exposure);
        }
        
        result = linear_to_srgb(result);
        fragColor = vec4(result, scene_color.a);
        return;
    }
    
    vec4 bloom_color = texture(u_bloom, v_tex);
    
    // Debug mode: show only bloom
    if (u_debug_bloom) {
        fragColor = vec4(bloom_color.rgb * u_intensity, 1.0);
        return;
    }
    
    // Combine scene and bloom (additive blending)
    vec3 result = scene_color.rgb + bloom_color.rgb * u_intensity;
    
    // Apply exposure
    result *= u_exposure;
    
    // Tone mapping
    if (u_tone_mapping == 1) {
        result = reinhard_tone_mapping(result);
    } else if (u_tone_mapping == 2) {
        result = filmic_tone_mapping(result);
    } else if (u_tone_mapping == 3) {
        result = aces_tone_mapping(result);
    }
    
    // Clamp to valid range
    result = clamp(result, 0.0, 1.0);
    
    // Gamma correction (linear to sRGB)
    result = linear_to_srgb(result);
    
    fragColor = vec4(result, scene_color.a);
}
