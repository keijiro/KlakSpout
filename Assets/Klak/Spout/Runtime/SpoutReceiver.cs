// KlakSpout - Spout video frame sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;

namespace Klak.Spout
{
    [ExecuteInEditMode]
    [AddComponentMenu("Klak/Spout/Spout Receiver")]
    public sealed class SpoutReceiver : MonoBehaviour
    {
        #region Source settings

        [SerializeField] string _sourceName;

        public string sourceName {
            get { return _sourceName; }
            set {
                if (_sourceName == value) return;
                _sourceName = value;
                RequestReconnect();
            }
        }

        #endregion

        #region Target settings

        [SerializeField] RenderTexture _targetTexture;

        public RenderTexture targetTexture {
            get { return _targetTexture; }
            set { _targetTexture = value; }
        }

        [SerializeField] Renderer _targetRenderer;

        public Renderer targetRenderer {
            get { return _targetRenderer; }
            set { _targetRenderer = value; }
        }

        [SerializeField] string _targetMaterialProperty = null;

        public string targetMaterialProperty {
            get { return _targetMaterialProperty; }
            set { targetMaterialProperty = value; }
        }

        #endregion

        #region Runtime properties

        RenderTexture _receivedTexture;

        public Texture receivedTexture {
            get { return _targetTexture != null ? _targetTexture : _receivedTexture; }
        }

        #endregion

        #region Private members

        System.IntPtr _plugin;
        Texture2D _sharedTexture;
        Material _blitMaterial;
        MaterialPropertyBlock _propertyBlock;

        #endregion

        #region Internal members

        internal void RequestReconnect()
        {
            OnDisable();
        }

        #endregion

        #region MonoBehaviour implementation

        void OnDisable()
        {
            if (_plugin != System.IntPtr.Zero)
            {
                PluginEntry.DestroySharedObject(_plugin);
                _plugin = System.IntPtr.Zero;
            }

            Util.Destroy(_sharedTexture);
        }

        void OnDestroy()
        {
            Util.Destroy(_blitMaterial);
            Util.Destroy(_receivedTexture);
        }

        void Update()
        {
            PluginEntry.Poll();

            // Plugin initialization/termination
            if (_plugin == System.IntPtr.Zero)
            {
                // No plugin instance exists:
                // Try connecting to the specified Spout source.
                _plugin = PluginEntry.TryCreateReceiver(_sourceName);
            }
            else
            {
                // A plugin instance exists:
                // Check if the connection is still alive. If it seems to be
                // disconnected from the source, dispose the instance.
                if (PluginEntry.DetectDisconnection(_plugin)) OnDisable();
            }

            // Shared texture lazy initialization
            if (_plugin != System.IntPtr.Zero && _sharedTexture == null)
            {
                var ptr = PluginEntry.GetTexturePointer(_plugin);
                if (ptr != System.IntPtr.Zero)
                {
                    _sharedTexture = Texture2D.CreateExternalTexture(
                        PluginEntry.GetTextureWidth(_plugin),
                        PluginEntry.GetTextureHeight(_plugin),
                        TextureFormat.ARGB32, false, false, ptr
                    );
                    _sharedTexture.hideFlags = HideFlags.DontSave;

                    // Dispose a previously allocated instance of receiver
                    // texture to refresh texture specifications.
                    if (_receivedTexture == null) Util.Destroy(_receivedTexture);
                }
            }

            // Texture format conversion with the blit shader
            if (_sharedTexture != null)
            {
                // Blit shader lazy initialization
                if (_blitMaterial == null)
                {
                    _blitMaterial = new Material(Shader.Find("Hidden/Spout/Blit"));
                    _blitMaterial.hideFlags = HideFlags.DontSave;
                }

                if (_targetTexture != null)
                {
                    // Blit the shared texture to the target texture.
                    Graphics.Blit(_sharedTexture, _targetTexture, _blitMaterial, 1);
                }
                else
                {
                    // Receiver texture lazy initialization
                    if (_receivedTexture == null)
                    {
                        _receivedTexture = new RenderTexture
                            (_sharedTexture.width, _sharedTexture.height, 0);
                        _receivedTexture.hideFlags = HideFlags.DontSave;
                    }

                    // Blit the shared texture to the receiver texture.
                    Graphics.Blit(_sharedTexture, _receivedTexture, _blitMaterial, 1);
                }
            }

            // Renderer override
            if (_targetRenderer != null)
            {
                // Material property block lazy initialization
                if (_propertyBlock == null)
                    _propertyBlock = new MaterialPropertyBlock();

                // Read-modify-write
                _targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetTexture(_targetMaterialProperty, receivedTexture);
                _targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        #if UNITY_EDITOR

        // Invoke update on repaint in edit mode. This is needed to update the
        // shared texture without getting the object marked dirty.

        void OnRenderObject()
        {
            if (Application.isPlaying) return;

            // Graphic.Blit used in Update will change the current active RT,
            // so let us back it up and restore after Update.
            var activeRT = RenderTexture.active;
            Update();
            RenderTexture.active = activeRT;
        }

        #endif

        #endregion
    }
}
