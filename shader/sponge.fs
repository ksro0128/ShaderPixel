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

float sdBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b; // 점과 정육면체 표면 간의 거리
    return length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);
}

float map(vec3 p) {
    p -= uCenter;
    float d = sdBox(p, vec3(1.0)); // 기본 정육면체 거리
    float s = 1.0;

    // Menger Sponge를 반복적으로 생성
    for (int m = 0; m < 4; m++) {
        vec3 a = mod(p * s, 2.0) - 1.0; // 좌표를 -1~1로 정규화
        s *= 3.0; // 스케일 확대
        vec3 r = abs(1.0 - 3.0 * abs(a));
        float da = max(r.x, r.y);
        float db = max(r.y, r.z);
        float dc = max(r.z, r.x);
        float c = (min(da, min(db, dc)) - 1.0) / s; // Sponge 내부 구멍 거리 계산

        if (c > d) { // 기존 거리보다 더 가까운 구멍이면 갱신
            d = c;
        }
    }

    return d; // Sponge 큐브까지의 거리 반환
}

// 레이마칭 함수
bool raymarch(vec3 rayOrigin, vec3 rayDir, out vec3 hitPos) {
    for (int i = 0; i < 200; i++) {
        float dist = map(rayOrigin);
        if (dist < 0.001) {
            hitPos = rayOrigin;
            return true;
        }
        rayOrigin += rayDir * dist;
    }
    return false;
}

vec3 calculateNormal(vec3 pos) {
    vec2 e = vec2(0.001, 0.0);
    return normalize(vec3(
        map(pos + vec3(e.x, e.y, e.y)) - map(pos - vec3(e.x, e.y, e.y)),
        map(pos + vec3(e.y, e.x, e.y)) - map(pos - vec3(e.y, e.x, e.y)),
        map(pos + vec3(e.y, e.y, e.x)) - map(pos - vec3(e.y, e.y, e.x))
    ));
}

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

bool isInShadow(vec3 point, vec3 lightPos) {
    vec3 lightDir = normalize(lightPos - point);
    float distToLight = length(lightPos - point);
    float shadowDepth = 0.01; // 그림자 시작 깊이 보정
    
    for (int i = 0; i < 200; ++i) {
        vec3 samplePoint = point + shadowDepth * lightDir;
        float dist = map(samplePoint);
        if (dist < 0.001) return true; // 빛이 차단됨
        shadowDepth += dist;
        if (shadowDepth >= distToLight) return false; // 그림자 없음
    }
    return false; // 빛이 도달
}

void main() {
    
    // 카메라가 구 바깥에 있을때는 레이를 두번 쏘기 때문에 걸러준다    
    vec3 viewToSurface = normalize(vPosition - uViewPos);
    float alignment = dot(viewToSurface, vNormal);    
    bool outside = (sdBox(uViewPos - uCenter, vec3(1.0, 1.0, 1.0)) > 0.00);
    if (outside) {
        if (alignment > 0.01) {
            discard;
        }
    }
    ////



    // Calculate ray direction
    vec3 rayDir = calculateRayDirection(gl_FragCoord.xy);
    // 레이 원점
    vec3 rayPos = uViewPos;

    vec3 hitPos;
    if (raymarch(rayPos, rayDir, hitPos)) {

        vec3 nor = calculateNormal(hitPos);
        vec3 color = phongShading(hitPos, nor, uLightPos, uViewPos);
        bool shadow = isInShadow(hitPos + nor * 0.001, uLightPos);
        if (shadow) {
            color *= 0.5;
        }
        fragColor = vec4(color, 1.0);
    }
    else
        discard ;
}
