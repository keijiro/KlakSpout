#include "KlakSpoutGlobals.h"
#include "KlakSpoutSender.h"
#include "KlakSpoutReceiver.h"
#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"
#include "Unity/IUnityGraphicsD3D11.h"
#include <vector>

namespace
{
    IUnityInterfaces* unity_;

    // Sender/Receiver list
    std::vector<klakspout::Sender> senders_;
    std::vector<klakspout::Receiver> receivers_;
    int last_id_;

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
            // Release all the senders/receivers (Cleanup is called in the destructor).
            senders_.clear();
            receivers_.clear();

            // Finalize the Spout globals.
            delete g.spout_;
            delete g.sender_names_;
            g.spout_ = nullptr;
            g.sender_names_ = nullptr;
        }
    }

    // Render event callback
    void UNITY_INTERFACE_API OnRenderEvent(int event_id)
    {
        DEBUG_LOG("OnRenderEvent(%d)", event_id);

        if (event_id == 0)
        {
            // Set up all the senders/receivers that is not ready yet.
            for (auto & sender : senders_) if (!sender.IsReady()) sender.Setup();
            for (auto & receiver : receivers_) if (!receiver.IsReady()) receiver.Setup();
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
    senders_.push_back(klakspout::Sender(last_id_++, name, width, height));
    return senders_.back().id_;
}

extern "C" UNITY_INTERFACE_EXPORT int CreateReceiver(const char* name)
{
    receivers_.push_back(klakspout::Receiver(last_id_++, name));
    return receivers_.back().id_;
}

extern "C" UNITY_INTERFACE_EXPORT void Destroy(int id)
{
    // Simply scan all the senders/receivers and release found one.
    for (auto it = senders_.begin(); it != senders_.end(); it++)
    {
        if (it->id_ == id)
        {
            it->Cleanup();
            senders_.erase(it);
            return;
        }
    }
    for (auto it = receivers_.begin(); it != receivers_.end(); it++)
    {
        if (it->id_ == id)
        {
            it->Cleanup();
            receivers_.erase(it);
            return;
        }
    }
}

extern "C" void UNITY_INTERFACE_EXPORT * GetTexturePtr(int id)
{
    for (auto & sender : senders_) if (sender.id_ == id) return sender.view_;
    for (auto & receiver : receivers_) if (receiver.id_ == id) return receiver.view_;
    return nullptr; // No such a sender/receiver was found.
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureWidth(int id)
{
    for (auto & sender : senders_) if (sender.id_ == id) return sender.width_;
    for (auto & receiver : receivers_) if (receiver.id_ == id) return receiver.width_;
    return 0; // No such a sender/receiver was found.
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureHeight(int id)
{
    for (auto & sender : senders_) if (sender.id_ == id) return sender.height_;
    for (auto & receiver : receivers_) if (receiver.id_ == id) return receiver.height_;
    return 0; // No such a sender/receiver was found.
}

extern "C" int UNITY_INTERFACE_EXPORT CountSharedTextures()
{
    return klakspout::Globals::get().sender_names_->GetSenderCount();
}

extern "C" void UNITY_INTERFACE_EXPORT * GetSharedTextureName(int index)
{
    static char name[SpoutMaxSenderNameLen];
    unsigned int width, height;
    HANDLE handle;
    klakspout::Globals::get().sender_names_->GetSenderNameInfo(index, name, SpoutMaxSenderNameLen, width, height, handle);
    return name;
}