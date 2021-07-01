#pragma once

#include "KlakSpoutGlobals.h"
#include <combaseapi.h>

namespace klakspout
{
    // Shared Spout object handler class
    // Not thread safe. The owner should care about it.
    class SharedObject final
    {
    public:

        // Object type
        enum class Type { sender, receiver } const type_;

        // Object attributes
        const std::string name_;
        int width_, height_;
        DXGI_FORMAT format_;

        // For DX11
        ID3D11Resource* d3d11_resource_;
        ID3D11ShaderResourceView* d3d11_resource_view_;

        // Constructor
        SharedObject(Type type, const std::string& name, int width = -1, int height = -1)
            : type_(type), name_(name), width_(width), height_(height),
              d3d11_resource_(nullptr), d3d11_resource_view_(nullptr)
        {
            if (type_ == Type::sender)
                DEBUG_LOG("Sender created (%s)", name_.c_str());
            else
                DEBUG_LOG("Receiver created (%s)", name_.c_str());
        }

        // Destructor
        ~SharedObject()
        {
            releaseInternals();

            if (type_ == Type::sender)
                DEBUG_LOG("Sender disposed (%s)", name_.c_str());
            else
                DEBUG_LOG("Receiver disposed (%s)", name_.c_str());
        }

        // Prohibit use of default constructor and copy operators
        SharedObject() = delete;
        SharedObject(SharedObject&) = delete;
        SharedObject& operator = (const SharedObject&) = delete;

        // Check if it's active.
        bool isActive() const
        {
            return d3d11_resource_ != nullptr;
        }

        // Try activating the object. Returns false when failed.
        bool activate()
        {
            assert(d3d11_resource_ == nullptr && d3d11_resource_view_ == nullptr);
            return type_ == Type::sender ? setupSender() : setupReceiver();
        }

        int sendTexture(void* tex)
        {
            auto& g = Globals::get();

            if (g.renderer_ == klakspout::Globals::Renderer::DX11)
            {
                PROFILE_SCOPE(markerTextureCopy);

                ID3D11Resource* source_resource = static_cast<ID3D11Resource*>(tex);

                g.d3d11Context_->CopyResource(d3d11_resource_, source_resource);
            }
            else if (g.renderer_ == klakspout::Globals::Renderer::DX12)
            {
                ID3D12Resource* dx12_resource = static_cast<ID3D12Resource*>(tex);
                ID3D11Resource* dx11_resource = getWrappedResource(dx12_resource, g.frame_count_);

                {
                    PROFILE_SCOPE(markerTextureWrappedActions);

                    // Taken from spoutDX12::SendDX11Resource
                    g.d3d11on12_->AcquireWrappedResources(&dx11_resource, 1);
                    {
                        PROFILE_SCOPE(markerTextureCopy);

                        g.d3d11Context_->CopyResource(d3d11_resource_, dx11_resource);
                    }
                    g.d3d11on12_->ReleaseWrappedResources(&dx11_resource, 1);
                }

                // DX12 needs a flush call
                {
                    PROFILE_SCOPE(markerFlush);

                    g.d3d11Context_->Flush();
                }
            }

            return 1;
        }

        int receiveTexture(void* tex)
        {
            auto& g = Globals::get();

            if (g.renderer_ == klakspout::Globals::Renderer::DX11)
            {
                PROFILE_SCOPE(markerTextureCopy);

                ID3D11Resource* destination_resource = static_cast<ID3D11Resource*>(tex);

                g.d3d11Context_->CopyResource(destination_resource, d3d11_resource_);
            }
            else if (g.renderer_ == klakspout::Globals::Renderer::DX12)
            {
                ID3D12Resource* dx12_resource = static_cast<ID3D12Resource*>(tex);
                ID3D11Resource* dx11_resource = getWrappedResource(dx12_resource, g.frame_count_);

                {
                    PROFILE_SCOPE(markerTextureWrappedActions);

                    // Taken from spoutDX12::ReceiveDX12Resource
                    g.d3d11on12_->AcquireWrappedResources(&dx11_resource, 1);
                    {
                        PROFILE_SCOPE(markerTextureCopy);

                        g.d3d11Context_->CopyResource(dx11_resource, d3d11_resource_);
                    }
                    g.d3d11on12_->ReleaseWrappedResources(&dx11_resource, 1);
                }

                // DX12 needs a flush call
                {
                    PROFILE_SCOPE(markerFlush);

                    g.d3d11Context_->Flush();
                }
            }

            return 1;
        }

        // Deactivate the object and release its internal resources.
        void deactivate()
        {
            releaseInternals();
        }

    private:

        // Release internal objects.
        void releaseInternals()
        {
            auto& g = Globals::get();

            // Senders should unregister their own name on destruction.
            if (type_ == Type::sender && d3d11_resource_)
                g.sender_names_->ReleaseSenderName(name_.c_str());

            // Release D3D11 objects.
            if (d3d11_resource_)
            {
                d3d11_resource_->Release();
                d3d11_resource_ = nullptr;
            }

            if (d3d11_resource_view_)
            {
                d3d11_resource_view_->Release();
                d3d11_resource_view_ = nullptr;
            }
        }

        // For DX12 only, not needed on DX11.
        // Each resource that we use needs a DX11on12 representation.
        //
        // Creation of a DX11on12 representation is an expensive operation.
        //
        // There's no SharedObject to DX12 resource mapping possible because
        // temporary RTs might be shared between multiple SharedObjects.
        //
        // For these reasons we use a cache.
        ID3D11Resource* getWrappedResource(ID3D12Resource* dx12_resource, int frame_count)
        {
            ID3D11Resource* dx11_resource = nullptr;

            auto& g = Globals::get();
            auto it = g.wrap_cache_.find(dx12_resource);

            // Taken from spoutDX12::WrapDX12Resource
            if (it == g.wrap_cache_.end())
            {
                PROFILE_SCOPE(markerTextureWrap);

                HRESULT hr = S_OK;

                // A D3D11_RESOURCE_FLAGS structure that enables an application to override flags
                // that would be inferred by the resource/heap properties.
                // The D3D11_RESOURCE_FLAGS structure contains bind flags, misc flags, and CPU access flags.
                D3D11_RESOURCE_FLAGS d3d11Flags = {};

                // Create a wrapped resource to access our d3d12 resource from the d3d11 device
                // Note: D3D12_RESOURCE_STATE variables are: 
                //    (1) the state of the d3d12 resource when we acquire it
                //        (when the d3d12 pipeline is finished with it and we are ready to use it in d3d11)
                //    (2) when we are done using it in d3d11 (we release it back to d3d12) 
                //        these are the states our resource will be transitioned into
                hr = g.d3d11on12_->CreateWrappedResource(
                    dx12_resource, // A pointer to an already-created D3D12 resource or heap.
                    &d3d11Flags, 
                    D3D12_RESOURCE_STATE_COMMON, // InState
                    D3D12_RESOURCE_STATE_COMMON, // OutState
                    IID_PPV_ARGS(&dx11_resource)
                ); // Lazy

                if (FAILED(hr)) {
                    DEBUG_LOG("spoutDX12::WrapDX12Resource - failed to create wrapped resource (%d 0x%.7X)", LOWORD(hr), UINT(hr));
                    return 0;
                }

                DX12WrapCacheEntry entry;
                entry.wrapped_resource = dx11_resource;
                entry.last_usage_frame = frame_count;
                g.wrap_cache_.insert(std::make_pair(dx12_resource, entry));

                DEBUG_LOG(
                    "Added resource (%p), cache size: %d",
                    dx12_resource,
                    static_cast<int>(g.wrap_cache_.size())
                );
            }
            else
            {
                dx11_resource = it->second.wrapped_resource;
                it->second.last_usage_frame = frame_count;
            }

            return dx11_resource;
        }

        // Set up as a sender.
        bool setupSender()
        {
            auto& g = Globals::get();

            // Avoid name duplication.
            {
                PROFILE_SCOPE(markerCheckSender);

                unsigned int width, height; HANDLE handle; DWORD format; // unused
                if (g.sender_names_->CheckSender(name_.c_str(), width, height, handle, format))
                    return false;
            }

            // Currently we only support Unity's RGBA32 TextureFormat
            // (which is the ARGB32 RenderTextureFormat).
            format_ = DXGI_FORMAT_R8G8B8A8_UNORM;

            ID3D11Texture2D* texture = nullptr;
            HANDLE handle;
            bool res_spout;

            // Create a shared texture.
            {
                PROFILE_SCOPE(markerCreateSharedTexture);

                res_spout = g.spout_->CreateSharedDX11Texture(g.d3d11_, width_, height_, format_, &texture, handle);
            }

            if (!res_spout)
            {
                DEBUG_LOG("CreateSharedDX11Texture failed (%s)", name_.c_str());
                return false;
            }

            d3d11_resource_ = texture;

            HRESULT res_d3d = S_OK;

            // Create a resource view for the shared texture.
            {
                PROFILE_SCOPE(markerCreateSRV);

                res_d3d = g.d3d11_->CreateShaderResourceView(d3d11_resource_, nullptr, &d3d11_resource_view_);
            }

            if (FAILED(res_d3d))
            {
                d3d11_resource_->Release();
                d3d11_resource_ = nullptr;
                DEBUG_LOG("CreateShaderResourceView failed (%s:%x)", name_.c_str(), res_d3d);
                return false;
            }

            // Create a Spout sender object for the shared texture.
            {
                PROFILE_SCOPE(markerCreateSender);

                res_spout = g.sender_names_->CreateSender(name_.c_str(), width_, height_, handle, format_);
            }

            if (!res_spout)
            {
                d3d11_resource_view_->Release();
                d3d11_resource_view_ = nullptr;
                d3d11_resource_->Release();
                d3d11_resource_ = nullptr;
                DEBUG_LOG("CreateSender failed (%s)", name_.c_str());
                return false;
            }

            DEBUG_LOG("Sender activated (%s)", name_.c_str());
            return true;
        }

        // Set up as a receiver.
        bool setupReceiver()
        {
            auto& g = Globals::get();

            // Retrieve the sender information with the given name.
            HANDLE handle;
            DWORD format;
            unsigned int w, h;

            bool res_spout = true;

            {
                PROFILE_SCOPE(markerCheckSender);

                res_spout = g.sender_names_->CheckSender(name_.c_str(), w, h, handle, format);
            }

            if (!res_spout)
            {
                // This happens really frequently. Avoid spamming the console.
                // DEBUG_LOG("CheckSender failed (%s)", name_.c_str());
                return false;
            }

            width_ = w;
            height_ = h;
            format_ = static_cast<DXGI_FORMAT>(format);
            HRESULT res_d3d = S_OK;

            // Start sharing the texture.
            {
                PROFILE_SCOPE(markerOpenSharedTexture);

                void** ptr = reinterpret_cast<void**>(&d3d11_resource_);
                res_d3d = g.d3d11_->OpenSharedResource(handle, __uuidof(ID3D11Resource), ptr);
            }

            if (FAILED(res_d3d))
            {
                DEBUG_LOG("OpenSharedResource failed (%s:%x)", name_.c_str(), res_d3d);
                return false;
            }

            // Create a resource view for the shared texture.
            {
                PROFILE_SCOPE(markerCreateSRV);

                res_d3d = g.d3d11_->CreateShaderResourceView(d3d11_resource_, nullptr, &d3d11_resource_view_);
            }

            if (FAILED(res_d3d))
            {
                d3d11_resource_->Release();
                d3d11_resource_ = nullptr;
                DEBUG_LOG("CreateShaderResourceView failed (%s:%x)", name_.c_str(), res_d3d);
                return false;
            }

            DEBUG_LOG("Receiver activated (%s)", name_.c_str());
            return true;
        }
    };
}
