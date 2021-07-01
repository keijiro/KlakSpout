#pragma once

#include <memory>
#include <d3d11.h>
#include <d3d12.h>
#include <d3d11on12.h>
#include <wrl/client.h> // for ComPtr
#include "Spout/SpoutSenderNames.h"
#include "Unity/IUnityGraphics.h"
#include "Unity/IUnityGraphicsD3D11.h"
#include "Unity/IUnityGraphicsD3D12.h"

namespace KlakSpout {

namespace WRL = Microsoft::WRL; // for ComPtr

static inline void LogError
  (const char* label, const std::string& name, unsigned int code)
{
    std::printf("KlakSpout error: %s (%s) - %x\n", label, name.c_str(), code);
}

} // namespace KlakSpout
