#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(component = 0, location = 0) in vec2 in_position;
layout(component = 0, location = 1) in vec2 in_uv;
layout(component = 1, location = 0) in uint in_instance;

out gl_PerVertex 
{
    vec4 gl_Position;   
};

void main()
{
	gl_Position = vec4(in_position, 0, 1);
}
