#pragma once

#include "Common.h"

namespace KlakSpout {

// Texture format type enumeration
// Should match with Klak.Spout.Format (Format.cs)
enum class Format : int32_t
{
    Unknown, RGBA32, RGBA32_SRGB, BGRA32, BGRA32_SRGB, RGBAHalf, RGBAFloat
};

static inline Format ToFormat(DXGI_FORMAT format)
{
    switch (format)
    {
        case DXGI_FORMAT_R8G8B8A8_UNORM:      return Format::RGBA32;
        case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB: return Format::RGBA32_SRGB;
        case DXGI_FORMAT_B8G8R8A8_UNORM:      return Format::BGRA32;
        case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB: return Format::BGRA32_SRGB;
        case DXGI_FORMAT_R16G16B16A16_FLOAT:  return Format::RGBAHalf;
        case DXGI_FORMAT_R32G32B32A32_FLOAT:  return Format::RGBAFloat;
        default: return Format::Unknown;
    }
}

} // namespace KlakSpout
