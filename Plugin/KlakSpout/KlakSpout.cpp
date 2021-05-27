#include "KlakSpoutSharedObject.h"
#include "../Unity/IUnityGraphics.h"
#include "../Unity/IUnityGraphicsD3D11.h"
#include "../Unity/IUnityGraphicsD3D12.h"
#include "../Unity/IUnityRenderingExtensions.h"
#include <d3d11.h>
#include <d3d11on12.h>
#include <mutex>

namespace
{
    // Low-level native plugin interface
    IUnityInterfaces* unity_;

    // Temporary storage for shared Spout object list
    std::set<std::string> shared_object_names_;

    // Local mutex object used to prevent race conditions between the main
    // thread and the render thread. This should be locked at the following
    // points:
    // - OnRenderEvent
    // - OnCustomBlit
    // - In general, plugin functions that call SharedObject or Spout API functions.
    //
    // Some Spout API functions are already thread-safe internally.
    std::mutex lock_;

    #define MUTEX_LOCK PROFILE_START(markerMutex); std::lock_guard<std::mutex> guard(lock_); PROFILE_END(markerMutex);

    // The default event config asks Unity to do a lot of syncs and flushes.
    // We need to configure the event to do the minimum work required for our use case.
    UnityD3D12PluginEventConfig custom_blit_event_config_;

    // Set by the user before calling OnCustomBlit, which will apply to the set object
    klakspout::SharedObject* target_shared_object_ = nullptr;

    // Unity device event callback
    void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType event_type)
    {
        assert(unity_);

        // Do nothing if it's not the D3D11 or D3D12 renderer.
        UnityGfxRenderer renderer = unity_->Get<IUnityGraphics>()->GetRenderer();
        if (renderer != kUnityGfxRendererD3D11 && renderer != kUnityGfxRendererD3D12) return;

        DEBUG_LOG("OnGraphicsDeviceEvent (%d)", event_type);

        auto& g = klakspout::Globals::get();

        if (event_type == kUnityGfxDeviceEventInitialize)
        {
            // Initialize the Spout global objects.
            g.spout_ = std::make_unique<spoutDirectX>();
            g.sender_names_ = std::make_unique<spoutSenderNames>();

            // Enable Spout's debug log in addition to our own
#ifdef KLAKSPOUT_DEBUG
            OpenSpoutConsole(); // Console only for debugging
            EnableSpoutLog(); // Log to console
#endif

            if (renderer == kUnityGfxRendererD3D11)
            {
                g.renderer_ = klakspout::Globals::Renderer::DX11;

                g.d3d11_interface_ = unity_->Get<IUnityGraphicsD3D11>();
                if (!g.d3d11_interface_)
                    DEBUG_LOG("Couldn't get d3d11 interface");

                g.d3d11_ = g.d3d11_interface_->GetDevice();
                if (!g.d3d11_)
                    DEBUG_LOG("Couldn't get d3d11 device");

                g.d3d11_->GetImmediateContext(&g.d3d11Context_);
                if (!g.d3d11Context_)
                    DEBUG_LOG("Couldn't get d3d11 context");
            }
            else if (renderer == kUnityGfxRendererD3D12)
            {
                g.renderer_ = klakspout::Globals::Renderer::DX12;

                g.d3d12_interface_ = unity_->Get<IUnityGraphicsD3D12v6>();
                if (!g.d3d12_interface_)
                    DEBUG_LOG("Couldn't get d3d12 interface");

                g.d3d12_ = g.d3d12_interface_->GetDevice();
                if (!g.d3d12_)
                    DEBUG_LOG("Couldn't get d3d12 device");

                UINT flags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
#ifdef KLAKSPOUT_DEBUG
                // Enable debug layer, can slow down performance
                flags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

                // Create 11on12 device
                IUnknown* queue = static_cast<IUnknown*>(g.d3d12_interface_->GetCommandQueue());
                HRESULT hr = D3D11On12CreateDevice(
                    g.d3d12_,
                    flags,
                    nullptr,
                    0,
                    &queue,
                    1,
                    0,
                    &g.d3d11_,
                    &g.d3d11Context_,
                    nullptr
                );

                if (FAILED(hr))
                    DEBUG_LOG("Failed to create 11on12 device");

                // Grab interface to the d3d11on12 device from the newly created d3d11 device
                hr = g.d3d11_->QueryInterface(__uuidof(ID3D11On12Device), (void**)&g.d3d11on12_);
                if (FAILED(hr))
                    DEBUG_LOG("Failed to query 11on12 device");

                custom_blit_event_config_.flags = 0;
                custom_blit_event_config_.ensureActiveRenderTextureIsBound = false;
                custom_blit_event_config_.graphicsQueueAccess = kUnityD3D12GraphicsQueueAccess_Allow;
                g.d3d12_interface_->ConfigureEvent(kUnityRenderingExtEventCustomBlit, &custom_blit_event_config_);
            }
        }
        else if (event_type == kUnityGfxDeviceEventShutdown)
        {
            // Invalidate state
            g.d3d11_ = nullptr;
            g.d3d11Context_ = nullptr;
            g.d3d11on12_ = nullptr;
            g.d3d12_ = nullptr;

            // Finalize the Spout globals.
            g.spout_.reset();
            g.sender_names_.reset();
        }
    }

    // Unity render event callbacks
    void UNITY_INTERFACE_API OnRenderEvent(int event_id, void* data)
    {
        // Do nothing if the D3D11 interface is not available. This only
        // happens on Editor. It may leak some resoruces but we can't do
        // anything about them.
        auto& g = klakspout::Globals::get();
        if (!g.isReady()) return;

        MUTEX_LOCK;
        auto plugin_event = static_cast<klakspout::Globals::PluginEvent>(event_id);

        if (plugin_event == klakspout::Globals::PluginEvent::Dispose)
        {
            PROFILE_SCOPE(markerEventDispose);

            auto* pobj = reinterpret_cast<klakspout::SharedObject*>(data);
            delete pobj;
        }
        else if (plugin_event == klakspout::Globals::PluginEvent::SetTargetObject)
        {
            PROFILE_SCOPE(markerEventTargetObject);

            auto* pobj = reinterpret_cast<klakspout::SharedObject*>(data);
            target_shared_object_ = pobj;

            if (!pobj->isActive())
                pobj->activate();
        }
        else if (plugin_event == klakspout::Globals::PluginEvent::UpdateWrapCache)
        {
            // The wrap cache is only necessary for DX12
            if (g.renderer_ != klakspout::Globals::Renderer::DX12)
                return;

            PROFILE_SCOPE(markerEventUpdateWrapCache);

            // Evicts resources that we haven't seen in a while.
            //
            // This is necessary because of some unfortunate circumstances:
            // * We need to maintain a DX11 mirror of every DX12 resource we use (for D3D11on12)
            // * Creating a mirror is expensive
            // * Want to support temporary RTs whose lifetimes we don't know about

            for (auto it = g.wrap_cache_.begin(); it != g.wrap_cache_.end(); )
            {
                int frame_age = g.frame_count_ - it->second.last_usage_frame;

                if (frame_age >= g.cache_eviction_limit_)
                {
                    DEBUG_LOG(
                        "Evicted resource (%p) at frame age (%d), cache size: %d",
                        it->first,
                        frame_age,
                        static_cast<int>(g.wrap_cache_.size())
                    );

                    it->second.wrapped_resource->Release();
                    it->second.wrapped_resource = nullptr;
                    it = g.wrap_cache_.erase(it);
                }
                else
                    ++it;
            }

            g.frame_count_++;
        }
    }

    void UNITY_INTERFACE_API OnCustomBlit(int event_id, void* data)
    {
        if (event_id == kUnityRenderingExtEventCustomBlit)
        {
            if (!target_shared_object_->isActive())
                return;

            auto* params = reinterpret_cast<UnityRenderingExtCustomBlitParams*>(data);
            auto command = static_cast<klakspout::Globals::PluginBlitCommand>(params->command);

            if (command == klakspout::Globals::PluginBlitCommand::Send)
            {
                PROFILE_SCOPE(markerSendTexture);

                auto& g = klakspout::Globals::get();
                if (!g.isReady()) return;
                MUTEX_LOCK;

                if (g.d3d11_interface_ != nullptr)
                {
                    void* tex = g.d3d11_interface_->TextureFromNativeTexture(params->source);
                    target_shared_object_->sendTexture(tex);
                }
                else if (g.d3d12_interface_ != nullptr)
                {
                    void* tex = g.d3d12_interface_->TextureFromNativeTexture(params->source);
                    target_shared_object_->sendTexture(tex);
                }
            }
            else if (command == klakspout::Globals::PluginBlitCommand::Receive)
            {
                PROFILE_SCOPE(markerReceiveTexture);

                auto& g = klakspout::Globals::get();
                if (!g.isReady()) return;
                MUTEX_LOCK;

                if (g.d3d11_interface_ != nullptr)
                {
                    void* tex = g.d3d11_interface_->TextureFromNativeTexture(params->source);
                    target_shared_object_->receiveTexture(tex);
                }
                else if (g.d3d12_interface_ != nullptr)
                {
                    void* tex = g.d3d12_interface_->TextureFromNativeTexture(params->source);
                    target_shared_object_->receiveTexture(tex);
                }
            }
        }
    }
}

//
// Low-level native plugin implementation
//

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces)
{
    unity_ = interfaces;

    // Replace stdout with a new console for debugging.
#if defined(KLAKSPOUT_DEBUG)
    FILE * pConsole;
    AllocConsole();
    freopen_s(&pConsole, "CONOUT$", "wb", stdout);
#endif

    klakspout::unityProfiler = unity_->Get<IUnityProfiler>();
    if (klakspout::unityProfiler != nullptr)
    {
        klakspout::unityProfilerIsAvailable = klakspout::unityProfiler->IsAvailable() != 0;
        klakspout::unityProfiler->CreateMarker(&klakspout::markerEventDispose,          "KlakSpout/Dispose", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerEventTargetObject,     "KlakSpout/TargetObject", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerEventUpdateWrapCache,  "KlakSpout/UpdateWrapCache", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerSendTexture,           "KlakSpout/SendTexture", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerReceiveTexture,        "KlakSpout/ReceiveTexture", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerGetSenderInfo,         "KlakSpout/GetSenderInfo", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerScanSharedObjects,     "KlakSpout/ScanSharedObjects", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerTextureCopy,           "KlakSpout/SharedObject/TextureCopy", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerTextureWrap,           "KlakSpout/SharedObject/TextureWrap", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerTextureWrappedActions, "KlakSpout/SharedObject/TextureWrappedActions", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerFlush,                 "KlakSpout/SharedObject/Flush", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerCheckSender,           "KlakSpout/SharedObject/CheckSender", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerCreateSharedTexture,   "KlakSpout/SharedObject/CreateSharedTexture", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerCreateSRV,             "KlakSpout/SharedObject/CreateSRV", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerCreateSender,          "KlakSpout/SharedObject/CreateSender", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerOpenSharedTexture,     "KlakSpout/SharedObject/OpenSharedTexture", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        klakspout::unityProfiler->CreateMarker(&klakspout::markerMutex,                 "KlakSpout/Mutex", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
    }

    // Register the custom callback, then manually invoke the initialization event once.
    unity_->Get<IUnityGraphics>()->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
    OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    // Unregister the custom callback.
    unity_->Get<IUnityGraphics>()->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);

    unity_ = nullptr;
}

extern "C" UnityRenderingEventAndData UNITY_INTERFACE_EXPORT GetRenderEventFunc()
{
    return OnRenderEvent;
}

extern "C" UnityRenderingEventAndData UNITY_INTERFACE_EXPORT GetCustomBlitFunc()
{
    return OnCustomBlit;
}

//
// Native plugin implementation
//

extern "C" void UNITY_INTERFACE_EXPORT * CreateSender(const char* name, int width, int height)
{
    if (!klakspout::Globals::get().isReady()) return nullptr;
    return new klakspout::SharedObject(klakspout::SharedObject::Type::sender, name != nullptr ? name : "", width, height);
}

extern "C" void UNITY_INTERFACE_EXPORT * CreateReceiver(const char* name)
{
    if (!klakspout::Globals::get().isReady()) return nullptr;
    return new klakspout::SharedObject(klakspout::SharedObject::Type::receiver, name != nullptr ? name : "");
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureWidth(void* ptr)
{
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->width_;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureHeight(void* ptr)
{
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->height_;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureFormat(void* ptr)
{
    return static_cast<int>(reinterpret_cast<const klakspout::SharedObject*>(ptr)->format_);
}

extern "C" bool UNITY_INTERFACE_EXPORT IsReady(void* ptr)
{
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->isActive();
}

extern "C" void UNITY_INTERFACE_EXPORT GetSenderInfo(const char* name, klakspout::SenderInfo* out_info)
{
    PROFILE_SCOPE(markerGetSenderInfo);

    auto& g = klakspout::Globals::get();

    unsigned int width, height;
    HANDLE handle;
    DWORD format;
    auto found = g.sender_names_->CheckSender(name, width, height, handle, format);

    out_info->width_ = static_cast<int>(width);
    out_info->height_ = static_cast<int>(height);
    out_info->format_ = static_cast<int>(format);
    out_info->exists_ = found;
}

extern "C" int UNITY_INTERFACE_EXPORT ScanSharedObjects()
{
    PROFILE_SCOPE(markerScanSharedObjects);

    auto& g = klakspout::Globals::get();
    if (!g.isReady()) return 0;

    shared_object_names_.clear();
    g.sender_names_->GetSenderNames(&shared_object_names_);

    return static_cast<int>(shared_object_names_.size());
}

extern "C" const void UNITY_INTERFACE_EXPORT * GetSharedObjectName(int index)
{
    auto count = 0;
    for (auto& name : shared_object_names_)
    {
        if (count++ == index)
        {
            // Return the name via a static string object.
            static std::string temp;
            temp = name;
            return temp.c_str();
        }
    }
    return nullptr;
}
