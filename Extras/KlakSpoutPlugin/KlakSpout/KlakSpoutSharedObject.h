#pragma once

#include "KlakSpoutGlobals.h"

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

        // D3D11 objects
        ID3D11Resource* d3d11_resource_;
        ID3D11ShaderResourceView* d3d11_resource_view_;

        // Constructor
        SharedObject(Type type, const string& name, int width = -1, int height = -1)
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
            return d3d11_resource_;
        }

        // Validate the internal resources.
        bool isValid() const
        {
            // Nothing to validate for senders.
            if (type_ == Type::sender) return true;

            // Non-active objects have nothing to validate.
            if (!isActive()) return true;

            auto& g = Globals::get();

            // This must be an active receiver, so check if the connection to
            // the sender is still valid.
            unsigned int width, height;
            HANDLE handle;
            DWORD format;
            auto found = g.sender_names_->CheckSender(name_.c_str(), width, height, handle, format);
            return found && width_ == width && height_ == height;
        }

        // Try activating the object. Returns false when failed.
        bool activate()
        {
            assert(d3d11_resource_ == nullptr && d3d11_resource_view_ == nullptr);
            return type_ == Type::sender ? setupSender() : setupReceiver();
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

        // Set up as a sender.
        bool setupSender()
        {
            auto& g = Globals::get();

            // Avoid name duplication.
            {
                unsigned int width, height; HANDLE handle; DWORD format; // unused
                if (g.sender_names_->CheckSender(name_.c_str(), width, height, handle, format)) return false;
            }

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
                d3d11_resource_ = nullptr;
                DEBUG_LOG("CreateShaderResourceView failed (%s:%x)", name_.c_str(), res_d3d);
                return false;
            }

            // Create a Spout sender object for the shared texture.
            res_spout = g.sender_names_->CreateSender(name_.c_str(), width_, height_, handle, format);

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
            auto res_spout = g.sender_names_->CheckSender(name_.c_str(), w, h, handle, format);

            if (!res_spout)
            {
                // This happens really frequently. Avoid spamming the console.
                // DEBUG_LOG("CheckSender failed (%s)", name_.c_str());
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
                d3d11_resource_ = nullptr;
                DEBUG_LOG("CreateShaderResourceView failed (%s:%x)", name_.c_str(), res_d3d);
                return false;
            }

            DEBUG_LOG("Receiver activated (%s)", name_.c_str());
            return true;
        }
    };
}