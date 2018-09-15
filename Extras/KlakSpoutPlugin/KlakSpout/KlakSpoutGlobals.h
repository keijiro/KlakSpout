#pragma once

#include <cstdio>
#include <cassert>
#include <d3d11.h>
#include "Spout/SpoutDirectX.h"
#include "Spout/SpoutSenderNames.h"

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
    };
}