#version 330 core

out vec4 fragColor;

uniform vec3 uCenter;         // Mandelbox 중심 위치
uniform vec3 uViewPos;        // 카메라 위치
uniform vec3 uLightPos;       // 광원 위치
uniform vec2 uResolution;     // 렌더링 해상도

in mat4 inverseView;
in mat4 inverseProjection;
in vec3 vNormal;
in vec3 vPosition;

vec3 calculateRayDirection(vec2 fragCoord) {
    vec4 clipSpacePos = vec4((fragCoord / uResolution) * 2.0 - 1.0, -1.0, 1.0);    
    vec4 viewSpacePos = inverseProjection * clipSpacePos;
    viewSpacePos = vec4(viewSpacePos.xy, -1.0, 0.0);
    vec3 worldSpaceDir = normalize((inverseView * viewSpacePos).xyz);
    
    return worldSpaceDir;
}

const int MAX_MARCHING_STEPS = 300;
const float MIN_DIST = 0.001f;  // 최소 거리 (탈출 조건)
const float MAX_DIST = 30.0f;  // 최대 거리 (탈출 조건)
const float EPSILON = 0.0001f;  // 거리 함수 민감도
const float power = 8.0f;       // Mandelbulb fractal 파워
const int iter = 8;             // 최대 반복 횟수
const float bailOut = 2.0f;     // 탈출 반경

// Mandelbulb distance function
float Mandelbulb(vec3 pos) {
    vec3 z = pos;
    float dr = 1.0;
    float r = 0.0;

    for (int i = 0; i < iter; ++i) {
        r = length(z);
        if (r > bailOut) break;

        float theta = acos(z.z / r);
        float phi = atan(z.y, z.x);
        float zr = pow(r, power - 1.0);
        dr = zr * power * dr + 1.0;
        zr *= r;
        theta *= power;
        phi *= power;

        z = zr * vec3(sin(theta) * cos(phi), sin(theta) * sin(phi), cos(theta));
        z += pos;
    }

    return 0.25 * log(r) * r / dr;
}

// Scene distance function
float SceneSDF(vec3 p) {
    vec3 relativePos = p - uCenter;
    return Mandelbulb(relativePos);
}

// Calculate surface normal using gradient approximation
vec3 calculateNormal(vec3 p) {
    return normalize(vec3(
        SceneSDF(vec3(p.x + EPSILON, p.y, p.z)) - SceneSDF(vec3(p.x - EPSILON, p.y, p.z)),
        SceneSDF(vec3(p.x, p.y + EPSILON, p.z)) - SceneSDF(vec3(p.x, p.y - EPSILON, p.z)),
        SceneSDF(vec3(p.x, p.y, p.z + EPSILON)) - SceneSDF(vec3(p.x, p.y, p.z - EPSILON))
    ));
}

// Ray marching function
vec4 rayMarch(vec3 rayOrigin, vec3 rayDir) {
    float depth = 0.0;
    for (int i = 0; i < MAX_MARCHING_STEPS; ++i) {
        vec3 p = rayOrigin + depth * rayDir;
        float dist = SceneSDF(p);
        if (dist < EPSILON) return vec4(p, 1.0); // Hit point
        if (depth > MAX_DIST) break;            // Exceed max distance
        depth += dist;
    }
    return vec4(0.0);
}

bool isInShadow(vec3 point, vec3 lightDir) {
    float distToLight = length(uLightPos - point);
    float shadowDepth = 0.01; // 그림자 시작 깊이 보정
    
    for (int i = 0; i < MAX_MARCHING_STEPS; ++i) {
        vec3 samplePoint = point + shadowDepth * lightDir;
        float dist = SceneSDF(samplePoint);
        if (dist < EPSILON) return true; // 빛이 차단됨
        shadowDepth += dist;
        if (shadowDepth >= distToLight) return false; // 그림자 없음
    }
    return false; // 빛이 도달
}

float calculateAO(vec3 point, vec3 normal) {
    float ao = 0.0;
    float aoScale = 0.1; // AO 반경
    for (int i = 0; i < 8; ++i) {
        vec3 samplePoint = point + normal * aoScale * float(i);
        float dist = SceneSDF(samplePoint);
        ao += clamp(dist - float(i) * aoScale, 0.0, 1.0);
    }
    return clamp(1.0 - ao * 0.1, 0.0, 1.0); // 0~1 범위로 정규화
}


// Phong shading for lighting
vec3 phongShading(vec3 p, vec3 normal, vec3 lightPos, vec3 viewPos) {
    vec3 ambient = 0.2 * vec3(1.0, 1.0, 1.0);
    vec3 lightDir = normalize(lightPos - p);
    vec3 viewDir = normalize(viewPos - p);
    vec3 reflectDir = reflect(-lightDir, normal);

    // Diffuse
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * vec3(1.0, 0.8, 0.6);

    // Specular
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 16.0);
    vec3 specular = spec * vec3(1.0);

    return ambient + diffuse + specular;
}


void main() {

    // 카메라가 구 바깥에 있을때는 레이를 두번 쏘기 때문에 걸러준다    
    vec3 viewToSurface = normalize(vPosition - uViewPos);
    float alignment = dot(viewToSurface, vNormal);    
    bool outside = (length(uViewPos - uCenter) > 1.5);
    if (outside) {
        if (alignment > 0.01) {
            discard;
        }
    }
    ////


    // Calculate ray direction
    vec3 rayDir = calculateRayDirection(gl_FragCoord.xy);
    vec4 hit = rayMarch(uViewPos, rayDir);
    if (hit.w == 0.0) {
        discard;
    }

    vec3 hitPos = hit.xyz;
    vec3 normal = calculateNormal(hitPos);
    vec3 lightDir = normalize(uLightPos - hitPos);
    
    // Shadows
    bool inShadow = isInShadow(hitPos + normal * EPSILON, lightDir);
    
    // Ambient Occlusion
    float ao = calculateAO(hitPos, normal);

    // Phong Shading
    vec3 color = phongShading(hitPos, normal, uLightPos, uViewPos);

    // Apply shadows and AO
    if (inShadow) {
        color *= 0.5; // Shadow intensity
    }
    color *= ao; // AO intensity

    fragColor = vec4(color, 1.0);
	

}
