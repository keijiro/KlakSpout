#pragma once

#include "KlakSpoutGlobals.h"

namespace klakspout
{
    // Shared spout object handler class
    class SharedObject final
    {
    public:

        // Object type
        enum class Type { sender, receiver } type_;

        // Object state
        enum class State { initialized, active, released, destroyed } state_;

        // Object attributes
        std::string name_;
        int width_, height_;

        // D3D11 objects
        ID3D11Resource* d3d11_resource_;
        ID3D11ShaderResourceView* d3d11_resource_view_;

        // Constructor
        SharedObject(Type type, const string& name, int width = -1, int height = -1)
            : type_(type), state_(State::initialized), name_(name), width_(width), height_(height)
        {
            if (type_ == Type::sender)
                DEBUG_LOG("Sender created (%s)", name_.c_str());
            else
                DEBUG_LOG("Receiver created (%s)", name_.c_str());
        }

        // Destructor
        ~SharedObject()
        {
            assert(state_ == State::destroyed);

            if (type_ == Type::sender)
                DEBUG_LOG("Sender disposed (%s)", name_.c_str());
            else
                DEBUG_LOG("Receiver disposed (%s)", name_.c_str());
        }

        // Prohibit use of default constructor and copy operators
        SharedObject() = delete;
        SharedObject(SharedObject&) = delete;
        SharedObject& operator = (const SharedObject&) = delete;

        // Check if a receiver is disconnected from a previously connected sender.
        // Returns true if disconnected.
        bool detectDisconnection() const
        {
            auto& g = Globals::get();

            // Do nothing with senders and non-active receivers.
            if (type_ == Type::sender || state_ != State::active) return false;

            // Retrieve the sender information.
            unsigned int width, height;
            HANDLE handle;
            DWORD format;
            auto res = g.sender_names_->CheckSender(name_.c_str(), width, height, handle, format);

            // Check if the dimensions/format have been changed.
            return !res || width != width_ || height != height_;
        }

        // Update the internal state. This only can be called in the render thread.
        void updateFromRenderThread()
        {
            if (state_ == State::initialized)
            {
                if (type_ == Type::sender)
                {
                    if (setupSender()) state_ = State::active;
                }
                else
                {
                    if (setupReceiver()) state_ = State::active;
                }
            }
            else if (state_ == State::released)
            {
                releaseResources();
                state_ = State::destroyed;
            }
        }

    private:

        // Set up as a sender.
        bool setupSender()
        {
            auto& g = Globals::get();

            // Currently we only support RGBA32.
            const auto format = DXGI_FORMAT_R8G8B8A8_UNORM;

            // Create a shared texture.
            ID3D11Texture2D* texture;
            HANDLE handle;
            auto res_spout = g.spout_->CreateSharedDX11Texture(g.d3d11_, width_, height_, format, &texture, handle);

            if (!res_spout)
            {
                DEBUG_LOG("CreateSharedDX11Texture failed (%s)", name_.c_str());
                return false;
            }

            d3d11_resource_ = texture;

            // Create a resource view for the shared texture.
            auto res_d3d = g.d3d11_->CreateShaderResourceView(d3d11_resource_, nullptr, &d3d11_resource_view_);

            if (FAILED(res_d3d))
            {
                d3d11_resource_->Release();
                DEBUG_LOG("CreateShaderResourceView failed (%s:%x)", name_.c_str(), res_d3d);
                return false;
            }

            // Create a Spout sender object for the shared texture.
            res_spout = g.sender_names_->CreateSender(name_.c_str(), width_, height_, handle, format);

            if (!res_spout)
            {
                d3d11_resource_view_->Release();
                d3d11_resource_->Release();
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
            auto res_spout = g.sender_names_->CheckSender(name_.c_str(), w, h, handle, format);

            if (!res_spout)
            {
                DEBUG_LOG("CheckSender failed (%s)", name_.c_str());
                return false;
            }

            width_ = w;
            height_ = h;

            // Start sharing the texture.
            void** ptr = reinterpret_cast<void**>(&d3d11_resource_);
            auto res_d3d = g.d3d11_->OpenSharedResource(handle, __uuidof(ID3D11Resource), ptr);

            if (FAILED(res_d3d))
            {
                DEBUG_LOG("OpenSharedResource failed (%s:%x)", name_.c_str(), res_d3d);
                return false;
            }

            // Create a resource view for the shared texture.
            res_d3d = g.d3d11_->CreateShaderResourceView(d3d11_resource_, nullptr, &d3d11_resource_view_);

            if (FAILED(res_d3d))
            {
                d3d11_resource_->Release();
                DEBUG_LOG("CreateShaderResourceView failed (%s:%x)", name_.c_str(), res_d3d);
                return false;
            }

            DEBUG_LOG("Receiver activated (%s)", name_.c_str());
            return true;
        }

        void releaseResources()
        {
            auto& g = Globals::get();

            // Senders should unregister their own name on destruction.
            if (type_ == Type::sender) g.sender_names_->ReleaseSenderName(name_.c_str());

            // Release D3D11 resources.
            d3d11_resource_->Release();
            d3d11_resource_view_->Release();

            state_ = State::destroyed;

            if (type_ == Type::sender)
                DEBUG_LOG("Sender deactivated (%s)", name_.c_str());
            else
                DEBUG_LOG("Receiver deactivated (%s)", name_.c_str());
        }
    };
}