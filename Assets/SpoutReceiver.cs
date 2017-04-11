using UnityEngine;

namespace Klak.Spout
{
    /// Spout receiver class
    public class SpoutReceiver : MonoBehaviour
    {
        #region Editable properties

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

        public Texture2D receivedTexture {
            get { return _sharedTexture; }
        }

        #endregion

        #region Private variables

        int _receiverID;
        MaterialPropertyBlock _propertyBlock;

        #endregion

        #region MonoBehaviour functions

        void Start()
        {
            var senderCount = PluginEntry.CountSharedTextures();

            if (senderCount == 0)
            {
                Destroy(this);
                return;
            }

            _receiverID = PluginEntry.CreateReceiver(PluginEntry.GetSharedTextureNameString(0));

            _propertyBlock = new MaterialPropertyBlock();
        }

        void OnDestroy()
        {
            PluginEntry.Destroy(_receiverID);
            Destroy(_sharedTexture);
        }

        void Update()
        {
            PluginEntry.Poll();

            // Try to initialize the shared texture if not yet.
            if (_sharedTexture == null)
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
                    Graphics.Blit(_sharedTexture, _targetTexture);

                if (_targetRenderer != null)
                {
                    _propertyBlock.SetTexture(_targetMaterialProperty, _sharedTexture);
                    _targetRenderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        #endregion
    }
}
