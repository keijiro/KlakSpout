// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;

namespace Klak.Spout
{
    /// Spout sender class
    [AddComponentMenu("Klak/Spout/Spout Sender")]
    [RequireComponent(typeof(Camera))]
    public class SpoutSender : MonoBehaviour
    {
        #region Private variables

        int _senderID;
        Texture2D _sharedTexture;

        #endregion

        #region MonoBehaviour functions

        void Start()
        {
            var camera = GetComponent<Camera>();
            _senderID = PluginEntry.CreateSender(name, camera.pixelWidth, camera.pixelHeight);
        }

        void OnDestroy()
        {
            PluginEntry.Destroy(_senderID);

            if (_sharedTexture != null) Destroy(_sharedTexture);
        }

        void Update()
        {
            PluginEntry.Poll();
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Try to initialize the shared texture if not yet initialized.
            if (_sharedTexture == null)
            {
                var ptr = PluginEntry.GetTexturePtr(_senderID);
                if (ptr != System.IntPtr.Zero)
                {
                    _sharedTexture = Texture2D.CreateExternalTexture(
                        source.width, source.height,
                        TextureFormat.ARGB32, false, false, ptr
                    );
                }
            }

            // Update the shared texture.
            if (_sharedTexture != null)
                Graphics.CopyTexture(source, _sharedTexture);

            // Just transfer the source to the destination.
            Graphics.Blit(source, destination);
        }

        #endregion
    }
}
