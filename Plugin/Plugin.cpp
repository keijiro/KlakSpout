#include "Common.h"
#include "Event.h"
#include "Receiver.h"
#include "Sender.h"
#include "System.h"
#include "Util.h"
#include <mutex>

using namespace KlakSpout;

namespace {

// Local mutex object used to prevent race conditions between the main thread
// and the render thread. This should be locked at the following points:
// - OnRenderEvent (this is the only point called from the render thread)
// - Plugin functions that use the Spout API functions.
std::mutex lock_;

// Graphics device event callback
void UNITY_INTERFACE_API
  OnGraphicsDeviceEvent(UnityGfxDeviceEventType event_type)
{
    if (event_type == kUnityGfxDeviceEventShutdown) _system->shutdown();
}

// Render event (via IssuePluginEvent) callback
void UNITY_INTERFACE_API
  OnRenderEvent(int event_id, void* event_data)
{
    std::lock_guard<std::mutex> guard(lock_);
    auto data = reinterpret_cast<const EventData*>(event_data);
    if (event_id == event_updateSender  ) data->sender->update(data->texture);
    if (event_id == event_updateReceiver) data->receiver->update();
    if (event_id == event_closeSender  ) delete data->sender;
    if (event_id == event_closeReceiver) delete data->receiver;
}

} // anonymous namespace

// Unity low-level native plugin interface

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
  UnityPluginLoad(IUnityInterfaces* interfaces)
{
    // System object instantiation, callback registration
    _system = std::make_unique<System>(interfaces);
    _system->getGraphics()->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    // System object destruction
    _system->getGraphics()->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
    _system.reset();
}

// Plugin functions

extern "C" UnityRenderingEventAndData UNITY_INTERFACE_EXPORT
  GetRenderEventCallback()
{
    return OnRenderEvent;
}

extern "C" Sender UNITY_INTERFACE_EXPORT *
  CreateSender(const char* name, int width, int height)
{
    return new Sender(name, width, height);
}

extern "C" Receiver UNITY_INTERFACE_EXPORT *
  CreateReceiver(const char* name)
{
    return new Receiver(name);
}

extern "C" Receiver::InteropData UNITY_INTERFACE_EXPORT
  GetReceiverData(Receiver* receiver)
{
    return receiver->getInteropData();
}

extern "C" void UNITY_INTERFACE_EXPORT
  GetSenderNames(char*** names, int* count)
{
    std::lock_guard<std::mutex> guard(lock_);
    std::set<std::string> senders;
    _system->spout.GetSenderNames(&senders);
    std::tie(*names, *count) = MarshalStringSet(senders);
}
