#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec4 aColor;
layout (location = 2) in vec2 aNormal;
layout (location = 3) in vec2 aUv;

out vec4 vertexColor;
out vec2 vertexUv;

uniform int frame;
uniform mat4 transform;
uniform mat4 camera;

void main()
{
	vertexColor = aColor;
	vertexUv = aUv;
    gl_Position = camera * transform * vec4(aPos, 0.0f, 1.0f);
}