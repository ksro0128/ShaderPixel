#version 330 core
out vec4 fragColor;

in vec3 normal;
in vec2 texCoord;
in vec3 position;

// 텍스처 샘플러 정의
uniform sampler2D diffuseMap;   // 기본 색상 텍스처
uniform sampler2D specularMap;  // 스펙큘러 텍스처

// 고정된 조명 정보
const vec3 lightPos = vec3(0.0, 10.0, 0.0);    // 고정된 조명 위치
const vec3 lightColor = vec3(1.0, 0.95, 0.8);  // 고정된 조명 색상
const vec3 ambientColor = vec3(0.2, 0.2, 0.2); // 고정된 주변광 색상

// 카메라 위치
uniform vec3 viewPos;

void main() {
    vec3 texColor = texture(diffuseMap, texCoord).xyz;
    vec3 ambient = texColor * ambientColor;

    float dist = length(lightPos - position);
    vec3 distPoly = vec3(1.0, dist, dist*dist);
    float attenuation = 1.0 / dot(distPoly, vec3(1.0, 0.7, 1.8));
    vec3 lightDir = (lightPos - position) / dist;

    float theta = dot(lightDir, normalize(-lightPos));
    float intensity = clamp((theta - 0.4) / 0.2, 0.0, 1.0);
    vec3 result = ambient;
    if (intensity > 0) {
        vec3 pixelNorm = normalize(normal);
        float diff = max(dot(pixelNorm, lightDir), 0.0);
        vec3 diffuse = diff * texColor * lightColor;
        vec3 specColor = texture(specularMap, texCoord).xyz;
        float spec = 0.0;
        vec3 viewDir = normalize(viewPos - position);
        vec3 reflectDir = reflect(-lightDir, pixelNorm);
        spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
        vec3 specular = spec * specColor * lightColor;

        result += (diffuse + specular) * intensity;
    }
    result *= attenuation;
    fragColor = vec4(ambient, 1.0);
}
