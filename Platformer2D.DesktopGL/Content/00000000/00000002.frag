#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

struct MaterialInfo
{
	uint TextureID;
	uint Unused_0;
	uint Unused_1;
	uint Unused_2;
	vec4 Color;
	mat4 Transform;
};

layout (binding = 0) uniform sampler2D in_Textures[16];

layout(binding = 1, std430) buffer CB0
{
	MaterialInfo material[];
};

out vec4 fragColor;

void main()
{
	fragColor = vec4(0, 0, 1, 1.0f);
}

