// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;

namespace Klak.Spout
{
    /// Spout sender class
    [AddComponentMenu("Klak/Spout/Spout Sender")]
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class SpoutSender : MonoBehaviour
    {
        #region Editable properties

        [SerializeField] bool _clearAlpha = true;

        public bool clearAlpha {
            get { return _clearAlpha; }
            set { _clearAlpha = value; }
        }

        #endregion

        #region Private members

        System.IntPtr _sender;
        Texture2D _sharedTexture;
        Material _fixupMaterial;

        #endregion

        #region MonoBehaviour functions

        void OnEnable()
        {
            var camera = GetComponent<Camera>();
            _sender = PluginEntry.CreateSender(name, camera.pixelWidth, camera.pixelHeight);
        }

        void OnDisable()
        {
            if (_sender != System.IntPtr.Zero)
            {
                PluginEntry.DestroySharedObject(_sender);
                _sender = System.IntPtr.Zero;
            }

            if (_sharedTexture != null)
            {
                if (Application.isPlaying)
                    Destroy(_sharedTexture);
                else
                    DestroyImmediate(_sharedTexture);
                _sharedTexture = null;
            }
        }

        void Update()
        {
            PluginEntry.Poll();
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Lazy initialization for the shared texture.
            if (_sharedTexture == null)
            {
                var ptr = PluginEntry.GetTexturePointer(_sender);
                if (ptr != System.IntPtr.Zero)
                {
                    _sharedTexture = Texture2D.CreateExternalTexture(
                        PluginEntry.GetTextureWidth(_sender),
                        PluginEntry.GetTextureHeight(_sender),
                        TextureFormat.ARGB32, false, false, ptr
                    );
                }
            }

            // Update the shared texture.
            if (_sharedTexture != null)
            {
                // Lazy initialization for the fix-up shader.
                if (_fixupMaterial == null)
                    _fixupMaterial = new Material(Shader.Find("Hidden/Spout/Fixup"));

                // Parameters for the fix-up shader.
                _fixupMaterial.SetFloat("_ClearAlpha", _clearAlpha ? 1 : 0);

                // Apply the fix-up shader.
                var tempRT = RenderTexture.GetTemporary(_sharedTexture.width, _sharedTexture.height);
                Graphics.Blit(source, tempRT, _fixupMaterial, 0);

                // Copy the result to the shared texture.
                Graphics.CopyTexture(tempRT, _sharedTexture);

                // Release temporaries.
                RenderTexture.ReleaseTemporary(tempRT);
            }

            // Just transfer the source to the destination.
            Graphics.Blit(source, destination);
        }

        #endregion
    }
}
