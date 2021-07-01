using UnityEngine;
using UnityEngine.Rendering;

namespace Klak.Spout {

//
// Spout sender class (main implementation)
//
[ExecuteInEditMode]
[AddComponentMenu("Klak/Spout/Spout Sender")]
public sealed partial class SpoutSender : MonoBehaviour
{
    #region Sender plugin object

    Sender _sender;

    void ReleaseSender()
    {
        _sender?.Dispose();
        _sender = null;
    }

    #endregion

    #region Buffer texture object

    RenderTexture _buffer;

    void PrepareBuffer(int width, int height)
    {
        // If the buffer exists but has wrong dimensions, destroy it first.
        if (_buffer != null &&
            (_buffer.width != width || _buffer.height != height))
        {
            ReleaseSender();
            Utility.Destroy(_buffer);
            _buffer = null;
        }

        // Create a buffer if it hasn't been allocated yet.
        if (_buffer == null && width > 0 && height > 0)
        {
            _buffer = new RenderTexture(width, height, 0);
            _buffer.hideFlags = HideFlags.DontSave;
            _buffer.Create();
        }
    }

    #endregion

    #region Camera capture (SRP)

    Camera _attachedCamera;

    void OnCameraCapture(RenderTargetIdentifier source, CommandBuffer cb)
    {
        if (_attachedCamera == null) return;
        Blitter.Blit(_resources, cb, source, _buffer, _keepAlpha);
    }

    void PrepareCameraCapture(Camera target)
    {
        // If it has been attached to another camera, detach it first.
        if (_attachedCamera != null && _attachedCamera != target)
        {
            #if KLAK_SPOUT_HAS_SRP
            CameraCaptureBridge
              .RemoveCaptureAction(_attachedCamera, OnCameraCapture);
            #endif
            _attachedCamera = null;
        }

        // Attach to the target if it hasn't been attached yet.
        if (_attachedCamera == null && target != null)
        {
            #if KLAK_SPOUT_HAS_SRP
            CameraCaptureBridge
              .AddCaptureAction(target, OnCameraCapture);
            #endif
            _attachedCamera = target;
        }
    }

    #endregion

    #region MonoBehaviour implementation

    void OnDisable()
    {
        ReleaseSender();
        PrepareBuffer(0, 0);
        PrepareCameraCapture(null);
    }

    void Update()
    {
        // GameView capture mode
        if (_captureMethod == CaptureMethod.GameView)
        {
            PrepareBuffer(Screen.width, Screen.height);
            var temp = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(temp);
            Blitter.BlitVFlip(_resources, temp, _buffer, _keepAlpha);
            RenderTexture.ReleaseTemporary(temp);
        }

        // Texture capture mode
        if (_captureMethod == CaptureMethod.Texture)
        {
            if (_sourceTexture == null) return;
            PrepareBuffer(_sourceTexture.width, _sourceTexture.height);
            Blitter.Blit(_resources, _sourceTexture, _buffer, _keepAlpha);
        }

        // Camera capture mode
        if (_captureMethod == CaptureMethod.Camera)
        {
            PrepareCameraCapture(_sourceCamera);
            if (_sourceCamera == null) return;
            PrepareBuffer(_sourceCamera.pixelWidth, _sourceCamera.pixelHeight);
        }

        // Sender lazy initialization
        if (_sender == null) _sender = new Sender(_spoutName, _buffer);

        // Sender plugin-side update
        _sender.Update();
    }

    #endregion
}

} // namespace Klak.Spout
