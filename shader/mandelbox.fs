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


// Mandelbox 파라미터
float fixed_radius2 = 1.5;
float min_radius2 = 0.5;
float folding_limit = 1.0;
float scale = -2.0;

// Sphere folding
void sphereFold(inout vec3 z, inout float dz) {
    float r2 = dot(z, z);
    if (r2 < min_radius2) { 
        float temp = fixed_radius2 / min_radius2;
        z *= temp;
        dz *= temp;
    } else if (r2 < fixed_radius2) { 
        float temp = fixed_radius2 / r2;
        z *= temp;
        dz *= temp;
    }
}

// Box folding
void boxFold(inout vec3 z, inout float dz) {
    z = clamp(z, -folding_limit, folding_limit) * 2.0 - z;
}

// Mandelbox Distance Estimation Function
float mandelboxDistance(vec3 z) {
    z -= uCenter;
    vec3 offset = z;
    float dr = 1.0;
    for (int n = 0; n < 15; ++n) {
        // Box folding
        boxFold(z, dr);

        // Sphere folding
        sphereFold(z, dr);

        // Mandelbox Scaling
        z = scale * z + offset;
        dr = dr * abs(scale) + 1.0;
    }
    return length(z)  / abs(dr);
}

// Ray Marching
float rayMarch(vec3 ro, vec3 rd) {
    const int MAX_STEPS = 128;
    const float HIT_THRESHOLD = 0.001;
    const float MAX_DISTANCE = 100.0;

    float t = 0.01;
    for (int i = 0; i < MAX_STEPS; ++i) {
        vec3 p = ro + t * rd;
        float dist = mandelboxDistance(p);
        if (dist < HIT_THRESHOLD) {
            return t;
        }
        if (t > MAX_DISTANCE) {
            break;
        }
        t += dist;
    }
    return -1.0;
}

// 법선 계산
vec3 calculateNormal(vec3 p) {
    float epsilon = 0.01;
    return normalize(vec3(
        mandelboxDistance(p + vec3(epsilon, 0.0, 0.0)) - mandelboxDistance(p - vec3(epsilon, 0.0, 0.0)),
        mandelboxDistance(p + vec3(0.0, epsilon, 0.0)) - mandelboxDistance(p - vec3(0.0, epsilon, 0.0)),
        mandelboxDistance(p + vec3(0.0, 0.0, epsilon)) - mandelboxDistance(p - vec3(0.0, 0.0, epsilon))
    ));
}

// Diffuse Lighting 계산
vec3 calculateDiffuseLighting(vec3 p, vec3 n, vec3 lightPos) {
    vec3 lightDir = normalize(lightPos - p);
    float diffuse = max(dot(n, lightDir), 0.0);
    diffuse = clamp(diffuse, 0.1, 1.0);
    return diffuse * vec3(1.0, 0.8, 0.6);
}

float sdBox( vec3 p, vec3 b )
{
    vec3 q = abs(p) - b;
    return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

void main() {
    // 카메라가 구 바깥에 있을때는 레이를 두번 쏘기 때문에 걸러준다    
    vec3 viewToSurface = normalize(vPosition - uViewPos);
    float alignment = dot(viewToSurface, vNormal);    
    bool outside = (sdBox(uViewPos - uCenter, vec3(4.0, 4.0, 4.0)) > 0.00);
    if (outside) {
        if (alignment > 0.01) {
            discard;
        }
    }
    ////
    
    vec3 rayPos = uViewPos;
    vec3 rayDir = calculateRayDirection(gl_FragCoord.xy);

    float t = rayMarch(rayPos, rayDir);
    if (t > 0.0) {
        vec3 p = rayPos + t * rayDir;
        vec3 n = calculateNormal(p);
        vec3 color = calculateDiffuseLighting(p, n, uLightPos);

        fragColor = vec4(color, 1.0);
    }
    else
        discard;

}
