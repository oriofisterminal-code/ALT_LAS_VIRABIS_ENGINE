#version 330 core
/**
 * ALT_LAS Engine - Base Vertex Shader
 * Handles sprite transformation and UV mapping.
 */

// Vertex attributes
in vec2 in_position;
in vec2 in_texcoord;

// Output to fragment shader
out vec2 v_texcoord;

// Uniforms
uniform mat4 u_projection;  // Orthographic projection matrix
uniform mat4 u_model;       // Model transformation matrix

void main() {
    // Pass texture coordinates to fragment shader
    v_texcoord = in_texcoord;

    // Transform vertex position
    gl_Position = u_projection * u_model * vec4(in_position, 0.0, 1.0);
}
