#include "KlakSpoutSharedObject.h"
#include "Unity/IUnityGraphics.h"
#include "Unity/IUnityGraphicsD3D11.h"
#include <list>
#include <mutex>

namespace
{
    // Low-level native plugin interface
    IUnityInterfaces* unity_;

    // Shared Spout object list
    std::list<std::unique_ptr<klakspout::SharedObject>> shared_objects_;
    std::mutex shared_objects_lock_;

    // Temporary storage for shared Spout object list
    std::set<std::string> shared_object_names_;

    // Remove an object from the shared Spout object list.
    void remove_shared_object(klakspout::SharedObject* pobj)
    {
        std::lock_guard<std::mutex> guard(shared_objects_lock_);
        shared_objects_.remove_if([pobj](auto& p) { return p.get() == pobj; });
    }

    // Unity device event callback
    void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType event_type)
    {
        auto& g = klakspout::Globals::get();

        DEBUG_LOG("OnGraphicsDeviceEvent(%d)", event_type);

        if (event_type == kUnityGfxDeviceEventInitialize && !g.spout_)
        {
            // Initialize the Spout globals.
            g.spout_ = std::make_unique<spoutDirectX>();
            g.sender_names_ = std::make_unique<spoutSenderNames>();
        }

        if (event_type == kUnityGfxDeviceEventShutdown && g.spout_)
        {
            // Release all the Spout shared objects.
            shared_objects_.clear();

            // Finalize the Spout globals.
            g.spout_.reset();
            g.sender_names_.reset();
        }
    }

    // Unity render event callback
    void UNITY_INTERFACE_API OnRenderEvent(int event_id)
    {
        // Update D3D11 resources with the shared Spout objects.
        // Note that this has to be done in the render thread.
        std::lock_guard<std::mutex> guard(shared_objects_lock_);
        for (const auto& p : shared_objects_) p->updateResources();
    }
}

//
// Low-level native plugin implementation
//

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* interfaces)
{
    auto& g = klakspout::Globals::get();

    // Replace stdout with a new console for debugging.
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
    auto& g = klakspout::Globals::get();

    // Unregister the custom callback.
    unity_->Get<IUnityGraphics>()->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);

    // Invalidate the interface pointers.
    unity_ = nullptr;
    g.d3d11_ = nullptr;
}

extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT GetRenderEventFunc()
{
    return OnRenderEvent;
}

//
// Native plugin implementation
//

extern "C" void UNITY_INTERFACE_EXPORT * CreateSender(const char* name, int width, int height)
{
    std::lock_guard<std::mutex> guard(shared_objects_lock_);
    auto pobj = new klakspout::SharedObject(klakspout::SharedObject::kSender, name, width, height);
    shared_objects_.emplace_front(pobj);
    return pobj;
}

extern "C" void UNITY_INTERFACE_EXPORT * TryCreateReceiver(const char* name)
{
    auto& g = klakspout::Globals::get();

    // Do nothing if it can't find a sender object with the given name.
    if (!name || !g.sender_names_->FindSenderName(name)) return nullptr;

    std::lock_guard<std::mutex> guard(shared_objects_lock_);
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

extern "C" int UNITY_INTERFACE_EXPORT ScanSharedObjects()
{
    klakspout::Globals::get().sender_names_->GetSenderNames(&shared_object_names_);
    return shared_object_names_.size();
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