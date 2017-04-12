#pragma once

#include "KlakSpoutGlobals.h"

namespace klakspout
{
    //
    // Abstract base for Spout shared resource classes
    //
    class SharedResource
    {
    public:

        // Identifier for the shared resource list
        int id_;

        // Resource attributes
        string name_;
        unsigned int width_, height_;
        DXGI_FORMAT format_;

        // D3D objects
        ID3D11Resource * resource_;
        ID3D11ShaderResourceView* view_;
        HANDLE handle_;

        // Constructor
        SharedResource(int id, string name, unsigned int width, unsigned int height)
            : id_(id), name_(name),
            width_(width), height_(height),
            format_(DXGI_FORMAT_R8G8B8A8_UNORM),
            resource_(nullptr), view_(nullptr), handle_(nullptr)
        {
        }

        // Destructor
        virtual ~SharedResource()
        {
            if (view_) view_->Release();
            if (resource_) resource_->Release();
        }

        // Has it been already up?
        bool IsReady() const
        {
            return resource_;
        }

        // Set up and start using.
        virtual void Setup() = 0;
    };

    //
    // Spout sender class
    //
    class Sender : public SharedResource
    {
    public:

        // Constructor
        Sender(int id, string name, unsigned int width, unsigned int height)
            : SharedResource(id, name, width, height)
        {
        }

        // Destructor
        ~Sender() override
        {
            // Unregister the sender name.
            if (resource_)
                Globals::get().sender_names_->ReleaseSenderName(name_.c_str());
        }

        // Set up and start using.
        void Setup() override
        {
            // Do nothing if ready.
            if (IsReady()) return;

            auto & g = Globals::get();

            // Create a shared texture.
            ID3D11Texture2D* texture;
            auto res_spout = g.spout_->CreateSharedDX11Texture(g.d3d11_, width_, height_, format_, &texture, handle_);

            if (!res_spout)
            {
                DEBUG_LOG("CreateSharedDX11Texture failed.");
                return;
            }

            resource_ = texture;

            // Create a resource view for the shared texture.
            auto res_d3d = g.d3d11_->CreateShaderResourceView(resource_, nullptr, &view_);

            if (FAILED(res_d3d))
            {
                DEBUG_LOG("CreateShaderResourceView failed (%x).", res_d3d);
                return;
            }

            // Create a Spout sender object for the shared texture.
            res_spout = g.sender_names_->CreateSender(name_.c_str(), width_, height_, handle_, format_);

            if (!res_spout)
            {
                DEBUG_LOG("CreateSender failed.");
                return;
            }

            DEBUG_LOG("Sender created.");
        }
    };

    //
    // Spout receiver class
    //
    class Receiver : public SharedResource
    {
    public:

        // Constructor
        Receiver(int id, string name)
            : SharedResource(id, name, 0, 0)
        {
        }

        // Set up and start using.
        void Setup() override
        {
            // Do nothing if ready.
            if (IsReady()) return;

            auto & g = Globals::get();

            // Retrieve the sender information with the name.
            DWORD format;
            auto res_spout = g.sender_names_->CheckSender(name_.c_str(), width_, height_, handle_, format);

            if (!res_spout)
            {
                DEBUG_LOG("CheckSender failed.");
                return;
            }

            // Start using the shared texture.
            auto res_d3d = g.d3d11_->OpenSharedResource(handle_, __uuidof(ID3D11Resource), reinterpret_cast<void**>(&resource_));

            if (FAILED(res_d3d))
            {
                DEBUG_LOG("OpenSharedResource failed (%x).", res_d3d);
                return;
            }

            // Create a resource view for the shared texture.
            res_d3d = g.d3d11_->CreateShaderResourceView(resource_, nullptr, &view_);

            if (FAILED(res_d3d))
            {
                DEBUG_LOG("CreateShaderResourceView failed (%x).", res_d3d);
                return;
            }

            DEBUG_LOG("Receiver created.");
        }
    };
}