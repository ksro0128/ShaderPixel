## 프로젝트 소개  
**ShaderPixel**은 OpenGL을 활용한 실시간 셰이더 렌더링 프로젝트입니다.  
이 프로젝트에서는 다양한 그래픽 기술을 실험하고, 셰이더를 활용해 멋진 효과를 만들어 볼 수 있습니다.  
마치 셰이더 갤러리처럼, 여러 가지 셰이더를 적용한 장면을 직접 탐험하면서 실시간으로 렌더링된 그래픽을 감상할 수 있습니다.  

---

## 주요 기능  
- **실시간 셰이더 렌더링**  
  - OpenGL 4.5를 사용하여 다양한 셰이더 효과 구현  
- **환경 및 조명 시스템**  
  - Skybox를 활용하여 자연스러운 배경 구현  
  - 바닥에 Normal Map을 적용하여 더 디테일한 표면 효과 연출  
  - 하나의 광원(Light Source)을 중심으로 모든 셰이더 오브젝트가 반응하도록 설계  
- **다양한 셰이더 효과**  
  - Mandelbox (프랙탈) + 조명  
  - 3D 프랙탈 (AO & Shadow)  
  - IFS (Iterated Function System)  
  - 볼륨 레이마칭(Volumetric Raymarching)  
  - 투명한 오브젝트 렌더링 (볼륨 조명)  
  - 포털 효과 (다른 세계로 보이는 창)  
  - 2D/3D 커스텀 셰이더  
- **사용자 조작 가능**  
  - `WASD` + 마우스로 자유롭게 이동  
  - 카메라를 이동하며 셰이더들이 적용된 오브젝트를 다양한 각도에서 감상 가능  

---

## Shader Gallery

| 오브젝트 |   |
|----------|---------|
| **Mandelbox** | ![Mandelbox](https://github.com/ksro0128/ShaderPixel/blob/main/docs/images/Mandelbox.png) |
| **3D Fractal** | ![3D Fractal](https://github.com/ksro0128/ShaderPixel/blob/main/docs/images/3D%20Fractal.gif) |
| **IFS** | ![IFS](https://github.com/ksro0128/ShaderPixel/blob/main/docs/images/IFS.png) |
| **볼륨 레이마칭** | ![Volumetric Raymarching](https://github.com/ksro0128/ShaderPixel/blob/main/docs/images/Volumetric%20Raymarching.png) |
| **반투명한 오브젝트** | ![Translucent Object](https://github.com/ksro0128/ShaderPixel/blob/main/docs/images/Translucent%20Object.png) |
| **포털 효과** | ![Portal](https://github.com/ksro0128/ShaderPixel/blob/main/docs/images/Portal.gif) |
| **2D Shader** | ![2D Shader](https://github.com/ksro0128/ShaderPixel/blob/main/docs/images/2D%20Shader.gif) |
| **3D Shader** | ![3D Shader](https://github.com/ksro0128/ShaderPixel/blob/main/docs/images/3D%20Shader.gif) |




## Installation

### 1. 프로젝트 클론
```sh
git clone https://github.com/your-username/ShaderPixel.git
cd ShaderPixel
```

### 2. 빌드 방법

#### 🔹 **CMake만 사용하여 빌드하는 방법**
1. `cmake` 설정  
   ```sh
   cmake -B build -DCMAKE_BUILD_TYPE=Debug
   ```
2. 빌드  
   ```sh
   cmake --build build
   ```
3. 실행  
   ```sh
   ./build/Debug/shaderpixel
   ```

#### 🔹 **Makefile을 사용하여 빌드하는 방법**  
이 프로젝트에는 `Makefile`이 포함되어 있으며, `make` 명령어를 실행하면 자동으로 `CMake` 설정과 빌드가 수행됩니다.

```sh
make
```
> 내부적으로 `cmake` 설정 및 `cmake --build`가 자동으로 실행됩니다.

빌드가 완료되면 실행 파일을 다음과 같이 실행할 수 있습니다.
```sh
./shaderpixel
```
