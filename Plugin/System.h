#pragma once

#include "Common.h"

namespace KlakSpout {

// Singleton system class that manages DX11/12 device/context objects
class System final
{
public:

    System(IUnityInterfaces* unity)
      : _unity(unity),
        isD3D12(getGraphics()->GetRenderer() == kUnityGfxRendererD3D12) {}

    void shutdown()
    {
        _d3d11on12 = nullptr;
        _d3d11_device = nullptr;
        _d3d11_context = nullptr;
    }

    IUnityGraphics* getGraphics() const
    {
        return _unity->Get<IUnityGraphics>();
    }

    WRL::ComPtr<ID3D12Device> getD3D12Device() const
    {
        return _unity->Get<IUnityGraphicsD3D12v6>()->GetDevice();
    }

    WRL::ComPtr<ID3D11On12Device> getD3D11On12Device()
    {
        if (!_d3d11_device) PrepareD3D11On12();
        WRL::ComPtr<ID3D11On12Device> d3d11on12;
        _d3d11_device.As(&d3d11on12);
        return d3d11on12;
    }

    WRL::ComPtr<ID3D11Device> getD3D11Device()
    {
        if (isD3D12)
        {
            if (!_d3d11_device) PrepareD3D11On12();
            return _d3d11_device;
        }
        else
        {
            return _unity->Get<IUnityGraphicsD3D11>()->GetDevice();
        }
    }

    WRL::ComPtr<ID3D11DeviceContext> getD3D11Context()
    {
        if (isD3D12)
        {
            if (!_d3d11_context) PrepareD3D11On12();
            return _d3d11_context;
        }
        else
        {
            WRL::ComPtr<ID3D11DeviceContext> ctx;
            _unity->Get<IUnityGraphicsD3D11>()
              ->GetDevice()->GetImmediateContext(&ctx);
            return ctx;
        }
    }

private:

    IUnityInterfaces* _unity;

public:

    spoutSenderNames spout;
    const bool isD3D12;

public:

    WRL::ComPtr<ID3D11On12Device> _d3d11on12;
    WRL::ComPtr<ID3D11Device> _d3d11_device;
    WRL::ComPtr<ID3D11DeviceContext> _d3d11_context;

    void PrepareD3D11On12()
    {
        // Command queue array
        IUnknown* queues[]
          = { _unity->Get<IUnityGraphicsD3D12v6>()->GetCommandQueue() };

        // Create a D3D11-on-12 device.
        D3D11On12CreateDevice
          (getD3D12Device().Get(), 0, nullptr, 0,
           queues, 1, 0, &_d3d11_device, &_d3d11_context, nullptr);
    }
};

// Singleton instance
inline std::unique_ptr<System> _system;

} // namespace KlakSpout
