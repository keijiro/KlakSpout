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
        Material _fixupMaterial;

        #endregion

        #region MonoBehaviour functions

        void Start()
        {
            var camera = GetComponent<Camera>();
            _senderID = PluginEntry.CreateSender(name, camera.pixelWidth, camera.pixelHeight);
            _fixupMaterial = new Material(Shader.Find("Hidden/Spout/Fixup"));
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
            {
                var tempRT = RenderTexture.GetTemporary(source.width, source.height);
                Graphics.Blit(source, tempRT, _fixupMaterial, 0);
                Graphics.CopyTexture(tempRT, _sharedTexture);
                RenderTexture.ReleaseTemporary(tempRT);
            }

            // Just transfer the source to the destination.
            Graphics.Blit(source, destination);
        }

        #endregion
    }
}
