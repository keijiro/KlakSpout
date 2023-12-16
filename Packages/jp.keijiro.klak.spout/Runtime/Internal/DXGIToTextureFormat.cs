using System;
using UnityEngine;

namespace Klak.Spout
{
    public static class DXGIToTextureFormat
    {
        public static TextureFormat Convert(DXGIFormat format)
        {
            switch (format)
            {
                case DXGIFormat.DXGI_FORMAT_R8G8B8A8_UNORM:
                    return TextureFormat.RGBA32;
                case DXGIFormat.DXGI_FORMAT_B8G8R8A8_UNORM:
                    return TextureFormat.BGRA32;
                case DXGIFormat.DXGI_FORMAT_R32G32B32A32_FLOAT:
                    return TextureFormat.RGBAFloat;
                case DXGIFormat.DXGI_FORMAT_R16G16B16A16_FLOAT:
                    return TextureFormat.RGBAHalf;
                case DXGIFormat.DXGI_FORMAT_R32G32_FLOAT:
                    return TextureFormat.RGFloat;
                case DXGIFormat.DXGI_FORMAT_R16G16_FLOAT:
                    return TextureFormat.RGHalf;
                case DXGIFormat.DXGI_FORMAT_R32_FLOAT:
                    return TextureFormat.RFloat;
                case DXGIFormat.DXGI_FORMAT_R16_FLOAT:
                    return TextureFormat.RHalf;
                case DXGIFormat.DXGI_FORMAT_UNKNOWN:
                    throw new Exception("Unknown DXGIFormat");
                default:
                    throw new NotImplementedException();
            }
        }
    }
    
    public enum DXGIFormat
    {
        DXGI_FORMAT_UNKNOWN = 0,
        DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
        DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
        DXGI_FORMAT_R32G32_FLOAT = 16,
        DXGI_FORMAT_R8G8B8A8_UNORM = 28,
        DXGI_FORMAT_R16G16_FLOAT = 34,
        DXGI_FORMAT_R32_FLOAT = 41,
        DXGI_FORMAT_R16_FLOAT = 54,
        DXGI_FORMAT_B8G8R8A8_UNORM = 87,
    }
}