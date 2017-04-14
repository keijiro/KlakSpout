#pragma once

#include "KlakSpoutGlobals.h"

namespace klakspout
{
    //
    // Spout shared object handler class
    //
    // This class is directly accessed from the C# land.
    // Keep careful about the memory layout.
    //
    class SharedObject final
    {
    public:

        // Is it a sender or a receiver?
        enum Type { kSender, kReceiver } type_;

        // Resource attributes
        char name_[SpoutMaxSenderNameLen];
        int width_, height_;
        DXGI_FORMAT format_;

        // D3D11 objects
        ID3D11Resource* d3d11_resource_;
        ID3D11ShaderResourceView* d3d11_resource_view_;

        // Constructor
        SharedObject(Type type, const string& name, int width = -1, int height = -1)
            : type_(type), width_(width), height_(height), format_(DXGI_FORMAT_UNKNOWN),
            d3d11_resource_(nullptr), d3d11_resource_view_(nullptr)
        {
            auto len = name._Copy_s(name_, SpoutMaxSenderNameLen - 1, name.length());
            name_[len] = 0;
        }

        // Destructor
        ~SharedObject()
        {
            auto & g = Globals::get();

            // A sender should unregister its name on destruction.
            if (type_ == kSender)
                g.sender_names_->ReleaseSenderName(name_);

            // Release the D3D11 resources.
            if (d3d11_resource_) d3d11_resource_->Release();
            if (d3d11_resource_view_) d3d11_resource_view_->Release();

            DEBUG_LOG("Resource released (%s).", name_);
        }

        // Detect disconnection from the sender. Returns true on if disconnected.
        bool detectDisconnection() const
        {
            auto & g = Globals::get();

            if (type_ == kSender) return false;

            // Retrieve the sender information.
            unsigned int width, height;
            HANDLE handle;
            DWORD format;
            auto res = g.sender_names_->CheckSender(name_, width, height, handle, format);

            // Check if something has been changed.
            return !res || width != width_ || height != height_ || format_ != format;
        }

        // Update the D3D11 resources. This should be called from the render thread.
        void updateResources()
        {
            // Call the setup function if it hasn't been set up.
            if (!d3d11_resource_view_)
            {
                if (type_ == kSender)
                    setupSender();
                else
                    setupReceiver();
            }
        }

        // Set up as a sender.
        void setupSender()
        {
            auto & g = Globals::get();

            // The texture format is fixed to RGBA32.
            // TODO: we should support other formats.
            format_ = DXGI_FORMAT_R8G8B8A8_UNORM;

            // Create a shared texture.
            ID3D11Texture2D* texture;
            HANDLE handle;
            auto res_spout = g.spout_->CreateSharedDX11Texture(g.d3d11_, width_, height_, format_, &texture, handle);

            if (!res_spout)
            {
                DEBUG_LOG("CreateSharedDX11Texture failed.");
                return;
            }

            d3d11_resource_ = texture;

            // Create a resource view for the shared texture.
            auto res_d3d = g.d3d11_->CreateShaderResourceView(d3d11_resource_, nullptr, &d3d11_resource_view_);

            if (FAILED(res_d3d))
            {
                DEBUG_LOG("CreateShaderResourceView failed (%x).", res_d3d);
                return;
            }

            // Create a Spout sender object for the shared texture.
            res_spout = g.sender_names_->CreateSender(name_, width_, height_, handle, format_);

            if (!res_spout)
            {
                DEBUG_LOG("CreateSender failed.");
                return;
            }

            DEBUG_LOG("Sender created (%s).", name_);
        }

        // Set up as a receiver.
        void setupReceiver()
        {
            auto & g = Globals::get();

            // Retrieve the sender information with the name.
            HANDLE handle;
            DWORD format;
            unsigned int w, h;
            auto res_spout = g.sender_names_->CheckSender(name_, w, h, handle, format);

            if (!res_spout)
            {
                DEBUG_LOG("CheckSender failed.");
                return;
            }

            width_ = w;
            height_ = h;
            format_ = static_cast<DXGI_FORMAT>(format);

            // Start using the shared texture.
            void** ptr = reinterpret_cast<void**>(&d3d11_resource_);
            auto res_d3d = g.d3d11_->OpenSharedResource(handle, __uuidof(ID3D11Resource), ptr);

            if (FAILED(res_d3d))
            {
                DEBUG_LOG("OpenSharedResource failed (%x).", res_d3d);
                return;
            }

            // Create a resource view for the shared texture.
            res_d3d = g.d3d11_->CreateShaderResourceView(d3d11_resource_, nullptr, &d3d11_resource_view_);

            if (FAILED(res_d3d))
            {
                DEBUG_LOG("CreateShaderResourceView failed (%x).", res_d3d);
                return;
            }

            DEBUG_LOG("Receiver created (%s).", name_);
        }
    };
}