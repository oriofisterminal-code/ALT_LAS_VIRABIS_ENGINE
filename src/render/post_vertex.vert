#version 330

// ALT_LAS Engine - Post-Process Vertex Shader
// Fullscreen quad for post-processing effects
// Version: GFX-002

// Vertex attributes for fullscreen quad
// Using a triangle that covers the entire screen
// No VAO needed, generated in vertex shader

out vec2 v_tex;

void main() {
    // Generate fullscreen triangle
    // gl_VertexID: 0, 1, 2
    
    // Triangle vertices covering entire screen
    vec2 positions[3] = vec2[](
        vec2(-1.0, -1.0),
        vec2( 3.0, -1.0),
        vec2(-1.0,  3.0)
    );
    
    // UV coordinates matching the triangle
    vec2 uvs[3] = vec2[](
        vec2(0.0, 0.0),
        vec2(2.0, 0.0),
        vec2(0.0, 2.0)
    );
    
    // Output
    gl_Position = vec4(positions[gl_VertexID], 0.0, 1.0);
    v_tex = uvs[gl_VertexID];
}
