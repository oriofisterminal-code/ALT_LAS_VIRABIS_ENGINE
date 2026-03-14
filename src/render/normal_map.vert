#version 330

// ALT_LAS Engine - Normal Map Vertex Shader
// 2D sprite normal mapping için vertex shader
// Version: GFX-001

// Vertex attributes
in vec2 in_pos;      // Position (pixels)
in vec2 in_tex;      // Texture coordinates

// Output to fragment shader
out vec2 v_tex;           // Texture coordinate
out vec2 v_world_pos;     // World position (pixels)
out vec2 v_local_pos;     // Local position within sprite

// Uniforms
uniform vec2 u_offset;    // Sprite offset in world
uniform vec2 u_screen;    // Screen size
uniform vec2 u_size;      // Sprite size

void main() {
    // Convert to normalized device coordinates
    // NDC: -1 to 1, Y is inverted for screen coordinates
    vec2 pos = (in_pos + u_offset) / u_screen * 2.0 - 1.0;
    gl_Position = vec4(pos.x, -pos.y, 0.0, 1.0);
    
    // Pass texture coordinates
    v_tex = in_tex;
    
    // Calculate world position for lighting
    v_world_pos = in_pos + u_offset;
    
    // Local position within sprite (0-1)
    v_local_pos = in_pos / u_size;
}
