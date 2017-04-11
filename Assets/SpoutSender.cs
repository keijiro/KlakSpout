using UnityEngine;

namespace Klak.Spout
{
    public class SpoutSender : MonoBehaviour
    {
        #region Editable properties

        [SerializeField] int _width = 1280;
        [SerializeField] int _height = 720;

        public int Width { get { return _width; } set { _width = value; } }
        public int Height { get { return _height; } set { _height = value; } }

        #endregion

        #region Private resources

        RenderTexture _renderTarget;
        Texture2D _sharedTexture;
        int _senderID;

        #endregion

        #region MonoBehaviour functions

        void OnValidate()
        {
            _width = Mathf.Max(16, _width);
            _height = Mathf.Max(16, _height);
        }

        void OnDestroy()
        {
            PluginEntry.Destroy(_senderID);
            Destroy(_renderTarget);
            Destroy(_sharedTexture);
        }

        void Start()
        {
            _renderTarget = new RenderTexture(_width, _height, 24);
            GetComponent<Camera>().targetTexture = _renderTarget;

            _senderID = PluginEntry.CreateSender(name, _width, _height);
        }

        void Update()
        {
            PluginEntry.Poll();

            if (_sharedTexture == null)
            {
                var ptr = PluginEntry.GetTexturePtr(_senderID);
                if (ptr != System.IntPtr.Zero)
                    _sharedTexture = Texture2D.CreateExternalTexture(
                        _width, _height, TextureFormat.ARGB32, false, false, ptr);
            }

            if (_sharedTexture != null)
            {
                if (_renderTarget.IsCreated())
                {
                    Graphics.CopyTexture(_renderTarget, _sharedTexture);
                }
            } 
                
        }

        #endregion
    }
}
