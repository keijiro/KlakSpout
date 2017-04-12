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

        int _receiverID;
        Material _fixupMaterial;
        MaterialPropertyBlock _propertyBlock;

        void SearchTexture()
        {
            var name = PluginEntry.SearchSharedTextureNameString(_nameFilter);
            if (name != null) _receiverID = PluginEntry.CreateReceiver(name);
        }

        #endregion

        #region MonoBehaviour functions

        void Start()
        {
            _fixupMaterial = new Material(Shader.Find("Hidden/Spout/Fixup"));
            _propertyBlock = new MaterialPropertyBlock();

            // Initial search.
            SearchTexture();
        }

        void OnDestroy()
        {
            if (_receiverID != 0) PluginEntry.Destroy(_receiverID);
            if (_sharedTexture != null) Destroy(_sharedTexture);
            if (_fixedTexture != null) Destroy(_fixedTexture);
        }

        void Update()
        {
            PluginEntry.Poll();

            // Search the texture list if we haven't found one.
            if (_receiverID == 0) SearchTexture();

            // Try to initialize the shared texture if not yet initialized.
            if (_receiverID != 0 && _sharedTexture == null)
            {
                var ptr = PluginEntry.GetTexturePtr(_receiverID);
                if (ptr != System.IntPtr.Zero)
                {
                    _sharedTexture = Texture2D.CreateExternalTexture(
                        PluginEntry.GetTextureWidth(_receiverID),
                        PluginEntry.GetTextureHeight(_receiverID),
                        TextureFormat.ARGB32, false, false, ptr
                    );
                }
            }

            // Update external objects.
            if (_sharedTexture != null)
            {
                if (_targetTexture != null)
                {
                    Graphics.Blit(_sharedTexture, _targetTexture, _fixupMaterial, 1);
                }
                else
                {
                    if (_fixedTexture == null)
                        _fixedTexture = new RenderTexture(
                            _sharedTexture.width, _sharedTexture.height, 0);
                    Graphics.Blit(_sharedTexture, _fixedTexture, _fixupMaterial, 1);
                }

                if (_targetRenderer != null)
                {
                    _propertyBlock.SetTexture(_targetMaterialProperty, receivedTexture);
                    _targetRenderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        #endregion
    }
}
