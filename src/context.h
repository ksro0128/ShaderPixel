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

    // texture
    TextureUPtr m_groundAlbedo;
    TextureUPtr m_groundNormal;
    TextureUPtr m_hdrMap;
    CubeTexturePtr m_hdrCubeMap;

    // mesh
    MeshUPtr m_box;
    MeshUPtr m_plane;
    MeshUPtr m_sphere;

    //framebuffer
    FramebufferUPtr m_framebuffer;
    FramebufferUPtr m_testFramebuffer;

    // screen size
    int m_width {1920};
    int m_height {1080};

    // camera parameter
    bool m_cameraControl { false };
    glm::vec2 m_prevMousePos { glm::vec2(0.0f) };
    float m_cameraPitch { 0.0f };
    float m_cameraYaw { 0.0f };
    glm::vec3 m_cameraFront { glm::vec3(0.0f, -1.0f, 0.0f) };
    glm::vec3 m_cameraPos { glm::vec3(0.0f, 1.8f, 8.0f) };
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

    
};

#endif // __CONTEXT_H__