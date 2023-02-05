#include <glad/glad.h>
#include <GLFW/glfw3.h>

#include "stb_image.h"

#include <iostream>
#include <cmath>

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

#include "camera.hpp"
#include "shader.hpp"

void framebuffer_size_callback(GLFWwindow *, int, int);
void mouse_callback(GLFWwindow *window, double xpos, double ypos);
void processInput(GLFWwindow *);

Camera camera;
Shader* theShader;

int main()
{
    /* Init glfw*/
    glfwInit();
    /* Major version */
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
    /* Minor version */
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
    /* Use Core profile */
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
    // glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE);


    /* create window */
    GLFWwindow *window = glfwCreateWindow(800, 600, "LearnOpenGL", NULL, NULL);
    if (window == NULL)
    {
        std::cout << "Failed to create GLFW window" << std::endl;
        glfwTerminate();
        return -1;
    }
    glfwMakeContextCurrent(window);

    if (!gladLoadGLLoader((GLADloadproc)glfwGetProcAddress))
    {
        std::cout << "Failed to initialize GLAD" << std::endl;
        return -1;
    }

    std::cout << "Using GPU: " << glGetString(GL_VENDOR) << " " << glGetString(GL_RENDERER) << "\n";
    
    glViewport(0, 0, 800, 600);
    glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);
    glfwSetInputMode(window, GLFW_CURSOR, GLFW_CURSOR_DISABLED);
    glfwSetCursorPosCallback(window, mouse_callback);

    // Create a quad

    float vertices[] = {
        -1.0f, 1.0f, 0.0f,
        1.0f, 1.0f, 0.0f,
        1.0f, -1.0f, 0.0f,
        -1.0f, -1.0f, 0.0f};

    unsigned int indices[] = {
        // note that we start from 0!
        0, 1, 3, // first triangle
        1, 2, 3  // second triangle
    };

    unsigned int VBO, VAO, EBO;
    glGenVertexArrays(1, &VAO);
    glGenBuffers(1, &VBO);
    glGenBuffers(1, &EBO);

    // bind the Vertex Array Object first, then bind and set vertex buffer(s), and then configure vertex attributes(s).
    glBindVertexArray(VAO);

    // VBO with vertices
    glBindBuffer(GL_ARRAY_BUFFER, VBO);
    glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);

    // EBO with indices
    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices), indices, GL_STATIC_DRAW);

    // Enable vertices
    glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void *)0);
    glEnableVertexAttribArray(0);

    // Shader with dummy vertex shader
    theShader = new Shader("shaders/shader.vs", "shaders/shader.fs");
    theShader->use();

    float lastFrame = glfwGetTime(), deltaTime = 0;
    int width, height;
    glfwGetWindowSize(window, &width, &height);

    while (!glfwWindowShouldClose(window))
    {
        // DeltaTime
        float currentFrame = glfwGetTime();
        deltaTime = currentFrame - lastFrame;
        lastFrame = currentFrame;

        // std::cout << "Frametime (ms)" << deltaTime * 1000 << "\n";

        // Clear scene
        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        processInput(window);
        glfwGetWindowSize(window, &width, &height);

        glBindVertexArray(VAO);
        
        camera.update(window, deltaTime);
        theShader->use();
        // std::cout << width << "*" << height << "\n";
        theShader->setVec2("u_resolution", (float)width, (float)height);
        theShader->setFloat("u_time", currentFrame);
        theShader->setFloat("u_deltatime", deltaTime);
        theShader->setVec3("u_camorigin", camera.getPos());
        theShader->setVec3("u_camdir", camera.getFront());
        theShader->setVec3("u_camup", camera.getUp());
        theShader->setMat4("u_projection", camera.getProjection());
        theShader->setMat4("u_view", camera.getView());
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0);

        
        // Swap buffers to avoid tearing
        glfwSwapBuffers(window);
        glfwPollEvents();
    }

    glfwTerminate();
    return 0;
}

void framebuffer_size_callback(GLFWwindow *window, int width, int height)
{
    glViewport(0, 0, width, height);
}

void mouse_callback(GLFWwindow *window, double xpos, double ypos)
{
    camera.mouseCallback(window, xpos, ypos);
}

void processInput(GLFWwindow *window)
{

    if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
        glfwSetWindowShouldClose(window, true);
    if( glfwGetKey(window, GLFW_KEY_0) == GLFW_PRESS){
        std::cout << "Rebuilding main shader" << std::endl;
        delete(theShader);
        theShader = new Shader("shaders/shader.vs", "shaders/shader.fs");
    }
}