#version 330 core
out vec4 FragColor;

in vec4 vertexColor;
in vec2 vertexUv;

uniform int frame;

void main()
{
    FragColor = vertexColor;
}