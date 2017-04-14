#include "KlakSpoutGlobals.h"
#include "KlakSpoutSharedObject.h"
#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"
#include "Unity/IUnityGraphicsD3D11.h"
#include <list>
#include <memory>

namespace
{
    // Low-level native plugin interface
    IUnityInterfaces* unity_;

    // Shared object list
    typedef std::list<std::shared_ptr<klakspout::SharedObject>> SharedObjectList;
    SharedObjectList shared_objects_;

    // Remove a given object from the list.
    void remove_shared_object(klakspout::SharedObject* pobj)
    {
        shared_objects_.remove_if([pobj](auto & sp) { return sp.get() == pobj; });
    }

    // Device event callback
    void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType event_type)
    {
        auto & g = klakspout::Globals::get();

        DEBUG_LOG("OnGraphicsDeviceEvent(%d)", event_type);

        if (event_type == kUnityGfxDeviceEventInitialize && !g.spout_)
        {
            // Initialize the Spout globals.
            g.spout_ = new spoutDirectX;
            g.sender_names_ = new spoutSenderNames;
        }

        if (event_type == kUnityGfxDeviceEventShutdown && g.spout_)
        {
            // Release all the shared resources.
            shared_objects_.clear();

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
        // Update all the D3D11 resources. This has to be done in the render thread.
        for (auto p : shared_objects_) p->updateResources();
    }
}

//
// Low-level plugin interface functions
//

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces)
{
    auto & g = klakspout::Globals::get();

    // Open a new console on debug builds.
    #if defined(_DEBUG)
    FILE * pConsole;
    AllocConsole();
    freopen_s(&pConsole, "CONOUT$", "wb", stdout);
    #endif

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
// Native plugin functions
//

extern "C" void UNITY_INTERFACE_EXPORT * CreateSender(const char* name, int width, int height)
{
    auto pobj = new klakspout::SharedObject(klakspout::SharedObject::kSender, name, width, height);
    shared_objects_.emplace_front(pobj);
    return pobj;
}

extern "C" void UNITY_INTERFACE_EXPORT * CreateReceiver(const char* name)
{
    auto pobj = new klakspout::SharedObject(klakspout::SharedObject::kReceiver, name);
    shared_objects_.emplace_front(pobj);
    return pobj;
}

extern "C" UNITY_INTERFACE_EXPORT void DestroySharedObject(void* ptr)
{
    remove_shared_object(reinterpret_cast<klakspout::SharedObject*>(ptr));
}

extern "C" bool UNITY_INTERFACE_EXPORT DetectDisconnection(void* ptr)
{
    return reinterpret_cast<klakspout::SharedObject*>(ptr)->detectDisconnection();
}

extern "C" void UNITY_INTERFACE_EXPORT * GetTexturePointer(void* ptr)
{
    return reinterpret_cast<klakspout::SharedObject*>(ptr)->d3d11_resource_view_;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureWidth(void* ptr)
{
    return reinterpret_cast<klakspout::SharedObject*>(ptr)->width_;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureHeight(void* ptr)
{
    return reinterpret_cast<klakspout::SharedObject*>(ptr)->height_;
}

extern "C" int UNITY_INTERFACE_EXPORT CountSharedObjects()
{
    return klakspout::Globals::get().sender_names_->GetSenderCount();
}

extern "C" const void UNITY_INTERFACE_EXPORT * GetSharedObjectName(int index)
{
    auto & g = klakspout::Globals::get();

    // Static string object used for storing a result.
    static string temp;

    // Retrieve all the sender names.
    std::set<std::string> names;
    g.sender_names_->GetSenderNames(&names);

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

extern "C" const void UNITY_INTERFACE_EXPORT * SearchSharedObjectName(const char* keyword)
{
    auto & g = klakspout::Globals::get();

    // Static string object used for storing a result.
    static string temp;

    // Retrieve all the sender names.
    std::set<std::string> names;
    g.sender_names_->GetSenderNames(&names);

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