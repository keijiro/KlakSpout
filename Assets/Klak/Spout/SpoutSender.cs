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
        #region Editable properties

        [SerializeField] bool _clearAlpha = true;

        public bool clearAlpha {
            get { return _clearAlpha; }
            set { _clearAlpha = value; }
        }

        #endregion

        #region Private variables

        System.IntPtr _sender;
        Texture2D _sharedTexture;
        Material _fixupMaterial;

        #endregion

        #region MonoBehaviour functions

        void Start()
        {
            _fixupMaterial = new Material(Shader.Find("Hidden/Spout/Fixup"));

            var camera = GetComponent<Camera>();
            _sender = PluginEntry.CreateSender(name, camera.pixelWidth, camera.pixelHeight);
        }

        void OnDestroy()
        {
            if (_sender != System.IntPtr.Zero) PluginEntry.DestroySharedObject(_sender);
            if (_sharedTexture != null) Destroy(_sharedTexture);
        }

        void Update()
        {
            PluginEntry.Poll();

            _fixupMaterial.SetFloat("_ClearAlpha", _clearAlpha ? 1 : 0);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Try to initialize the shared texture if not yet initialized.
            if (_sender != System.IntPtr.Zero && _sharedTexture == null)
            {
                var ptr = PluginEntry.GetTexturePointer(_sender);
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
