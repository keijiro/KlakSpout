#pragma once

#include "Common.h"
#include "System.h"

namespace KlakSpout {

// DX11/12 compatible Spout receiver class
class Receiver final
{
public:

    Receiver(const char* name)
      : _name(name) {}

    ~Receiver()
    {
        _texture = nullptr;
    }

    void update()
    {
        // Search the Spout name list.
        unsigned int width, height;
        HANDLE handle;
        DWORD format;
        auto res = _system->spout
          .CheckSender(_name.c_str(), width, height, handle, format);

        // Do nothing further if the current texture is valid.
        if (res && _texture && _width == width && _height == height) return;

        HRESULT hres;

        if (_system->isD3D12)
        {
            // Handle -> D3D12Resource
            WRL::ComPtr<ID3D12Resource> resource;
            hres = _system->getD3D12Device()
              ->OpenSharedHandle(handle, IID_PPV_ARGS(&resource));
            _texture = resource;
        }
        else
        {
            // Handle -> D3D11Resource
            WRL::ComPtr<ID3D11Resource> resource;
            hres = _system->getD3D11Device()
              ->OpenSharedResource(handle, IID_PPV_ARGS(&resource));
            _texture = resource;
        }

        _width = width;
        _height = height;

        if (FAILED(hres)) LogError("OpenSharedResource", _name, hres);
    }

    // Receiver interop data structure
    // Should match with Klak.Spout.Plugin.ReceiverData (Plugin.cs)
    struct InteropData
    {
        unsigned int width, height;
        void* texture_pointer;
    };

    InteropData getInteropData() const
    {
        return InteropData
          { .width = _width, .height = _height,
            .texture_pointer = _texture.Get() };
    }

private:

    std::string _name;
    unsigned int _width, _height;
    WRL::ComPtr<IUnknown> _texture;
};

} // namespace KlakSpout
