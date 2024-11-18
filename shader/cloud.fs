#version 330 core
out vec4 fragColor;

in mat4 inverseView;
in mat4 inverseProjection;
in vec2 texCoord;

uniform vec3 uCenter;           // 구름 중심 위치
uniform vec3 uViewPos;          // 카메라 위치
uniform vec2 uResolution;       // 렌더링 해상도
uniform vec3 uLightPos;         // 광원 위치
uniform vec3 uObstaclePos;      // 장애물 위치
uniform bool uObstacleOn;        // 장애물 on/ff 1/0

uniform sampler2D tex;

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
    f  = 0.5000 * noise(p); p = m * p * 2.02;
    f += 0.2500 * noise(p); p = m * p * 2.03;
    f += 0.1250 * noise(p);
    return f;
}


float sdSphere(vec3 p, float radius) {
    return length(p) - radius;
}

float sdBox( vec3 p, vec3 b )
{
    vec3 q = abs(p) - b;
    return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float sceneBox(vec3 p) {
    float distance = sdBox(p, vec3(0.5));

    float f = fbm(p);

    return (-distance + f);
}

float scene(vec3 p) {
    float distance = sdSphere(p, 1.0);
    // float distance = sdBox(p, vec3(1.0));

    float f = fbm(p);

    return (-distance + f) / 2;
}

const float MARCH_SIZE = 0.08;
const int MAX_STEPS = 50;
vec3 SUN_POSITION = uLightPos;

vec4 raymarch(vec3 rayOrigin, vec3 rayDirection) {
    float depth = 0.0;
    vec3 p = rayOrigin + depth * rayDirection;
    vec3 sunDirection = normalize(SUN_POSITION - uCenter);

    vec4 res = vec4(0.0);
    float shadow = 1.0;

    for (int i = 0; i < MAX_STEPS; i++) {
        float density = scene(p - uCenter);
        if (density > 0.0) {
            if (uObstacleOn) {
                float isShadow = 0.0;
                vec3 shadowPos = p;
                vec3 shadowSunDirection = sunDirection;
                for (int j = 0; j < 80; j++) { // Shadow ray steps
                    isShadow = sdSphere(shadowPos - uObstaclePos, 0.1);
                    if (isShadow < 0.01) {
                        shadow = mix(1.0, 0.0, exp(-j * 0.1));
                        break;
                    }
                    shadowPos += shadowSunDirection * 0.03;
                    shadowSunDirection = normalize(SUN_POSITION - shadowPos);
                }
            }

            // Inigo Quilez 
            float diffuse = clamp((scene(p - uCenter) - scene((p - uCenter) + 0.3 * sunDirection)) / 0.3, 0.0, 1.0 );
            vec3 lin = vec3(0.60, 0.65, 0.75) * 1.1 + 0.8 * vec3(1.0,0.6,0.3) * diffuse;      
            // Rayleigh scattering
            float phase = 0.75 * (1.0 + pow(dot(normalize(rayDirection), sunDirection), 2.0));
            phase *= 0.5;
            vec3 scatterColor = lin * phase;
            vec4 color = vec4(mix(vec3(1.0,1.0,1.0), scatterColor, density), density );


            color.rgb *= lin;
            color.rgb *= color.a;
            color.rgb *= shadow;
            res += color*(1.0-res.a);
        }

        depth += MARCH_SIZE;
        p = rayOrigin + depth * rayDirection;
    }
    return res;
}


void main() {

	vec4 pixel = texture(tex, texCoord);
	if (pixel.a < 0.01) discard ;

    // ray marching 사용
    vec3 rayDir = calculateRayDirection(gl_FragCoord.xy);
    vec3 rayPos = uViewPos;

    vec3 color = vec3(0.0);
    bool hit = false;
    // 그릴 공간까지 이동
    for (int i = 0; i < 2; i++) {
        float dist = sdSphere(rayPos - uCenter, 2.1);
        if (dist < 0.01) {
            hit = true;
            break;
        }
        rayPos += rayDir * dist;
    }
    if (hit == false) 
		fragColor = pixel;
    ////


    if (uObstacleOn) {
        hit = false;
        vec3 tmpRo = rayPos;
        for (int i = 0; i < 5; i++) {
            float dist = sdSphere(tmpRo - uObstaclePos, 0.1);
            if (dist < 0.01) {
                hit = true;
                break ;
            }
            tmpRo += rayDir * dist;
        }
    }
    else {
        hit == false;
    }

    if (hit)
        color = vec3(1.0, 0.0, 0.0);
    else
        color = pixel.rgb;


    vec4 res = raymarch(rayPos, rayDir);

	// color += pixel.rgb;
    color = color * (1.0 - res.a) + res.rgb;

    fragColor = vec4(color, 1.0);
	
}
