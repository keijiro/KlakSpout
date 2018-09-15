// KlakSpout - Spout video frame sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;

namespace Klak.Spout
{
    [ExecuteInEditMode]
    [AddComponentMenu("Klak/Spout/Spout Sender")]
    public sealed class SpoutSender : MonoBehaviour
    {
        #region Source settings

        [SerializeField] RenderTexture _sourceTexture;

        public RenderTexture sourceTexture {
            get { return _sourceTexture; }
            set { _sourceTexture = value; }
        }

        #endregion

        #region Format options

        [SerializeField] bool _alphaSupport;

        public bool alphaSupport {
            get { return _alphaSupport; }
            set { _alphaSupport = value; }
        }

        #endregion

        #region Private members

        System.IntPtr _plugin;
        Texture2D _sharedTexture;
        Material _blitMaterial;

        void SendRenderTexture(RenderTexture source)
        {
            // Plugin lazy initialization
            if (_plugin == System.IntPtr.Zero)
            {
                _plugin = PluginEntry.CreateSender(name, source.width, source.height);
                if (_plugin == System.IntPtr.Zero) return; // Spout may not be ready.
            }

            // Shared texture lazy initialization
            if (_sharedTexture == null)
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
                }
            }

            // Shared texture update
            if (_sharedTexture != null)
            {
                // Blit shader lazy initialization
                if (_blitMaterial == null)
                {
                    _blitMaterial = new Material(Shader.Find("Hidden/Spout/Blit"));
                    _blitMaterial.hideFlags = HideFlags.DontSave;
                }

                // Blit shader parameters
                _blitMaterial.SetFloat("_ClearAlpha", _alphaSupport ? 0 : 1);

                // We can't directly blit to the shared texture (as it lacks
                // render buffer functionality), so we temporarily allocate a
                // render texture as a middleman, blit the source to it, then
                // copy it to the shared texture using the CopyTexture API.
                var tempRT = RenderTexture.GetTemporary
                    (_sharedTexture.width, _sharedTexture.height);
                Graphics.Blit(source, tempRT, _blitMaterial, 0);
                Graphics.CopyTexture(tempRT, _sharedTexture);
                RenderTexture.ReleaseTemporary(tempRT);
            }
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
        }

        void Update()
        {
            // Update the plugin internal state.
            if (_plugin != System.IntPtr.Zero)
                Util.IssuePluginEvent(PluginEntry.Event.Update, _plugin);

            // Render texture mode update
            if (GetComponent<Camera>() == null && _sourceTexture != null)
                SendRenderTexture(_sourceTexture);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Camera capture mode update
            SendRenderTexture(source);

            // Thru blit
            Graphics.Blit(source, destination);
        }

        #endregion
    }
}
