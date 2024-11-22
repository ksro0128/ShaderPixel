#include "context.h"
#include "image.h"
#include <imgui.h>

ContextUPtr Context::Create() {
    auto context = ContextUPtr(new Context());
    if (!context->Init())
        return nullptr;
    return std::move(context);
}

void Context::ProcessInput(GLFWwindow* window) {
    if (!m_cameraControl)
        return;
    const float cameraSpeed = 0.05f;
    if (glfwGetKey(window, GLFW_KEY_W) == GLFW_PRESS)
        m_cameraPos += cameraSpeed * m_cameraFront;
    if (glfwGetKey(window, GLFW_KEY_S) == GLFW_PRESS)
        m_cameraPos -= cameraSpeed * m_cameraFront;

    auto cameraRight = glm::normalize(glm::cross(m_cameraUp, -m_cameraFront));
    if (glfwGetKey(window, GLFW_KEY_D) == GLFW_PRESS)
        m_cameraPos += cameraSpeed * cameraRight;
    if (glfwGetKey(window, GLFW_KEY_A) == GLFW_PRESS)
        m_cameraPos -= cameraSpeed * cameraRight;    

    auto cameraUp = glm::normalize(glm::cross(-m_cameraFront, cameraRight));
    if (glfwGetKey(window, GLFW_KEY_E) == GLFW_PRESS)
        m_cameraPos += cameraSpeed * cameraUp;
    if (glfwGetKey(window, GLFW_KEY_Q) == GLFW_PRESS)
        m_cameraPos -= cameraSpeed * cameraUp;
    m_cameraPos.y = 1.8f;
    float mapSize = 30.0f;
    if (m_cameraPos.x > mapSize / 2) m_cameraPos.x = mapSize / 2;
    if (m_cameraPos.x < -mapSize / 2) m_cameraPos.x = -mapSize / 2;
    if (m_cameraPos.z > mapSize / 2) m_cameraPos.z = mapSize / 2;
    if (m_cameraPos.z < -mapSize / 2) m_cameraPos.z = -mapSize / 2;
}

void Context::Reshape(int width, int height) {
    if (width == 0) width = 1;
    if (height == 0) height = 1;
    m_width = width;
    m_height = height;
    glViewport(0, 0, m_width, m_height);

    // framebuffer create
    m_framebuffer = Framebuffer::Create({Texture::Create(width, height, GL_RGBA)});
    m_testFramebuffer = Framebuffer::Create({Texture::Create(width, height, GL_RGBA)});
    m_anotherWorldFramebuffer = Framebuffer::Create({Texture::Create(width, height, GL_RGBA)});

}

void Context::MouseMove(double x, double y) {
    if (!m_cameraControl)
        return;
    auto pos = glm::vec2((float)x, (float)y);
    auto deltaPos = pos - m_prevMousePos;

    const float cameraRotSpeed = 0.8f;
    m_cameraYaw -= deltaPos.x * cameraRotSpeed;
    m_cameraPitch -= deltaPos.y * cameraRotSpeed;

    if (m_cameraYaw < 0.0f)   m_cameraYaw += 360.0f;
    if (m_cameraYaw > 360.0f) m_cameraYaw -= 360.0f;

    if (m_cameraPitch > 89.0f)  m_cameraPitch = 89.0f;
    if (m_cameraPitch < -89.0f) m_cameraPitch = -89.0f;

    m_prevMousePos = pos;
}

void Context::MouseButton(int button, int action, double x, double y) {
    if (button == GLFW_MOUSE_BUTTON_RIGHT) {
        if (action == GLFW_PRESS) {
            m_prevMousePos = glm::vec2((float)x, (float)y);
            m_cameraControl = true;
        }
        else if (action == GLFW_RELEASE) {
            m_cameraControl = false;
        }
    }
}

bool Context::Init() {
    glEnable(GL_MULTISAMPLE);
    glEnable(GL_DEPTH_TEST);
    glEnable(GL_TEXTURE_CUBE_MAP_SEAMLESS);
    glClearColor(0.1f, 0.2f, 0.3f, 0.0f);

    m_box = Mesh::CreateBox();
    m_plane = Mesh::CreatePlane();
    m_sphere = Mesh::CreateSphere();
    m_dinoModel = Model::Load("./model/Dino.vox.obj");
    m_pictureFrame = Model::Load("./model/Moldura Sketchfab.obj");

    m_simpleProgram = Program::Create("./shader/simple.vs", "./shader/simple.fs");
    m_skyboxProgram = Program::Create("./shader/skybox_hdr.vs", "./shader/skybox_hdr.fs");
    m_textureProgram = Program::Create("./shader/texture.vs", "./shader/texture.fs");
    m_normalProgram = Program::Create("./shader/normal.vs", "./shader/normal.fs");
    m_beadProgram = Program::Create("./shader/bead.vs", "./shader/bead.fs");
    m_testProgram = Program::Create("./shader/test.vs", "./shader/test.fs");
    m_cloudProgram = Program::Create("./shader/cloud.vs", "./shader/cloud.fs");
    m_mandelboxProgram = Program::Create("./shader/mandelbox.vs", "./shader/mandelbox.fs");
    m_mandelbulbProgram = Program::Create("./shader/mandelbulb.vs", "./shader/mandelbulb.fs");
    m_spongeProgram = Program::Create("./shader/sponge.vs", "./shader/sponge.fs");


    m_groundAlbedo = Texture::CreateFromImage(Image::Load("./image/Old_Plastered_Stone_Wall_1_Diffuse.png").get());
    m_groundNormal = Texture::CreateFromImage(Image::Load("./image/Old_Plastered_Stone_Wall_1_Normal.png").get());
    m_dinoTexture = Texture::CreateFromImage(Image::Load("./model/Dino.vox.png").get());

    // create hdr cubemap
    m_hdrMap = Texture::CreateFromImage(Image::Load("./image/god_rays_sky_dome_8k.hdr").get());
    m_sphericalMapProgram = Program::Create("./shader/spherical_map.vs", "./shader/spherical_map.fs");
    m_hdrCubeMap = CubeTexture::Create(2048, 2048, GL_RGB16F, GL_FLOAT);
    auto cubeFramebuffer = CubeFramebuffer::Create(m_hdrCubeMap);
    auto projection = glm::perspective(glm::radians(90.0f), 1.0f, 0.1f, 10.0f);
    std::vector<glm::mat4> views = {
        glm::lookAt(glm::vec3(0.0f),
        glm::vec3(1.0f, 0.0f, 0.0f), glm::vec3(0.0f, -1.0f, 0.0f)),
        glm::lookAt(glm::vec3(0.0f),
        glm::vec3(-1.0f, 0.0f, 0.0f), glm::vec3(0.0f, -1.0f, 0.0f)),
        glm::lookAt(glm::vec3(0.0f),
        glm::vec3(0.0f, 1.0f, 0.0f), glm::vec3(0.0f, 0.0f, 1.0f)),
        glm::lookAt(glm::vec3(0.0f),
        glm::vec3(0.0f, -1.0f, 0.0f), glm::vec3(0.0f, 0.0f, -1.0f)),
        glm::lookAt(glm::vec3(0.0f),
        glm::vec3(0.0f, 0.0f, 1.0f), glm::vec3(0.0f, -1.0f, 0.0f)),
        glm::lookAt(glm::vec3(0.0f),
        glm::vec3(0.0f, 0.0f, -1.0f), glm::vec3(0.0f, -1.0f, 0.0f)),
    };
    m_sphericalMapProgram->Use();
    m_sphericalMapProgram->SetUniform("tex", 0);
    m_hdrMap->Bind();
    glViewport(0, 0, 2048, 2048);
    for (int i = 0; i < (int)views.size(); i++) {
        cubeFramebuffer->Bind(i);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        m_sphericalMapProgram->SetUniform("transform", projection * views[i]);
        m_box->Draw(m_sphericalMapProgram.get());
    }
    Framebuffer::BindToDefault();
    glViewport(0, 0, m_width, m_height);

    m_anotherWorldHdrMap = Texture::CreateFromImage(Image::Load("./image/dug_up_dark_soil_in_the_field_8k.hdr").get());
    m_anotherWorldCubeMap = CubeTexture::Create(2048, 2048, GL_RGB16F, GL_FLOAT);
    cubeFramebuffer = CubeFramebuffer::Create(m_anotherWorldCubeMap);
    projection = glm::perspective(glm::radians(90.0f), 1.0f, 0.1f, 10.0f);
    m_sphericalMapProgram->Use();
    m_sphericalMapProgram->SetUniform("tex", 0);
    m_anotherWorldHdrMap->Bind();
    glViewport(0, 0, 2048, 2048);
    for (int i = 0; i < (int)views.size(); i++) {
        cubeFramebuffer->Bind(i);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        m_sphericalMapProgram->SetUniform("transform", projection * views[i]);
        m_box->Draw(m_sphericalMapProgram.get());
    }
    Framebuffer::BindToDefault();
    glViewport(0, 0, m_width, m_height);

    return true;
}

void Context::Render() {
    if (ImGui::Begin("ui window")) {
        ImGui::DragFloat3("camera pos", glm::value_ptr(m_cameraPos), 0.01f);
        ImGui::DragFloat("camera yaw", &m_cameraYaw, 0.5f);
        ImGui::DragFloat("camera pitch", &m_cameraPitch, 0.5f, -89.0f, 89.0f);
        ImGui::DragFloat3("light pos", glm::value_ptr(m_lightPos), 0.01f);
        ImGui::Separator();
        if (ImGui::Button("reset camera")) {
            m_cameraYaw = 0.0f;
            m_cameraPitch = 0.0f;
            m_cameraPos = glm::vec3(0.0f, 1.8f, 0.0f);
        }
        ImGui::Separator();
        // bool 토글
        ImGui::Checkbox("BeadDiffuse", &m_diffuseBead);
        ImGui::Checkbox("BeadSpecular", &m_specularBead);

        ImGui::Separator();
        ImGui::Checkbox("Cloud Obstacle ON", &m_obstacleOn);
    }
    ImGui::End();

    m_cameraFront =
        glm::rotate(glm::mat4(1.0f),
        glm::radians(m_cameraYaw), glm::vec3(0.0f, 1.0f, 0.0f)) *
        glm::rotate(glm::mat4(1.0f),
        glm::radians(m_cameraPitch), glm::vec3(1.0f, 0.0f, 0.0f)) *
        glm::vec4(0.0f, 0.0f, -1.0f, 0.0f);
    auto projection = glm::perspective(glm::radians(45.0f),
        (float)m_width / (float)m_height, 0.01f, 150.0f);
    auto view = glm::lookAt(
        m_cameraPos,
        m_cameraPos + m_cameraFront,
        m_cameraUp);
    auto model = glm::mat4(1.0f);

    // start another world
    auto anotherWorldCameraFront = glm::normalize(m_anotherWorldPos - m_cameraPos);
    auto anotherWorldProjection = glm::perspective(glm::radians(45.0f),
        (float)m_width / (float)m_height, 0.01f, 150.0f);
    auto anotherWorldView = glm::lookAt(
        m_anotherWorldPos,
        m_anotherWorldPos + anotherWorldCameraFront,
        m_cameraUp);
    m_anotherWorldFramebuffer->Bind();
    auto& colorAttachmentAW = m_anotherWorldFramebuffer->GetColorAttachment(0);
    glViewport(0, 0, m_width, m_height);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    glDepthFunc(GL_LEQUAL);
    m_skyboxProgram->Use();
    m_skyboxProgram->SetUniform("projection", anotherWorldProjection);
    m_skyboxProgram->SetUniform("view", anotherWorldView);
    m_skyboxProgram->SetUniform("cubeMap", 0);
    m_anotherWorldCubeMap->Bind();
    m_box->Draw(m_skyboxProgram.get());
    glDepthFunc(GL_LESS);

    glActiveTexture(GL_TEXTURE0);
    m_dinoTexture->Bind();
    for (int i = 0; i < 10; i++) {
        for (int j = 0; j < 10; j++) {
            m_textureProgram->Use();
            model = glm::mat4(1.0f);
            model = glm::translate(model, glm::vec3(-10.0f + j * 4.0f, 0.0f, 7.5f + i * 4.0f));
            model = glm::scale(model, glm::vec3(0.5f));
            model = glm::rotate(model, glm::radians(180.0f), glm::vec3(0.0f, 1.0f, 0.0f));
            m_textureProgram->SetUniform("transform", anotherWorldProjection * anotherWorldView * model);
            m_textureProgram->SetUniform("tex", 0);
            m_dinoModel->Draw(m_textureProgram.get());
        }
    }
    // end another world


    m_framebuffer->Bind();
    auto& colorAttachment = m_framebuffer->GetColorAttachment(0);
    glViewport(0, 0, m_width, m_height);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);


    // start ground
    m_normalProgram->Use();
    m_normalProgram->SetUniform("viewPos", m_cameraPos);
    m_normalProgram->SetUniform("lightPos", m_lightPos);
    glActiveTexture(GL_TEXTURE0);
    m_groundAlbedo->Bind();
    m_normalProgram->SetUniform("diffuse", 0);
    glActiveTexture(GL_TEXTURE1);
    m_groundNormal->Bind();
    m_normalProgram->SetUniform("normalMap", 1);
    glActiveTexture(GL_TEXTURE0);
    model = glm::mat4(1.0f);
    model = glm::scale(model, glm::vec3(30.0f));
    model = glm::rotate(model, glm::radians(-90.0f), glm::vec3(1.0f, 0.0f, 0.0f));
    m_normalProgram->SetUniform("transform", projection * view * model);
    m_normalProgram->SetUniform("modelTransform", model);
    m_plane->Draw(m_normalProgram.get());
    // end ground

    // start light
    m_simpleProgram->Use();
    model = glm::mat4(1.0f);
    model = glm::translate(model, m_lightPos);
    model = glm::scale(model, glm::vec3(0.1f));
    m_simpleProgram->SetUniform("transform", projection * view * model);
    m_simpleProgram->SetUniform("color", glm::vec4(1.0f, 1.0f, 1.0f, 1.0f));
    m_sphere->Draw(m_simpleProgram.get());
    // end light

    // start skybox
    glDepthFunc(GL_LEQUAL);
    m_skyboxProgram->Use();
    m_skyboxProgram->SetUniform("projection", projection);
    m_skyboxProgram->SetUniform("view", view);
    m_skyboxProgram->SetUniform("cubeMap", 0);
    m_hdrCubeMap->Bind();
    m_box->Draw(m_skyboxProgram.get());
    glDepthFunc(GL_LESS);
    // end skybox

    // start bead
    //blending
    glEnable(GL_BLEND);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    m_beadProgram->Use();
    model = glm::mat4(1.0f);
    model = glm::translate(model, m_beadPos);
    model = glm::scale(model, glm::vec3(2.1f));
    m_beadProgram->SetUniform("uView", view);
    m_beadProgram->SetUniform("uProjection", projection);
    m_beadProgram->SetUniform("uModel", model);
    m_beadProgram->SetUniform("uTransform", projection * view * model);
    m_beadProgram->SetUniform("uCenter", m_beadPos);
    m_beadProgram->SetUniform("uViewPos", m_cameraPos);
    m_beadProgram->SetUniform("uResolution", glm::vec2(m_width, m_height));
    m_beadProgram->SetUniform("uLightPos", m_lightPos);
    m_beadProgram->SetUniform("uDiffuse", m_diffuseBead);
    m_beadProgram->SetUniform("uSpecular", m_specularBead);
    m_sphere->Draw(m_beadProgram.get());
    glDisable(GL_BLEND);
    // end bead


    // start mandelbox
    m_mandelboxProgram->Use();
    model = glm::mat4(1.0f);
    model = glm::translate(model, m_mandelboxPos);
    model = glm::scale(model, glm::vec3(4.0f));
    m_mandelboxProgram->SetUniform("uView", view);
    m_mandelboxProgram->SetUniform("uProjection", projection);
    m_mandelboxProgram->SetUniform("uModel", model);
    m_mandelboxProgram->SetUniform("uTransform", projection * view * model);
    m_mandelboxProgram->SetUniform("uCenter", m_mandelboxPos);
    m_mandelboxProgram->SetUniform("uViewPos", m_cameraPos);
    m_mandelboxProgram->SetUniform("uResolution", glm::vec2(m_width, m_height));
    m_mandelboxProgram->SetUniform("uLightPos", m_lightPos);
    m_box->Draw(m_mandelboxProgram.get());
    // end mandelbox


    // start mandelbulb
    m_mandelbulbProgram->Use();
    model = glm::mat4(1.0f);
    model = glm::translate(model, m_mandelbulbPos);
    model = glm::scale(model, glm::vec3(3.0f));
    m_mandelbulbProgram->SetUniform("uView", view);
    m_mandelbulbProgram->SetUniform("uProjection", projection);
    m_mandelbulbProgram->SetUniform("uModel", model);
    m_mandelbulbProgram->SetUniform("uTransform", projection * view * model);
    m_mandelbulbProgram->SetUniform("uCenter", m_mandelbulbPos);
    m_mandelbulbProgram->SetUniform("uViewPos", m_cameraPos);
    m_mandelbulbProgram->SetUniform("uResolution", glm::vec2(m_width, m_height));
    m_mandelbulbProgram->SetUniform("uLightPos", m_lightPos);
    m_sphere->Draw(m_mandelbulbProgram.get());
    // end mandelbulb

    // start sponge
    m_spongeProgram->Use();
    model = glm::mat4(1.0f);
    model = glm::translate(model, m_spongePos);
    model = glm::scale(model, glm::vec3(2.0f));
    m_spongeProgram->SetUniform("uView", view);
    m_spongeProgram->SetUniform("uProjection", projection);
    m_spongeProgram->SetUniform("uModel", model);
    m_spongeProgram->SetUniform("uTransform", projection * view * model);
    m_spongeProgram->SetUniform("uCenter", m_spongePos);
    m_spongeProgram->SetUniform("uViewPos", m_cameraPos);
    m_spongeProgram->SetUniform("uResolution", glm::vec2(m_width, m_height));
    m_spongeProgram->SetUniform("uLightPos", m_lightPos);
    m_box->Draw(m_spongeProgram.get());
    // end sponge

    // start another world
    m_textureProgram->Use();
    model = glm::mat4(1.0f);
    model = glm::translate(model, m_anotherWorldPos);
    model = glm::scale(model, glm::vec3(2.0f));
    model = glm::rotate(model, glm::radians(180.f), glm::vec3(0.0f, 1.0f, 0.0f));
    m_textureProgram->SetUniform("transform", projection * view * model);
    glActiveTexture(GL_TEXTURE0);
    colorAttachmentAW->Bind();
    m_textureProgram->SetUniform("tex", 0);
    m_plane->Draw(m_textureProgram.get());

    m_simpleProgram->Use();
    model = glm::mat4(1.0f);
    model = glm::translate(model, m_anotherWorldPos);
    model = glm::scale(model, glm::vec3(1.2f, 1.0f, 1.0f));
    model = glm::rotate(model, glm::radians(180.f), glm::vec3(0.0f, 1.0f, 0.0f));
    m_simpleProgram->SetUniform("transform", projection * view * model);
    m_simpleProgram->SetUniform("color", glm::vec4(1.0f, 0.8f, 0.6f, 1.0f));
    m_pictureFrame->Draw(m_simpleProgram.get());

    // end another world
    


    // start cloud
    Framebuffer::BindToDefault();

    // m_testFramebuffer->Bind();
    // auto& testColorAttachment = m_testFramebuffer->GetColorAttachment(0);
    // glViewport(0, 0, m_width, m_height);
    // glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    m_cloudProgram->Use();
    glDepthFunc(GL_LEQUAL);
    glViewport(0, 0, m_width, m_height);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    glActiveTexture(GL_TEXTURE0);
    colorAttachment->Bind();
    m_cloudProgram->SetUniform("tex", 0);

    model = glm::mat4(1.0f);
    m_cloudProgram->SetUniform("uView", view);
    m_cloudProgram->SetUniform("uProjection", projection);
    m_cloudProgram->SetUniform("uTransform", glm::scale(glm::mat4(1.0f), glm::vec3(2.0f)));
    m_cloudProgram->SetUniform("uCenter", m_cloudPos);
    m_cloudProgram->SetUniform("uViewPos", m_cameraPos);
    m_cloudProgram->SetUniform("uResolution", glm::vec2(m_width, m_height));
    m_cloudProgram->SetUniform("uLightPos", m_lightPos);
    m_cloudProgram->SetUniform("uObstaclePos", m_obstaclePos);
    m_cloudProgram->SetUniform("uObstacleOn", m_obstacleOn);
    m_plane->Draw(m_cloudProgram.get());
    glDepthFunc(GL_LESS);
    // end cloud


    // Framebuffer::BindToDefault();
    // glViewport(0, 0, m_width, m_height);
    // glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    // m_testProgram->Use();
    // glActiveTexture(GL_TEXTURE0);
    // testColorAttachment->Bind();
    // model = glm::mat4(1.0f);
    // m_testProgram->SetUniform("uView", view);
    // m_testProgram->SetUniform("uProjection", projection);
    // m_testProgram->SetUniform("uTransform", glm::scale(glm::mat4(1.0f), glm::vec3(2.0f)));
    // m_testProgram->SetUniform("uCenter", m_spongePos);
    // m_testProgram->SetUniform("uViewPos", m_cameraPos);
    // m_testProgram->SetUniform("uResolution", glm::vec2(m_width, m_height));
    // m_testProgram->SetUniform("uLightPos", m_lightPos);
    // m_testProgram->SetUniform("tex", 0);
    // m_plane->Draw(m_testProgram.get());

}