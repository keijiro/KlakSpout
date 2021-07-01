#pragma once

#include "Common.h"
#include "Sender.h"
#include "Receiver.h"

namespace KlakSpout {

// Render event IDs
// Should match with Klak.Spout.EventID (Event.cs)
enum EventID
{
    event_updateSender,
    event_updateReceiver,
    event_closeSender,
    event_closeReceiver
};

// Render event attachment data structure
// Should match with Klak.Spout.EventData (Event.cs)
struct EventData
{
    union
    {
        Sender* sender;
        Receiver* receiver;
    };
    IUnknown* texture; // ID3D11Texture or ID3D12Resource
};

} // namespace KlakSpout
