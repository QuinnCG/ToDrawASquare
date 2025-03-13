// Vertex Shader
#version 330 core

layout (location = 0) in vec2 a_pos;
layout (location = 1) in vec3 a_col;
layout (location = 2) in vec2 a_uv;

out vec3 v_col;
out vec2 v_uv;

uniform mat4 u_mvp;

void main()
{
	gl_Position = u_mvp * vec4(a_pos.x, a_pos.y, 0.0, 1.0);
	v_col = a_col;
	v_uv = a_uv;
}

// Fragment Shader
#version 330 core

in vec3 v_col;
in vec2 v_uv;
out vec4 f_color;

uniform sampler2D u_texture;

void main()
{
	f_color = vec4(v_col.xyz, 1.0) * texture(u_texture, v_uv);
}
