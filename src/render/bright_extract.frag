#version 330

// ALT_LAS Engine - Brightness Extraction Shader
// Extracts bright pixels above threshold for bloom effect
// Version: GFX-002
// Reference: Celeste bloom technique

in vec2 v_tex;
out vec4 fragColor;

// Input texture
uniform sampler2D u_texture;

// Settings
uniform float u_threshold;      // Brightness threshold (default: 0.7)
uniform float u_soft_threshold; // Soft knee for smooth transition

// Constants
const vec3 LUMINANCE_WEIGHTS = vec3(0.2126, 0.7152, 0.0722);

// Calculate relative luminance (perceived brightness)
float luminance(vec3 color) {
    return dot(color, LUMINANCE_WEIGHTS);
}

void main() {
    // Sample input texture
    vec4 color = texture(u_texture, v_tex);
    
    // Calculate brightness
    float brightness = luminance(color.rgb);
    
    // Soft threshold with knee
    // Creates smooth transition instead of hard cutoff
    float soft_knee = u_threshold - u_soft_threshold;
    
    float contribution = 0.0;
    
    if (brightness > u_threshold) {
        // Full contribution above threshold
        contribution = 1.0;
    } else if (brightness > soft_knee) {
        // Soft transition in knee region
        contribution = smoothstep(soft_knee, u_threshold, brightness);
    }
    
    // Apply contribution to color
    fragColor = color * contribution;
}
