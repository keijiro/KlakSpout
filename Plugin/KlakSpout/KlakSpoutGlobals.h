#pragma once

#include <cstdio>
#include <cassert>
#include <map>
#include <memory>
#include <d3d11.h>
#include <d3d11on12.h>
#include <d3d12.h>
#include "../Spout/SpoutGL/SpoutDirectX.h"
#include "../Spout/SpoutGL/SpoutSenderNames.h"
#include "../Unity/IUnityGraphicsD3D11.h"
#include "../Unity/IUnityGraphicsD3D12.h"
#include "../Unity/IUnityProfiler.h"

// Debug logging macro
#if defined(KLAKSPOUT_DEBUG)
#define DEBUG_LOG(fmt, ...) std::printf("KlakSpout: "#fmt"\n", __VA_ARGS__)
#else
#define DEBUG_LOG(fmt, ...) do {} while (0)
#endif

// Profiling macros
#define PROFILE_START(marker) if (klakspout::unityProfilerIsAvailable) klakspout::unityProfiler->BeginSample(klakspout::marker)
#define PROFILE_END(marker) if (klakspout::unityProfilerIsAvailable) klakspout::unityProfiler->EndSample(klakspout::marker)
#define PROFILE_SCOPE(marker) klakspout::ScopedMarker(klakspout::marker, klakspout::unityProfiler, klakspout::unityProfilerIsAvailable)

namespace klakspout
{
    static IUnityProfiler* unityProfiler = nullptr;
    static bool unityProfilerIsAvailable = false;
    static const UnityProfilerMarkerDesc* markerEventDispose = nullptr;
    static const UnityProfilerMarkerDesc* markerEventTargetObject = nullptr;
    static const UnityProfilerMarkerDesc* markerEventUpdateWrapCache = nullptr;
    static const UnityProfilerMarkerDesc* markerSendTexture = nullptr;
    static const UnityProfilerMarkerDesc* markerReceiveTexture = nullptr;
    static const UnityProfilerMarkerDesc* markerGetSenderInfo = nullptr;
    static const UnityProfilerMarkerDesc* markerScanSharedObjects = nullptr;
    static const UnityProfilerMarkerDesc* markerTextureCopy = nullptr;
    static const UnityProfilerMarkerDesc* markerTextureWrap = nullptr;
    static const UnityProfilerMarkerDesc* markerTextureWrappedActions = nullptr;
    static const UnityProfilerMarkerDesc* markerFlush = nullptr;
    static const UnityProfilerMarkerDesc* markerCheckSender = nullptr;
    static const UnityProfilerMarkerDesc* markerCreateSharedTexture = nullptr;
    static const UnityProfilerMarkerDesc* markerCreateSRV = nullptr;
    static const UnityProfilerMarkerDesc* markerCreateSender = nullptr;
    static const UnityProfilerMarkerDesc* markerOpenSharedTexture = nullptr;
    static const UnityProfilerMarkerDesc* markerMutex = nullptr;

    class ScopedMarker
    {
    public:

        explicit ScopedMarker(const UnityProfilerMarkerDesc* marker, IUnityProfiler* profiler, bool enabled)
        : marker_(marker), profiler_(profiler), enabled_(enabled)
        {
            if (enabled_)
                profiler_->BeginSample(marker_);
        }

        ~ScopedMarker() noexcept
        {
            if (enabled_)
                profiler_->EndSample(marker_);
        }

        ScopedMarker(const ScopedMarker&) = delete;
        ScopedMarker& operator=(const ScopedMarker&) = delete;

    private:

        const UnityProfilerMarkerDesc* marker_;
        IUnityProfiler* profiler_;
        bool enabled_;
    };

    struct SenderInfo
    {
        int width_;
        int height_;
        int format_;
        bool exists_;
        bool is_same_size_;
    };

    struct DX12WrapCacheEntry
    {
        ID3D11Resource* wrapped_resource;
        int last_usage_frame;
    };

    // Singleton class used for storing global variables
    class Globals final
    {
    public:

        enum class PluginEvent { Dispose, SetTargetObject, UpdateWrapCache };
        enum class PluginBlitCommand { Send = 2, Receive = 3 };

        enum class Renderer { DX11, DX12 } renderer_;

        IUnityGraphicsD3D11* d3d11_interface_ = nullptr;
        ID3D11Device* d3d11_ = nullptr;
        ID3D11DeviceContext* d3d11Context_ = nullptr;

        // For DX12
        IUnityGraphicsD3D12v6* d3d12_interface_ = nullptr;
        ID3D11On12Device* d3d11on12_ = nullptr;
        ID3D12Device* d3d12_ = nullptr;
        std::map<ID3D12Resource*, DX12WrapCacheEntry> wrap_cache_;
        const int cache_eviction_limit_ = 10;
        int frame_count_ = 0;

        std::unique_ptr<spoutDirectX> spout_ = nullptr;
        std::unique_ptr<spoutSenderNames> sender_names_ = nullptr;

        static Globals& get()
        {
            static Globals instance;
            return instance;
        }

        bool isReady() const
        {
            return d3d11_ != nullptr;
        }
    };
}
