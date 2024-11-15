#version 330 core

out vec4 fragColor;

in mat4 inverseView;
in mat4 inverseProjection;
in vec3 vNormal;
in vec3 vPosition;

uniform vec3 uCenter;           // 구 중심 위치
uniform vec3 uViewPos;          // 카메라 위치
uniform vec2 uResolution;       // 렌더링 해상도
uniform vec3 uLightPos;         // 광원 위치


float sdSphere(vec3 p, float s)
{
    return length(p)-s;
}

float sdCappedTorus(vec3 p, vec2 sc, float ra, float rb, float angle)
{
    angle = radians(angle);
    float cosAngle = cos(angle);
    float sinAngle = sin(angle);

    vec3 rotatedP = vec3(
        p.x,                        
        p.y * cosAngle - p.z * sinAngle, 
        p.y * sinAngle + p.z * cosAngle
    );

    rotatedP.x = abs(rotatedP.x);
    float k = (sc.y * rotatedP.x > sc.x * rotatedP.y) 
                ? dot(rotatedP.xy, sc) 
                : length(rotatedP.xy);

    return sqrt(dot(rotatedP, rotatedP) + ra * ra - 2.0 * ra * k) - rb;
}


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


void main() {

    // 카메라가 구 바깥에 있을때는 레이를 두번 쏘기 때문에 걸러준다    
    vec3 viewToSurface = normalize(vPosition - uViewPos);
    float alignment = dot(viewToSurface, vNormal);    
    bool outside = (length(uViewPos - uCenter) > 1.0);
    if (outside) {
        if (alignment < -0.01) {
            discard;
        }
    }
    ////


    // ray marching 사용
    vec3 rayDir = calculateRayDirection(gl_FragCoord.xy);
    vec3 rayPos = uViewPos;
    bool hit = false;
    for (int i = 0; i < 100; i++) {
        float dist = sdSphere(rayPos - uCenter, 1.0f);
        if (dist < 0.01) {
            hit = true;
            break;
        }
        rayPos += rayDir * dist;
    }

    if (hit) {
        vec3 hitPos = rayPos;
        float vol = 0.0;
        float dt = 0.01;
        bool torusHit = false;
        for (int i = 0; i < 400; i++) {
            float dist = sdSphere(rayPos - uCenter, 1.0f);
            if (dist > 0.1) {
                break ;
            }
            for (int j = 0; j < 16; j++) {
                dist = sdCappedTorus(rayPos - uCenter, vec2(0.8, 0.4), 0.8, 0.05, 22.5 * j);
                if (dist < 0.01) {
                    torusHit = true;
                    break ;
                }
            }
            if (torusHit)
                break ;
            rayPos += rayDir * dt;
            vol += dt;
        }
        vol /= 2;
        vol = smoothstep(0.0, 1.0, vol);
        vec3 normal = normalize(hitPos - uCenter);
        
        // 디퓨즈 및 스페큘러 색상 정의
        vec3 objectColor = vec3(0.4, 0.4, 0.8); // 구의 기본 색상
        vec3 lightColor = vec3(1.0);            // 광원의 색상
        vec3 edgeColor = vec3(1.0);
        vec3 torusColor = vec3(0.4, 0.4, 0.0);


        vec3 lightDir = normalize(uLightPos - hitPos);
        vec3 viewDir = normalize(uViewPos - hitPos);

        float fresnel = pow(1.0 - abs(dot(viewDir, normal)), 2.0);
        
        // 디퓨즈 셰이딩
        float diff = max(dot(normal, lightDir), 0.0);
        vec3 diffuse = diff * lightColor * objectColor;

        // 스페큘러 셰이딩
        vec3 reflectDir = reflect(-lightDir, normal);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0); // 샤인니스 값 32
        vec3 specular = vec3(1.0) * spec; // 하이라이트 색상 (흰색)

        vec3 lColor = vec3(0.4, 0.4, 0.8);
        vec3 dColor = vec3(0.1, 0.1, 0.4);
        // 최종 색상 계산
        vec3 lightColorSum = mix(diffuse + specular, edgeColor, fresnel);
        vec3 depthColor = mix(lColor, dColor, vol);
        if (torusHit){
            depthColor = mix(torusColor, depthColor, vol);
            vol = 1;
        }
        vec4 finalColor = mix(vec4(lightColorSum, 1.0), vec4(depthColor, 1.0), 0.7);
        finalColor.a = clamp(vol, 0.6, 1.0);
        fragColor = finalColor;
    }
    else {
        discard;
    }


}
