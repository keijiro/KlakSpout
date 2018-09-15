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
                Util.IssuePluginEvent(PluginEntry.Event.Dispose, _plugin);
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
            // Release the plugin instance when the previously established
            // connection is now invalid.
            if (_plugin != System.IntPtr.Zero && !PluginEntry.CheckValid(_plugin))
            {
                Util.IssuePluginEvent(PluginEntry.Event.Dispose, _plugin);
                _plugin = System.IntPtr.Zero;
            }

            // Plugin lazy initialization
            if (_plugin == System.IntPtr.Zero)
            {
                _plugin = PluginEntry.CreateReceiver(_sourceName);
                if (_plugin == System.IntPtr.Zero) return; // Spout may not be ready.
            }

            Util.IssuePluginEvent(PluginEntry.Event.Update, _plugin);

            // Texture information retrieval
            var ptr = PluginEntry.GetTexturePointer(_plugin);
            var width = PluginEntry.GetTextureWidth(_plugin);
            var height = PluginEntry.GetTextureHeight(_plugin);

            // Resource validity check
            if (_sharedTexture != null)
            {
                if (ptr != _sharedTexture.GetNativeTexturePtr() ||
                    width != _sharedTexture.width ||
                    height != _sharedTexture.height)
                {
                    // Not match: Destroy to get refreshed.
                    Util.Destroy(_sharedTexture);
                }
            }

            // Shared texture lazy (re)initialization
            if (_sharedTexture == null && ptr != System.IntPtr.Zero)
            {
                _sharedTexture = Texture2D.CreateExternalTexture(
                    width, height, TextureFormat.ARGB32, false, false, ptr
                );
                _sharedTexture.hideFlags = HideFlags.DontSave;

                // Destroy the previously allocated receiver texture to
                // refresh specifications.
                if (_receivedTexture == null) Util.Destroy(_receivedTexture);
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
            if (_targetRenderer != null && receivedTexture != null)
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
