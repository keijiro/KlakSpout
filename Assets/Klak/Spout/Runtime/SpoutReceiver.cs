// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;

namespace Klak.Spout
{
    /// Spout receiver class
    [AddComponentMenu("Klak/Spout/Spout Receiver")]
    public class SpoutReceiver : MonoBehaviour
    {
        #region Editable properties

        [SerializeField] string _nameFilter;

        public string nameFilter {
            get { return _nameFilter; }
            set { _nameFilter = value; }
        }

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

        #region Public property

        Texture2D _sharedTexture;
        RenderTexture _fixedTexture;

        public Texture receivedTexture {
            get { return _targetTexture != null ? _targetTexture : _fixedTexture; }
        }

        #endregion

        #region Private variables

        System.IntPtr _receiver;
        Material _fixupMaterial;
        MaterialPropertyBlock _propertyBlock;

        // Search the texture list and create a receiver when found one.
        void SearchAndCreateTexture()
        {
            var name = PluginEntry.SearchSharedObjectNameString(_nameFilter);
            if (name != null) _receiver = PluginEntry.CreateReceiver(name);
        }

        #endregion

        #region MonoBehaviour functions

        void Start()
        {
            _fixupMaterial = new Material(Shader.Find("Hidden/Spout/Fixup"));
            _propertyBlock = new MaterialPropertyBlock();

            // Initial search.
            SearchAndCreateTexture();
        }

        void OnDestroy()
        {
            if (_receiver != System.IntPtr.Zero)
            {
                PluginEntry.DestroySharedObject(_receiver);
                _receiver = System.IntPtr.Zero;
            }

            if (_sharedTexture != null)
            {
                Destroy(_sharedTexture);
                _sharedTexture = null;
            }

            if (_fixedTexture != null)
            {
                Destroy(_fixedTexture);
                _fixedTexture = null;
            }
        }

        void Update()
        {
            PluginEntry.Poll();

            if (_receiver == System.IntPtr.Zero)
            {
                // The receiver hasn't been set up yet; try to get one.
                SearchAndCreateTexture();
            }
            else
            {
                // We've received textures via this receiver
                // but now it's disconnected from the sender -> Destroy it.
                if (PluginEntry.GetTexturePointer(_receiver) != System.IntPtr.Zero &&
                    PluginEntry.DetectDisconnection(_receiver))
                {
                    OnDestroy();
                }
            }

            if (_receiver != System.IntPtr.Zero)
            {
                if (_sharedTexture == null)
                {
                    // Try to initialize the shared texture.
                    var ptr = PluginEntry.GetTexturePointer(_receiver);
                    if (ptr != System.IntPtr.Zero)
                    {
                        _sharedTexture = Texture2D.CreateExternalTexture(
                            PluginEntry.GetTextureWidth(_receiver),
                            PluginEntry.GetTextureHeight(_receiver),
                            TextureFormat.ARGB32, false, false, ptr
                        );
                    }
                }
                else
                {
                    // Update external objects.
                    if (_targetTexture != null)
                    {
                        Graphics.Blit(_sharedTexture, _targetTexture, _fixupMaterial, 1);
                    }
                    else
                    {
                        if (_fixedTexture == null)
                            _fixedTexture = new RenderTexture(_sharedTexture.width, _sharedTexture.height, 0);
                        Graphics.Blit(_sharedTexture, _fixedTexture, _fixupMaterial, 1);
                    }

                    if (_targetRenderer != null)
                    {
                        _propertyBlock.SetTexture(_targetMaterialProperty, receivedTexture);
                        _targetRenderer.SetPropertyBlock(_propertyBlock);
                    }
                }
            }
        }

        #endregion
    }
}
