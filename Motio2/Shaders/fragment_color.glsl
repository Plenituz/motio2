#version 330 core
out vec4 FragColor;

in vec4 vertexColor;
in vec2 vertexUv;

uniform int frame;

void main()
{
    FragColor = vec4(1, 0.5, 1, 1);
}