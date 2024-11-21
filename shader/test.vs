#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aColor;
layout (location = 2) in vec2 aTexCoord;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uTransform;

out mat4 inverseView;
out mat4 inverseProjection;
out vec2 texCoord;



void main() {
    inverseProjection = inverse(uProjection);
    inverseView = inverse(uView);
    texCoord = aTexCoord;
    vec4 clipPos = uTransform * vec4(aPos, 1.0);
    gl_Position = clipPos;
}