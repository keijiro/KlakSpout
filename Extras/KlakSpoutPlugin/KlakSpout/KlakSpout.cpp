#include "KlakSpoutSharedObject.h"
#include "Unity/IUnityGraphics.h"
#include "Unity/IUnityGraphicsD3D11.h"
#include <list>
#include <mutex>

namespace
{
    using SharedObjectState = klakspout::SharedObject::State;

    // Low-level native plugin interface
    IUnityInterfaces* unity_;

    // Shared Spout object list
    std::list<klakspout::SharedObject> shared_objects_;

    // Temporary storage for shared Spout object list
    std::set<std::string> shared_object_names_;

    // Local mutex object used to prevent race conditions between the main
    // thread and the render thread. This should be locked at the following
    // points:
    // - OnRenderEvent (this is the only point called from the render thread)
    // - Plugin function that accesses shared_objects_
    // - Plugin function that uses Spout API
    std::mutex lock_;

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

            // Set the maximum number of senders.
            // This should be exposed to the C# side, but it's a little bit
            // tricky as this setting is not allowed to modify after
            // initialization. So we simply chose to increase it to 32
            // (default is 10) as an ad-hoc workaround.
            g.sender_names_->SetMaxSenders(32);
        }

        if (event_type == kUnityGfxDeviceEventShutdown && g.spout_)
        {
            // Run the last update to process pending release request.
            for (auto& obj : shared_objects_) obj.updateFromRenderThread();

            // Release the object list for checking leaks.
            shared_objects_.clear();

            // Finalize the Spout globals.
            g.spout_.reset();
            g.sender_names_.reset();
        }
    }

    // Unity render event callback
    void UNITY_INTERFACE_API OnRenderEvent(int event_id)
    {
        std::lock_guard<std::mutex> guard(lock_);

        // Run render thread-dependent update with the shared Spout objects.
        for (auto& obj : shared_objects_) obj.updateFromRenderThread();

        // Remove destroyed objects from the list.
        shared_objects_.remove_if([](const auto& obj) { return obj.state_ == SharedObjectState::destroyed; });
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
    std::lock_guard<std::mutex> guard(lock_);
    shared_objects_.emplace_front(klakspout::SharedObject::Type::sender, name, width, height);
    return &shared_objects_.front();
}

extern "C" void UNITY_INTERFACE_EXPORT * TryCreateReceiver(const char* name)
{
    std::lock_guard<std::mutex> guard(lock_);

    // Do nothing if it can't find a sender object with the given name.
    auto& g = klakspout::Globals::get();
    if (!name || !g.sender_names_->FindSenderName(name)) return nullptr;

    shared_objects_.emplace_front(klakspout::SharedObject::Type::receiver, name);
    return &shared_objects_.front();
}

extern "C" UNITY_INTERFACE_EXPORT void DestroySharedObject(void* ptr)
{
    auto* pobj = reinterpret_cast<klakspout::SharedObject*>(ptr);
    // If the object is in initialized state, there must be nothing to
    // release, so it can directly go to the destroyed state. The only
    // thing we actually take care of is the active state.
    if (pobj->state_ == SharedObjectState::initialized)
        pobj->state_ = SharedObjectState::destroyed;
    else if (pobj->state_ == SharedObjectState::active)
        pobj->state_ =  SharedObjectState::released;
}

extern "C" bool UNITY_INTERFACE_EXPORT DetectDisconnection(void* ptr)
{
    std::lock_guard<std::mutex> guard(lock_);
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->detectDisconnection();
}

extern "C" void UNITY_INTERFACE_EXPORT * GetTexturePointer(void* ptr)
{
    auto const* pobj = reinterpret_cast<const klakspout::SharedObject*>(ptr);
    return pobj->state_ == SharedObjectState::active ? pobj->d3d11_resource_view_ : nullptr;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureWidth(void* ptr)
{
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->width_;
}

extern "C" int UNITY_INTERFACE_EXPORT GetTextureHeight(void* ptr)
{
    return reinterpret_cast<const klakspout::SharedObject*>(ptr)->height_;
}

extern "C" int UNITY_INTERFACE_EXPORT ScanSharedObjects()
{
    std::lock_guard<std::mutex> guard(lock_);
    klakspout::Globals::get().sender_names_->GetSenderNames(&shared_object_names_);
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
