#ifndef __CONTEXT_H__
#define __CONTEXT_H__

#include "common.h"
#include "shader.h"
#include "program.h"
#include "buffer.h"
#include "vertex_layout.h"
#include "texture.h"
#include "mesh.h"
#include "model.h"
#include "framebuffer.h"
#include "shadow_map.h"
#include <algorithm>

enum ObjectType {
    BEAD,
    MANDELBOX,
    MANDELBULB,
    SPONGE,
    WORLD,
    KALEIDOSCOPE,
    CLOUD,
    WATER
};

struct DrawCall {
    int type;
    glm::vec3 pos;
    float distance = 0;
};


CLASS_PTR(Context)
class Context {
public:
    static ContextUPtr Create();
    void Render();
    void ProcessInput(GLFWwindow* window);
    void Reshape(int width, int height);
    void MouseMove(double x, double y);
    void MouseButton(int button, int action, double x, double y);

private:
    Context() {}
    bool Init();
    
    float m_time { 0.0f };

    // shader
    ProgramUPtr m_simpleProgram;                    // simple shader
    ProgramUPtr m_textureProgram;                   // texture shader
    ProgramUPtr m_normalProgram;                    // normal map shader
    ProgramUPtr m_sphericalMapProgram;              // spherical map shader
    ProgramUPtr m_skyboxProgram;                    // skybox shader
    ProgramUPtr m_beadProgram;                      // bead shader
    ProgramUPtr m_testProgram;                      // test shader
    ProgramUPtr m_cloudProgram;                     // cloud shader
    ProgramUPtr m_mandelboxProgram;                 // mandelbox shader
    ProgramUPtr m_mandelbulbProgram;                // mandelbulb shader
    ProgramUPtr m_spongeProgram;                    // menger sponge shader
    ProgramUPtr m_kaleidoscopeProgram;              // kaleidoscope shader
    ProgramUPtr m_waterProgram;                     // water shader

    // texture
    TextureUPtr m_groundAlbedo;
    TextureUPtr m_groundNormal;
    TextureUPtr m_hdrMap;
    TextureUPtr m_anotherWorldHdrMap;
    TextureUPtr m_dinoTexture;
    CubeTexturePtr m_hdrCubeMap;
    CubeTexturePtr m_anotherWorldCubeMap;
    
    TexturePtr colorAttachment1;
    TexturePtr colorAttachment2;
    TexturePtr colorAttachmentAW;
    TexturePtr colorAttachment2D;


    // mesh
    MeshUPtr m_box;
    MeshUPtr m_plane;
    MeshUPtr m_sphere;
    ModelUPtr m_dinoModel;
    ModelUPtr m_pictureFrame;


    //framebuffer
    FramebufferUPtr m_framebuffer1;
    FramebufferUPtr m_framebuffer2;

    FramebufferUPtr m_testFramebuffer;
    FramebufferUPtr m_anotherWorldFramebuffer;
    FramebufferUPtr m_kaleidoscopeFramebuffer;

    // screen size
    int m_width {1920};
    int m_height {1080};

    // camera parameter
    bool m_cameraControl { false };
    glm::vec2 m_prevMousePos { glm::vec2(0.0f) };
    float m_cameraPitch { 0.0f };
    float m_cameraYaw { 0.0f };
    glm::vec3 m_cameraFront { glm::vec3(0.0f, -1.0f, 0.0f) };
    glm::vec3 m_cameraPos { glm::vec3(0.0f, 1.8f, 0.0f) };
    glm::vec3 m_cameraUp { glm::vec3(0.0f, 1.0f, 0.0f) };

    // light parameter
    glm::vec3 m_lightPos { 0.0f, 10.0f, 0.0f };

    // object parameter
        // bead
    glm::vec3 m_beadPos { -7.5f, 1.7f, -7.5f };
    bool m_specularBead { true };
    bool m_diffuseBead { true };

        // cloud
    glm::vec3 m_cloudPos { 0.0f, 1.7f, -7.5f };
    glm::vec3 m_obstaclePos { 0.0f, 2.7f, -7.5f };
    bool m_obstacleOn { false };

        // mandelbox
    glm::vec3 m_mandelboxPos { 7.5f, 2.1f, -7.5f };

        // mandelbulb
    glm::vec3 m_mandelbulbPos { -7.5f, 1.7f, 0.0f };

        // ifs - menger sponge
    glm::vec3 m_spongePos { 7.5f, 1.7f, 0.0f };

        // another world
    glm::vec3 m_anotherWorldPos { 0.0f, 1.7f, 7.5f };

        // 2d shader - kaleidoscope
    glm::vec3 m_kaleidoscopePos { -7.5f, 1.7f, 7.5f };

        // water block
    glm::vec3 m_waterPos { 7.5f, 1.7f, 7.5f };

    void PreRenderAnotherWorld(const glm::mat4& projection, const glm::mat4& view);
    void PreRenderKaleidoscope(const glm::mat4& projection, const glm::mat4& view);

    // 배경 그리기
    void DrawEnvironment(const glm::mat4& projection, const glm::mat4& view);

    void DrawBead(const glm::mat4& projection, const glm::mat4& view);
    void DrawMandelbox(const glm::mat4& projection, const glm::mat4& view);
    void DrawMandelbulb(const glm::mat4& projection, const glm::mat4& view);
    void DrawSponge(const glm::mat4& projection, const glm::mat4& view);
    void DrawAnotherWorld(const glm::mat4& projection, const glm::mat4& view);
    void DrawKaleidoscope(const glm::mat4& projection, const glm::mat4& view);
    void DrawCloud(const glm::mat4& projection, const glm::mat4& view);
    void DrawWater(const glm::mat4& projection, const glm::mat4& view);

    void BindFramebuffer();
    void BindColorAttachment();

    DrawCall m_drawcalls[8];
    void CalDistance();
    void SortDrawCall();
    void DrawAll(const glm::mat4& projection, const glm::mat4& view);
    int m_level {0};

};

#endif // __CONTEXT_H__