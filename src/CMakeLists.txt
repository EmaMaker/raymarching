cmake_minimum_required(VERSION 3.2)
project(OpenGLTest)

set(SOURCE_FILES main.cpp stb_image.cpp)

add_executable(OpenGLTest ${SOURCE_FILES})

find_package(glfw3 3.3 REQUIRED)
target_link_libraries(OpenGLTest glfw glad glm)
install(TARGETS OpenGLTest DESTINATION ${DIVISIBLE_INSTALL_BIN_DIR})
