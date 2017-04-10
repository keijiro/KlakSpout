#include "KlakSpoutGlobals.h"
#include "KlakSpoutSender.h"
#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"
#include "Unity/IUnityGraphicsD3D11.h"
#include <vector>

namespace
{
    IUnityInterfaces* unity_;

    // Sender objects
    std::vector<klakspout::Sender> senders_;
    int last_sender_id_;

    // Device event callback
    void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType event_type)
    {
        DEBUG_LOG("OnGraphicsDeviceEvent(%d)", event_type);

        auto & g = klakspout::Globals::get();

        if (event_type == kUnityGfxDeviceEventInitialize && !g.spout_)
        {
            // Initialize the Spout globals.
            g.spout_ = new spoutDirectX;
            g.sender_names_ = new spoutSenderNames;
        }

        if (event_type == kUnityGfxDeviceEventShutdown && g.spout_)
        {
            // Release the senders.
            for (auto & sender : senders_) sender.Cleanup();
            senders_.clear();

            // Finalize the Spout globals.
            delete g.spout_;
            g.spout_ = nullptr;

            delete g.sender_names_;
            g.sender_names_ = nullptr;
        }
    }

    // Render event callback
    void UNITY_INTERFACE_API OnRenderEvent(int event_id)
    {
        DEBUG_LOG("OnRenderEvent(%d)", event_id);

        if (event_id == 0)
        {
            // Set up all the senders that is not ready yet.
            for (auto & sender : senders_)
                if (!sender.IsReady()) sender.Setup();
        }
    }
}

//
// Low-level plugin interface functions
//

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces)
{
    // Open a new console on debug builds.
    #if defined(_DEBUG)
    FILE * pConsole;
    AllocConsole();
    freopen_s(&pConsole, "CONOUT$", "wb", stdout);
    #endif

    auto & g = klakspout::Globals::get();

    // Retrieve the interface pointers.
    unity_ = interfaces;
    g.d3d11_ = unity_->Get<IUnityGraphicsD3D11>()->GetDevice();

    // Register the custom callback.
    unity_->Get<IUnityGraphics>()->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

    // Invoke the initialization event.
    OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    // Unregister the custom callback.
    unity_->Get<IUnityGraphics>()->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}

extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc()
{
    return OnRenderEvent;
}

//
//  Native plugin functions
//

extern "C" UNITY_INTERFACE_EXPORT int CreateSender(const char* name, int width, int height)
{
    senders_.push_back(klakspout::Sender(last_sender_id_++, name, width, height));
    return senders_.back().id_;
}

extern "C" UNITY_INTERFACE_EXPORT void DestroySender(int id)
{
    // Search and destroy.
    for (auto it = senders_.begin(); it != senders_.end(); it++)
    {
        if (it->id_ == id)
        {
            it->Cleanup();
            senders_.erase(it);
            break;
        }
    }
}

extern "C" void UNITY_INTERFACE_EXPORT * GetSenderTexturePtr(int id)
{
    for (auto & sender : senders_)
        if (sender.id_ == id) return sender.view_;

    // No such a sender was found.
    return nullptr;
}