#include "KlakSpoutSharedObject.h"
#include "Unity/IUnityGraphics.h"
#include "Unity/IUnityGraphicsD3D11.h"
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
    // - OnRenderEvent (this is the only point called from the render thread)
    // - Plugin function that calls SharedObject or Spout API functions.
    std::mutex lock_;

    // Unity device event callback
    void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType event_type)
    {
        assert(unity_);

        // Do nothing if it's not the D3D11 renderer.
        if (unity_->Get<IUnityGraphics>()->GetRenderer() != kUnityGfxRendererD3D11) return;

        DEBUG_LOG("OnGraphicsDeviceEvent (%d)", event_type);

        auto& g = klakspout::Globals::get();

        if (event_type == kUnityGfxDeviceEventInitialize)
        {
            // Retrieve the D3D11 interface.
            g.d3d11_ = unity_->Get<IUnityGraphicsD3D11>()->GetDevice();

            // Initialize the Spout global objects.
            g.spout_ = std::make_unique<spoutDirectX>();
            g.sender_names_ = std::make_unique<spoutSenderNames>();

            // Apply the max sender registry value.
            DWORD max_senders;
            if (g.spout_->ReadDwordFromRegistry(&max_senders, "Software\\Leading Edge\\Spout", "MaxSenders"))
                g.sender_names_->SetMaxSenders(max_senders);
        }
        else if (event_type == kUnityGfxDeviceEventShutdown)
        {
            // Invalidate the D3D11 interface.
            g.d3d11_ = nullptr;

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
        if (!klakspout::Globals::get().isReady()) return;

        std::lock_guard<std::mutex> guard(lock_);

        auto* pobj = reinterpret_cast<klakspout::SharedObject*>(data);

        if (event_id == 0) // Update event
        {
            if (!pobj->isActive()) pobj->activate();
        }
        else if (event_id == 1) // Dispose event
        {
            delete pobj;
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
    #if defined(_DEBUG)
    FILE * pConsole;
    AllocConsole();
    freopen_s(&pConsole, "CONOUT$", "wb", stdout);
    #endif

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

extern "C" void UNITY_INTERFACE_EXPORT * GetTexturePointer(void* ptr)
{
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->d3d11_resource_view_;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureWidth(void* ptr)
{
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->width_;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureHeight(void* ptr)
{
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->height_;
}

extern "C" int UNITY_INTERFACE_EXPORT CheckValid(void* ptr)
{
    std::lock_guard<std::mutex> guard(lock_);
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->isValid();
}

extern "C" int UNITY_INTERFACE_EXPORT ScanSharedObjects()
{
    auto& g = klakspout::Globals::get();
    if (!g.isReady()) return 0;
    std::lock_guard<std::mutex> guard(lock_);
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
