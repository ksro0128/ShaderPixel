#version 330 core

out vec4 fragColor;

in mat4 inverseView;
in mat4 inverseProjection;
in vec3 vNormal;
in vec3 vPosition;

uniform vec3 uCenter;           // 구름 중심 위치
uniform vec3 uViewPos;          // 카메라 위치
uniform vec2 uResolution;       // 렌더링 해상도
uniform vec3 uLightPos;         // 광원 위치
uniform float uTime;            
uniform sampler2D uNoise;

vec3 calculateRayDirection(vec2 fragCoord) {
    // NDC로 변환 (-1, 1 범위로 변환)
    vec4 clipSpacePos = vec4((fragCoord / uResolution) * 2.0 - 1.0, -1.0, 1.0);
    
    // 클립 공간에서 뷰 공간으로 변환
    vec4 viewSpacePos = inverseProjection * clipSpacePos;
    viewSpacePos = vec4(viewSpacePos.xy, -1.0, 0.0); // 방향 벡터이므로 z = -1, w = 0
    
    // 뷰 공간에서 월드 공간으로 변환 및 정규화
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
    f  = 0.5000 * noise(p); p = m * p * 2.02;
    f += 0.2500 * noise(p); p = m * p * 2.03;
    f += 0.1250 * noise(p);
    return f;
}


float sdSphere(vec3 p, float radius) {
    return length(p) - radius;
}

float scene(vec3 p) {
    float distance = sdSphere(p, 1.0);

    float f = fbm(p);

    return (-distance + f) / 4;
}

const float MARCH_SIZE = 0.08;
const int MAX_STEPS = 100;
vec3 SUN_POSITION = uLightPos;

vec4 raymarch(vec3 rayOrigin, vec3 rayDirection) {
    float depth = 0.0;
    vec3 p = rayOrigin + depth * rayDirection;
    vec3 sunDirection = normalize(SUN_POSITION - uCenter);

    vec4 res = vec4(0.0);

    for (int i = 0; i < MAX_STEPS; i++) {
        float density = scene(p - uCenter);
        if (density > 0.0) {
        float diffuse = clamp((scene(p - uCenter) - scene((p - uCenter) + 0.3 * sunDirection)) / 0.3, 0.0, 1.0 );
        vec3 lin = vec3(0.60, 0.65, 0.75) * 1.1 + 0.8 * vec3(1.0,0.6,0.3) * diffuse;      
        vec4 color = vec4(mix(vec3(1.0,1.0,1.0), vec3(0.1, 0.1, 0.1), density), density );
        color.rgb *= lin;
        color.rgb *= color.a;
        res += color*(1.0-res.a);
        }

        depth += MARCH_SIZE;
        p = rayOrigin + depth * rayDirection;
    }

    return res;
}


void main() {

    //카메라가 구 바깥에 있을때는 레이를 두번 쏘기 때문에 걸러준다    
    vec3 viewToSurface = normalize(vPosition - uViewPos);
    float alignment = dot(viewToSurface, vNormal);    
    bool outside = (length(uViewPos - uCenter) > 2.1);
    if (outside) {
        if (alignment > 0.01) {
            discard;
        }
    }
    ////

    // ray marching 사용
    vec3 rayDir = calculateRayDirection(gl_FragCoord.xy);
    vec3 rayPos = uViewPos;

    bool hit = false;
    // 그릴 공간까지 이동
    for (int i = 0; i < 100; i++) {
        float dist = sdSphere(rayPos - uCenter, 4.1);
        if (dist < 0.01) {
            hit = true;
            break;
        }
        rayPos += rayDir * dist;
    }
    if (hit == false) discard;
    ////


    vec4 res = raymarch(rayPos, rayDir);
    vec3 color = res.rgb;

    // Ray Origin - camera
    vec3 ro = rayPos;
    // Ray Direction
    vec3 rd = rayDir;


    // Sun and Sky
    vec3 sunDirection = normalize(uCenter - SUN_POSITION);
    float sun = clamp(dot(sunDirection, rd), 0.0, 1.0 );
    // Base sky color
    color = vec3(0.7,0.7,0.90);
    // Add vertical gradient
    color -= 0.8 * vec3(0.90,0.75,0.90) * rd.y;
    // Add sun color to sky
    color += 0.5 * vec3(1.0,0.5,0.3) * pow(sun, 10.0);





    color = color * (1.0 - res.a) + res.rgb;

    // fragColor = vec4(color, 1.0);
    fragColor = res;
}