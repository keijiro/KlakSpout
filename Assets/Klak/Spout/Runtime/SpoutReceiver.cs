// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;

namespace Klak.Spout
{
    // Spout receiver class
    [ExecuteInEditMode]
    [AddComponentMenu("Klak/Spout/Spout Receiver")]
    public sealed class SpoutReceiver : MonoBehaviour
    {
        #region Source settings

        [SerializeField] string _nameFilter;

        public string nameFilter {
            get { return _nameFilter; }
            set { _nameFilter = value; }
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

        [SerializeField] string _targetMaterialProperty;

        public string targetMaterialProperty {
            get { return _targetMaterialProperty; }
            set { targetMaterialProperty = value; }
        }

        #endregion

        #region Public properties

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

        #region MonoBehaviour functions

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
                // No plugin instance exists: Search the spout sender list with
                // the name filter and connect to found one (if any).
                var name = PluginEntry.SearchSharedObjectNameString(_nameFilter);
                if (name != null) _plugin = PluginEntry.CreateReceiver(name);
            }
            else
            {
                // A plugin instance exists: Check if the connection is still
                // alive. Destroy it when disconnected.
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

                    // The previously allocated buffer should be disposed to
                    // refresh it. This is needed to follow changes in the
                    // dimensions of the shared texture.
                    if (_receivedTexture == null) Util.Destroy(_receivedTexture);
                }
            }

            // Blit the shared texture to the destination.
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
                    // Blit to the target texture.
                    Graphics.Blit(_sharedTexture, _targetTexture, _blitMaterial, 1);
                }
                else
                {
                    // Receive buffer lazy initialization
                    if (_receivedTexture == null)
                    {
                        _receivedTexture = new RenderTexture(_sharedTexture.width, _sharedTexture.height, 0);
                        _receivedTexture.hideFlags = HideFlags.DontSave;
                    }

                    // Blit to the receive buffer.
                    Graphics.Blit(_sharedTexture, _receivedTexture, _blitMaterial, 1);
                }
            }

            // Target renderer override
            if (_targetRenderer != null)
            {
                if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
                _targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetTexture(_targetMaterialProperty, receivedTexture);
                _targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        #endregion
    }
}
