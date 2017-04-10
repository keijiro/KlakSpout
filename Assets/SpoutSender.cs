using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Klak.Spout
{
    public class SpoutSender : MonoBehaviour
    {
        #region Editable properties

        [SerializeField] int _width = 1280;
        [SerializeField] int _height = 720;

        #endregion

        #region Private resources

        RenderTexture _renderTarget;
        Texture2D _sharedTexture;
        int _senderID;

        static int _lastUpdateFrame;

        #endregion

        #region MonoBehaviour functions

        void OnValidate()
        {
            _width = Mathf.Max(16, _width);
            _height = Mathf.Max(16, _height);
        }

        void OnDestroy()
        {
            DestroySender(_senderID);
            Destroy(_renderTarget);
        }

        void Start()
        {
            _renderTarget = new RenderTexture(_width, _height, 24);
            GetComponent<Camera>().targetTexture = _renderTarget;

            _senderID = CreateSender(name, _width, _height);
        }

        void Update()
        {
            if (Time.frameCount != _lastUpdateFrame)
            {
                GL.IssuePluginEvent(GetRenderEventFunc(), 0);
                _lastUpdateFrame = Time.frameCount;
            }

            if (_sharedTexture == null)
            {
                var ptr = GetSenderTexturePtr(_senderID);
                if (ptr != System.IntPtr.Zero)
                    _sharedTexture = Texture2D.CreateExternalTexture(
                        _width, _height, TextureFormat.ARGB32, false, false, ptr);
            }

            if (_sharedTexture != null)
                Graphics.CopyTexture(_renderTarget, _sharedTexture);
        }

        #endregion

        #region Native plugin interface

        [DllImport("KlakSpout")]
        static extern System.IntPtr GetRenderEventFunc();

        [DllImport("KlakSpout")]
        static extern int CreateSender(string name, int width, int height);

        [DllImport("KlakSpout")]
        static extern void DestroySender(int id);

        [DllImport("KlakSpout")]
        static extern System.IntPtr GetSenderTexturePtr(int id);

        #endregion
    }
}
