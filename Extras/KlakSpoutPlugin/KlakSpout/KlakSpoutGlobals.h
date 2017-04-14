#pragma once

#include <cstdio>
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
    //
    // A class for holding global variables
    //
    class Globals
    {
    public:

        ID3D11Device* d3d11_;
        spoutDirectX* spout_;
        spoutSenderNames* sender_names_;

        static Globals& get()
        {
            static Globals instance;
            return instance;
        }
    };
}