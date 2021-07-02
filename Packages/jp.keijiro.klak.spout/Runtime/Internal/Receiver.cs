using UnityEngine;
using System.Runtime.InteropServices;
using IntPtr = System.IntPtr;

namespace Klak.Spout {

//
// Wrapper class for receiver instances on the native plugin side
//
sealed class Receiver : System.IDisposable
{
    #region Public property

    public Texture2D Texture => _texture;

    #endregion

    #region Private objects

    IntPtr _plugin;
    EventKicker _event;
    Texture2D _texture;

    #endregion

    #region Object lifecycle

    public Receiver(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName)) return;

        // Plugin object allocation
        _plugin = Plugin.CreateReceiver(sourceName);
        if (_plugin == IntPtr.Zero) return;

        // Event kicker (heap block for interop communication)
        _event = new EventKicker(new EventData(_plugin));

        // Initial update event
        _event.IssuePluginEvent(EventID.UpdateReceiver);
    }

    public void Dispose()
    {
        if (_plugin != IntPtr.Zero)
        {
            // Isssue the closer event to destroy the plugin object from the
            // render thread.
            _event.IssuePluginEvent(EventID.CloseReceiver);

            // Event kicker (interop memory) deallocation:
            // The close event above will refer to the block from the render
            // thread, so we actually can't free the memory here. To avoid this
            // problem, EventKicker uses MemoryPool to delay the memory
            // deallocation by the end of the frame.
            _event.Dispose();

            _plugin = IntPtr.Zero;
        }

        Utility.Destroy(_texture);
        _texture = null;
    }

    #endregion

    #region Frame update method

    public void Update()
    {
        if (_plugin == IntPtr.Zero) return;

        var data = Plugin.GetReceiverData(_plugin);

        // Texture refresh:
        // If we are referring to an old texture pointer, destroy it first.
        if (_texture != null &&
            _texture.GetNativeTexturePtr() != data.texturePointer)
        {
            Utility.Destroy(_texture);
            _texture = null;
        }

        // Lazy initialization:
        // We try creating a receiver texture every frame until getting a
        // correct one.
        if (_texture == null && data.texturePointer != IntPtr.Zero)
            _texture = Texture2D.CreateExternalTexture
              ((int)data.width, (int)data.height, TextureFormat.RGBA32,
               false, false, data.texturePointer);

        // Update event for the render thread
        _event.IssuePluginEvent(EventID.UpdateReceiver);
    }

    #endregion
}

} // namespace Klak.Spout
