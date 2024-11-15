#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aColor;
layout (location = 2) in vec2 aTexCoord;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uTransform;


out mat4 veiwMat;
out mat4 inverseView;
out mat4 inverseProjection;
out vec4 vertexColor;
out vec2 texCoord;



void main() {
    inverseProjection = inverse(uProjection);
    inverseView = inverse(uView);
    veiwMat = uView;
    vertexColor = vec4(aColor, 1.0);
    texCoord = aTexCoord;
    gl_Position = uTransform * vec4(aPos, 1.0);
}