cmake_minimum_required(VERSION 3.15)

project(KlakSpout)

set(CMAKE_INSTALL_PREFIX ${CMAKE_SOURCE_DIR}/../Packages/jp.keijiro.klak.spout/Plugin CACHE PATH "KlakSpout DLL installation directory" FORCE)

set(Sources
    Unity/IUnityGraphics.h
    Unity/IUnityGraphicsD3D11.h
    Unity/IUnityGraphicsD3D12.h
    Unity/IUnityInterface.h
    Unity/IUnityProfiler.h
    Unity/IUnityRenderingExtensions.h

    Spout/SpoutGL/Spout.h
    Spout/SpoutGL/SpoutCommon.h
    Spout/SpoutGL/SpoutCopy.h
    Spout/SpoutGL/SpoutDirectX.h
    Spout/SpoutGL/SpoutFrameCount.h
    Spout/SpoutGL/SpoutGL.h
    Spout/SpoutGL/SpoutGLextensions.h
    Spout/SpoutGL/SpoutReceiver.h
    Spout/SpoutGL/SpoutSender.h
    Spout/SpoutGL/SpoutSenderNames.h
    Spout/SpoutGL/SpoutSharedMemory.h
    Spout/SpoutGL/SpoutUtils.h
    Spout/SpoutGL/Spout.cpp
    Spout/SpoutGL/SpoutCopy.cpp
    Spout/SpoutGL/SpoutDirectX.cpp
    Spout/SpoutGL/SpoutFrameCount.cpp
    Spout/SpoutGL/SpoutGL.cpp
    Spout/SpoutGL/SpoutGLextensions.cpp
    Spout/SpoutGL/SpoutReceiver.cpp
    Spout/SpoutGL/SpoutSender.cpp
    Spout/SpoutGL/SpoutSenderNames.cpp
    Spout/SpoutGL/SpoutSharedMemory.cpp
    Spout/SpoutGL/SpoutUtils.cpp

    # Spout/SpoutDirectX/SpoutDX/SpoutDX.h
    # Spout/SpoutDirectX/SpoutDX/SpoutDX.cpp
    # Spout/SpoutDirectX/SpoutDX/SpoutDX12/SpoutDX12.h
    # Spout/SpoutDirectX/SpoutDX/SpoutDX12/SpoutDX12.cpp

    KlakSpout/KlakSpoutGlobals.h
    KlakSpout/KlakSpoutSharedObject.h
    KlakSpout/KlakSpout.cpp
)

set(Link
    opengl32
    kernel32
    user32
    gdi32
    winspool
    comdlg32
    advapi32
    shell32
    ole32
    oleaut32
    uuid
    odbc32
    odbccp32
    d3d9
    d3d11
    d3d12
    DXGI
    shlwapi
    Version
)

add_library(KlakSpout SHARED ${Sources} )
target_link_libraries(KlakSpout PRIVATE ${Link} )
target_compile_definitions(KlakSpout PRIVATE SPOUT_BUILD_DLL)

target_compile_definitions(KlakSpout PRIVATE $<$<CONFIG:DEBUG>:KLAKSPOUT_DEBUG>)
target_compile_definitions(KlakSpout PRIVATE $<$<CONFIG:RELEASE>:KLAKSPOUT_RELEASE>)

# Enable all compiler warnings
if(MSVC)
    target_compile_options(KlakSpout PRIVATE /W4)
else()
    target_compile_options(KlakSpout PRIVATE -Wall)
endif()

# From Spout's CMake
if(NOT MSVC)
    target_compile_options(KlakSpout PRIVATE -msse4)
endif()

# 32 or 64 bit library
if(CMAKE_SIZEOF_VOID_P EQUAL 4)
    install(TARGETS KlakSpout RUNTIME DESTINATION ${CMAKE_INSTALL_PREFIX}/x86)
else()
    install(TARGETS KlakSpout RUNTIME DESTINATION ${CMAKE_INSTALL_PREFIX}/x86_64)
endif()
