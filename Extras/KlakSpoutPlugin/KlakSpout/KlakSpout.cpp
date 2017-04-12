#include "KlakSpoutGlobals.h"
#include "KlakSpoutSharedResource.h"
#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"
#include "Unity/IUnityGraphicsD3D11.h"
#include <list>
#include <memory>

namespace
{
    // Low-level native plugin interface
    IUnityInterfaces* unity_;

    // Shared resource list
    typedef std::list<std::shared_ptr<klakspout::SharedResource>> SharedResourceList;
    SharedResourceList resources_;
    int last_id_ = 1;

    // Find a shared resource with an ID.
    SharedResourceList::iterator find_shared_resource(int id)
    {
        auto it = resources_.begin();
        for (; it != resources_.end(); it++)
            if ((*it)->id_ == id) break;
        return it;
    }

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
            // Release all the shared resources.
            resources_.clear();

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
            // Set up all the shared resources that are not ready at this point.
            for (auto rp : resources_) if (!rp->IsReady()) rp->Setup();
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
    auto id = last_id_++;
    resources_.emplace_front(new klakspout::Sender(id, name, width, height));
    return id;
}

extern "C" UNITY_INTERFACE_EXPORT int CreateReceiver(const char* name)
{
    auto id = last_id_++;
    resources_.emplace_front(new klakspout::Receiver(id, name));
    return id;
}

extern "C" UNITY_INTERFACE_EXPORT void Destroy(int id)
{
    auto it = find_shared_resource(id);
    if (it != resources_.end()) resources_.erase(it);
}

extern "C" void UNITY_INTERFACE_EXPORT * GetTexturePtr(int id)
{
    auto it = find_shared_resource(id);
    return it != resources_.end() ? (*it)->view_ : nullptr;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureWidth(int id)
{
    auto it = find_shared_resource(id);
    return it != resources_.end() ? (*it)->width_ : 0;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureHeight(int id)
{
    auto it = find_shared_resource(id);
    return it != resources_.end() ? (*it)->height_ : 0;
}

extern "C" int UNITY_INTERFACE_EXPORT CountSharedTextures()
{
    return klakspout::Globals::get().sender_names_->GetSenderCount();
}

extern "C" const void UNITY_INTERFACE_EXPORT * GetSharedTextureName(int index)
{
    // Static string object used for storing a result.
    static string temp;

    // Retrieve all the sender names.
    std::set<std::string> names;
    klakspout::Globals::get().sender_names_->GetSenderNames(&names);

    // Return the n-th element.
    auto count = 0;
    for (auto & name : names)
    {
        if (count++ == index)
        {
            temp = name;
            return temp.c_str();
        }
    }

    return nullptr;
}

extern "C" const void UNITY_INTERFACE_EXPORT * SearchSharedTextureName(const char* keyword)
{
    // Static string object used for storing a result.
    static string temp;

    // Retrieve all the sender names.
    std::set<std::string> names;
    klakspout::Globals::get().sender_names_->GetSenderNames(&names);

    // Do nothing if the name list is empty.
    if (names.size() == 0) return nullptr;

    // Return the first element if the keyword is empty.
    if (keyword == nullptr || *keyword == 0)
    {
        temp = *names.begin();
        return temp.c_str();
    }

    // Scan the name list.
    for (auto & name : names)
    {
        if (name.find(keyword) != std::string::npos)
        {
            temp = name;
            return temp.c_str();
        }
    }

    // Nothing found.
    return nullptr;
}