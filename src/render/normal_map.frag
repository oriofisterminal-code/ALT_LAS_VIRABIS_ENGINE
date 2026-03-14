#version 330

// ALT_LAS Engine - Normal Map Fragment Shader
// 2D sprite normal mapping ve dinamik ışıklandırma
// Version: GFX-001
// Referans: Octopath Traveler

// Input from vertex shader
in vec2 v_tex;           // Texture coordinate
in vec2 v_world_pos;     // World position (pixels)
in vec2 v_local_pos;     // Local position within sprite

// Output
out vec4 fragColor;

// Texture samplers
uniform sampler2D u_texture;      // Sprite texture (diffuse)
uniform sampler2D u_normal_map;   // Normal map
uniform sampler2D u_specular_map; // Specular map (optional)

// Lighting uniforms
uniform vec2 u_light_pos[8];      // Light positions (up to 8 lights)
uniform vec3 u_light_color[8];    // Light colors
uniform float u_light_intensity[8];
uniform float u_light_radius[8];  // Light falloff radius
uniform int u_light_count;        // Active light count

// Global settings
uniform float u_ambient;          // Ambient light (0.0 - 1.0)
uniform float u_normal_strength;  // Normal map strength
uniform vec3 u_global_tint;       // Global color tint
uniform bool u_has_normal_map;    // Normal map available?
uniform bool u_has_specular;      // Specular map available?

// Debug
uniform bool u_debug_normals;     // Show normals as colors

// Constants
const float PI = 3.14159265359;

// Calculate normal from normal map
vec3 get_normal(vec2 uv) {
    if (!u_has_normal_map) {
        // Default normal pointing out of screen (Z direction)
        return vec3(0.0, 0.0, 1.0);
    }
    
    // Sample normal map (RGB = XYZ, 0-1 → -1 to 1)
    vec3 normal = texture(u_normal_map, uv).rgb;
    normal = normalize(normal * 2.0 - 1.0);
    
    // Apply strength (affect XY, keep Z normalized)
    normal.xy *= u_normal_strength;
    normal = normalize(normal);
    
    return normal;
}

// Calculate specular highlight
float get_specular(vec3 normal, vec2 light_dir, float shininess) {
    // Halfway vector for Blinn-Phong
    vec3 view_dir = vec3(0.0, 0.0, 1.0);
    vec3 light_dir_3d = vec3(light_dir, 0.3);
    vec3 halfway = normalize(light_dir_3d + view_dir);
    
    float spec = pow(max(dot(normal, halfway), 0.0), shininess);
    return spec;
}

// Point light calculation with attenuation
vec3 calculate_point_light(
    int index,
    vec3 base_color,
    vec3 normal,
    vec2 world_pos,
    float specular
) {
    vec2 light_pos = u_light_pos[index];
    vec3 light_color = u_light_color[index];
    float intensity = u_light_intensity[index];
    float radius = u_light_radius[index];
    
    // Direction to light
    vec2 to_light = light_pos - world_pos;
    float distance = length(to_light);
    vec2 light_dir = normalize(to_light);
    
    // Distance attenuation (smooth falloff)
    float attenuation = 1.0 - smoothstep(0.0, radius, distance);
    
    // If outside radius, no contribution
    if (attenuation <= 0.0) {
        return vec3(0.0);
    }
    
    // Convert 2D light direction to 3D for normal calculation
    vec3 light_dir_3d = vec3(light_dir, 0.5);
    light_dir_3d = normalize(light_dir_3d);
    
    // Diffuse (Lambertian)
    float diff = max(dot(normal, light_dir_3d), 0.0);
    
    // Combine diffuse and specular
    vec3 diffuse = base_color * diff * light_color;
    vec3 spec = specular * light_color * 0.5;
    
    // Final light contribution
    return (diffuse + spec) * intensity * attenuation;
}

void main() {
    // Sample base texture
    vec4 color = texture(u_texture, v_tex);
    
    // Alpha test - discard transparent pixels
    if (color.a < 0.1) {
        discard;
    }
    
    // Debug mode: show normals
    if (u_debug_normals) {
        vec3 normal = get_normal(v_tex);
        fragColor = vec4(normal * 0.5 + 0.5, 1.0);
        return;
    }
    
    // Get normal
    vec3 normal = get_normal(v_tex);
    
    // Get specular value
    float specular = 0.0;
    if (u_has_specular) {
        specular = texture(u_specular_map, v_tex).r;
    }
    
    // Start with ambient light
    vec3 final_color = color.rgb * u_ambient;
    
    // Add contribution from each light
    for (int i = 0; i < u_light_count && i < 8; i++) {
        vec3 light_contrib = calculate_point_light(
            i,
            color.rgb,
            normal,
            v_world_pos,
            specular
        );
        final_color += light_contrib;
    }
    
    // Apply global tint
    final_color *= u_global_tint;
    
    // Output final color with original alpha
    fragColor = vec4(final_color, color.a);
}
