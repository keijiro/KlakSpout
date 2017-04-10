#pragma once

#include "KlakSpoutGlobals.h"

namespace klakspout
{
    class Sender
    {
    public:

        int id_;
        string name_;
        int width_, height_;
        DXGI_FORMAT format_;

        ID3D11Texture2D * texture_;
        ID3D11ShaderResourceView* view_;
        HANDLE handle_;

        Sender(int id, string name, int width, int height)
            : id_(id), name_(name),
              width_(width), height_(height), format_(DXGI_FORMAT_R8G8B8A8_UNORM),
              texture_(nullptr), view_(nullptr), handle_(nullptr)
        {
        }

        ~Sender()
        {
            Cleanup();
        }

        bool IsReady()
        {
            return texture_;
        }

        void Cleanup()
        {
            if (view_)
            {
                view_->Release();
                view_ = nullptr;
            }

            if (texture_)
            {
                Globals::get().sender_names_->ReleaseSenderName(name_.c_str());
                texture_ = nullptr;
            }
        }

        void Setup()
        {
            if (texture_) return;

            auto & g = Globals::get();

            auto res_spout = g.spout_->CreateSharedDX11Texture(g.d3d11_, width_, height_, format_, &texture_, handle_);

            if (!res_spout)
            {
                DEBUG_LOG("CreateSharedDX11Texture failed.");
                return;
            }

            D3D11_SHADER_RESOURCE_VIEW_DESC viewDesc;
            viewDesc.Format = format_;
            viewDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
            viewDesc.Texture2D.MostDetailedMip = 0;
            viewDesc.Texture2D.MipLevels = 1;

            auto res_d3d = g.d3d11_->CreateShaderResourceView(texture_, &viewDesc, &view_);

            if (FAILED(res_d3d))
            {
                DEBUG_LOG("CreateShaderResourceView failed (%x).", res_d3d);
                Cleanup();
                return;
            }

            res_spout = g.sender_names_->CreateSender(name_.c_str(), width_, height_, handle_, format_);

            if (!res_spout)
            {
                DEBUG_LOG("CreateSender failed.");
                Cleanup();
                return;
            }

            DEBUG_LOG("Sender created");
        }
    };
}