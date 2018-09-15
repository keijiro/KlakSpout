#pragma once

#include <cstdio>
#include <cassert>
#include <d3d11.h>
#include "Spout/SpoutSDK.h"

// Debug logging macro
#if defined(_DEBUG)
#define DEBUG_LOG(fmt, ...) std::printf("KlakSpout: "#fmt"\n", __VA_ARGS__)
#else
#define DEBUG_LOG(fmt, ...)
#endif

namespace klakspout
{
    // Singleton class used for storing global variables
    class Globals final
    {
    public:

        ID3D11Device* d3d11_;
        std::unique_ptr<spoutDirectX> spout_;
        std::unique_ptr<spoutSenderNames> sender_names_;

        static Globals& get()
        {
            static Globals instance;
            return instance;
        }

        bool isReady() const
        {
            return d3d11_;
        }

        bool checkSenderExists(const char* name) const
        {
            unsigned int width, height; HANDLE handle; DWORD format; // unused
            return sender_names_->CheckSender(name, width, height, handle, format);
        }
    };
}