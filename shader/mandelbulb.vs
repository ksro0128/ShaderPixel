#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uModel;
uniform mat4 uTransform;

out mat4 inverseView;
out mat4 inverseProjection;
out vec3 vNormal;
out vec3 vPosition;

void main() {
    inverseProjection = inverse(uProjection);
    inverseView = inverse(uView);
    vNormal = (transpose(inverse(uModel)) * vec4(aNormal, 0.0)).xyz;
    vPosition = (uModel * vec4(aPos, 1.0)).xyz;
    gl_Position = uTransform * vec4(aPos, 1.0);
}