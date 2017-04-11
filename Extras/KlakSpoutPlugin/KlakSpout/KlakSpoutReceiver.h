#pragma once

#include "KlakSpoutGlobals.h"

namespace klakspout
{
    // Spout receiver object
    class Receiver
    {
    public:

        // Identifier for the receier list
        int id_;

        // Spout sender infromation
        string name_;
        unsigned int width_, height_;
        DXGI_FORMAT format_;

        // D3D resources and handlers
        ID3D11Resource * resource_;
        ID3D11ShaderResourceView* view_;
        HANDLE handle_;

        // Constructor (only for basic initialization)
        Receiver(int id, string name)
            : id_(id), name_(name),
            format_(DXGI_FORMAT_R8G8B8A8_UNORM),
            resource_(nullptr), view_(nullptr), handle_(nullptr)
        {
        }

        // Destructor
        ~Receiver()
        {
            Cleanup();
        }

        // Has the texture been already up?
        bool IsReady()
        {
            return resource_;
        }

        // Releases all the resources. Can be called multiple times.
        void Cleanup()
        {
            if (view_)
            {
                view_->Release();
                view_ = nullptr;
            }

            if (resource_)
            {
                resource_->Release();
                resource_ = nullptr;
            }
        }

        // Sets up the resources and start using them.
        void Setup()
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
                Cleanup();
                return;
            }

            DEBUG_LOG("Receiver created.");
        }
    };
}