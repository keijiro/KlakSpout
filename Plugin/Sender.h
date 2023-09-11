#pragma once

#include "Common.h"
#include "System.h"

namespace KlakSpout {

// DX11/12 compatible Spout sender class
class Sender final
{
public:

    Sender(const char* name, int width, int height)
      : _name(name), _width(width), _height(height) {}

    ~Sender()
    {
        if (_texture)
        {
            _system->spout.ReleaseSenderName(_name.c_str());
            _texture = nullptr;
        }
    }

    void update(IUnknown* source)
    {
        // Lazy initialization
        if (!_texture) initialize();

        WRL::ComPtr<IUnknown> unknown(source);

        if (_system->isD3D12)
        {
            // DX12: Texture update
            WRL::ComPtr<ID3D12Resource> d3d12;
            unknown.As(&d3d12);
            updateTexture(d3d12.Get());
        }
        else
        {
            // DX11: Texture update
            WRL::ComPtr<ID3D11Resource> d3d11;
            unknown.As(&d3d11);
            updateTexture(d3d11.Get());
        }
        
        // update frame count, only took into account if FrameCount is enabled
        // in the registry, which should have be done in the initializer
        updateFrameCount();
    }

private:

    std::string _name;
    int _width, _height;
    WRL::ComPtr<ID3D11Texture2D> _texture;
    HANDLE _hSemaphore;

    void updateFrameCount() {
        const DWORD dwWaitResult = WaitForSingleObject(_hSemaphore, 0);
        switch (dwWaitResult) {
        case WAIT_OBJECT_0:
            // Release the frame counting semaphore to increase it's count.
            // so that the receiver can retrieve the new count.
            // Increment by 2 because WaitForSingleObject decremented it.
            if (ReleaseSemaphore(_hSemaphore, 2, NULL) == false) {
                LogError("Sender::updateFrameCount - ReleaseSemaphore failed", _name, 0);
            }
            return;
        case WAIT_ABANDONED:
            LogError("Sender::updateFrameCount - WAIT_ABANDONED", _name, 0);
            break;
        case WAIT_FAILED:
            LogError("Sender::updateFrameCount - WAIT_FAILED", _name, 0);
            break;
        default:
            break;
        }
    }

    void initialize()
    {
        // Enable FrameCount in the registry, else it will not be sent nor received
        WriteDwordToRegistry(HKEY_CURRENT_USER, "SOFTWARE\\Leading Edge\\Spout", "Framecount", 1);

        // Make a Spout-compatible texture description.
        D3D11_TEXTURE2D_DESC desc = {};
        desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
        desc.Width = _width;
        desc.Height = _height;
        desc.MipLevels = 1;
        desc.ArraySize = 1;
        desc.SampleDesc.Count = 1;
        desc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
        desc.MiscFlags = D3D11_RESOURCE_MISC_SHARED;

        // Create a shared texture.
        auto hres = _system->getD3D11Device()
          ->CreateTexture2D(&desc, nullptr, &_texture);

        if (FAILED(hres))
        {
            LogError("CereateTexture2D", _name, hres);
            return;
        }

        // Retrieve the texture handle.
        HANDLE handle;
        WRL::ComPtr<IDXGIResource> resource;
        _texture.As(&resource);
        resource->GetSharedHandle(&handle);

        // Create a Spout sender object for the shared texture.
        auto res = _system->spout
          .CreateSender(_name.c_str(), _width, _height, handle, desc.Format);

        std::string semaphore_name = _name + "_Count_Semaphore";

        // Create or open a named frame count semaphore with the name
        _hSemaphore = CreateSemaphoreA(
            NULL, // default security attributes
            1, // initial count
            LONG_MAX, // maximum count - LONG_MAX (2147483647) at 60fps = 2071 days
            (LPSTR)semaphore_name.c_str());

        if (!res) LogError("CreateSender", _name, 0);
    }

    void updateTexture(ID3D11Resource* source)
    {
        // Texture copy
        _system->getD3D11Context()->CopyResource(_texture.Get(), source);
    }

    void updateTexture(ID3D12Resource* source)
    {
        auto d3d11on12 = _system->getD3D11On12Device();

        // Wrapping: D3D12 -> D3D11
        D3D11_RESOURCE_FLAGS flags = {};
        WRL::ComPtr<ID3D11Resource> wrap;
        auto hres = d3d11on12->CreateWrappedResource
          (source, &flags,
           D3D12_RESOURCE_STATE_COPY_SOURCE,
           D3D12_RESOURCE_STATE_PRESENT,
           IID_PPV_ARGS(&wrap));

        if (FAILED(hres))
        {
            LogError("CereateWrappedResource", _name, hres);
            return;
        }

        // Texture copy
        auto ctx = _system->getD3D11Context();
        d3d11on12->AcquireWrappedResources(wrap.GetAddressOf(), 1);
        ctx->CopyResource(_texture.Get(), wrap.Get());
        d3d11on12->ReleaseWrappedResources(wrap.GetAddressOf(), 1);
        ctx->Flush();
    }
};

} // namespace KlakSpout
