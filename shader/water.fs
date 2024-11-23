#version 330 core
out vec4 fragColor;

in mat4 inverseView;
in mat4 inverseProjection;
in vec2 texCoord;

uniform vec3 uCenter;           // object 중심 위치
uniform vec3 uViewPos;          // 카메라 위치
uniform vec2 uResolution;       // 렌더링 해상도
uniform vec3 uLightPos;         // 광원 위치
uniform float uTime;

uniform sampler2D tex;          // 배경
uniform samplerCube cubeTex;    // 큐브 배경

vec3 calculateRayDirection(vec2 fragCoord) {
    vec4 clipSpacePos = vec4((fragCoord / uResolution) * 2.0 - 1.0, -1.0, 1.0);    
    vec4 viewSpacePos = inverseProjection * clipSpacePos;
    viewSpacePos = vec4(viewSpacePos.xy, -1.0, 0.0);
    vec3 worldSpaceDir = normalize((inverseView * viewSpacePos).xyz);
    
    return worldSpaceDir;
}

mat3 m = mat3( 0.00,  0.80,  0.60,
              -0.80,  0.36, -0.48,
              -0.60, -0.48,  0.64);

float hash(float n)
{
    return fract(sin(n) * 43758.5453);
}

float noise(in vec3 x)
{
    vec3 p = floor(x);
    vec3 f = fract(x);
    
    f = f * f * (3.0 - 2.0 * f);
    
    float n = p.x + p.y * 57.0 + 113.0 * p.z;
    
    float res = mix(mix(mix(hash(n +   0.0), hash(n +   1.0), f.x),
                        mix(hash(n +  57.0), hash(n +  58.0), f.x), f.y),
                    mix(mix(hash(n + 113.0), hash(n + 114.0), f.x),
                        mix(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
    return res;
}

float fbm(vec3 p)
{
    float f;
    p += vec3(0.0, uTime * 0.3, uTime * 0.4); // 시간에 따라 위치를 변화
    f  = 0.5000 * noise(p); p = m * p * 2.02;
    f += 0.2500 * noise(p); p = m * p * 2.03;
    f += 0.1250 * noise(p);
    return f;
}

float sdBox(vec3 p, vec3 b) {
    p -= uCenter;
    vec3 d = abs(p) - b; // 점과 정육면체 표면 간의 거리
    float dist = length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);

    float noise = fbm(p);
    return dist + 0.1 * noise;
}

bool raymarch(vec3 rayOri, vec3 rayDir, out vec3 hitPos) {
    const float MAX_DISTANCE = 100.0;
    const float MIN_HIT_DISTANCE = 0.001;
    const int MAX_STEPS = 100;

    float totalDistance = 0.0;

    for (int i = 0; i < MAX_STEPS; ++i) {
        vec3 currentPos = rayOri + totalDistance * rayDir;
        float dist = sdBox(currentPos, vec3(1.0));

        if (dist < MIN_HIT_DISTANCE) { 
            hitPos = currentPos;       
            return true;               
        }

        totalDistance += dist;         

        if (totalDistance > MAX_DISTANCE) { 
            break;
        }
    }

    hitPos = vec3(0.0);
    return false;
}

vec3 calculateNormal(vec3 p) {
    const vec3 epsilon = vec3(0.001, 0.0, 0.0);
    float dx = sdBox(p + epsilon.xyy, vec3(1.0)) - sdBox(p - epsilon.xyy, vec3(1.0));
    float dy = sdBox(p + epsilon.yxy, vec3(1.0)) - sdBox(p - epsilon.yxy, vec3(1.0));
    float dz = sdBox(p + epsilon.yyx, vec3(1.0)) - sdBox(p - epsilon.yyx, vec3(1.0));
    return normalize(vec3(dx, dy, dz));
}


void main() {
    vec4 pixel = texture(tex, texCoord);
    if (pixel.a < 0.01) discard;

    vec3 hitPos;
    vec3 rayDir = calculateRayDirection(gl_FragCoord.xy);
    vec3 rayPos = uViewPos;

    if (raymarch(rayPos, rayDir, hitPos)) {

        vec3 normal = calculateNormal(hitPos);

        // 반사 적용
        vec3 reflectedDir = reflect(rayDir, normal);
        vec3 reflectionColor = texture(cubeTex, reflectedDir).rgb;

        // 굴절 적용
        float refractionIndex = 1.0 / 1.33;
        vec3 refractedDir = refract(rayDir, normal, refractionIndex);
        vec3 refractedColor = texture(tex, texCoord + refractedDir.xy * 0.1).rgb;

        vec3 finalColor = mix(refractedColor, reflectionColor, 0.5);
        fragColor = vec4(finalColor, 1.0);

    }
    else {
        fragColor = pixel;
    }
    

}
