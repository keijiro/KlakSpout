// KlakSpout - Spout video frame sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;
using UnityEngine.Rendering;

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
        CommandBuffer _commandBuffer;
        int _middlemanRenderTextureID;
        Material _blitMaterial;

        // This is a hack to avoid trashing Spout on source resize (ex window
        // resize). Could be done through spoutSenderNames::UpdateSender on
        // the native plugin side instead to eliminate this hack and the
        // associated delay.
        const int _refreshDelay = 20;
        int _refreshDelayCounter = _refreshDelay;

        void SendRenderTexture(RenderTexture source)
        {
            // Handle source texture resize
            if (_plugin != System.IntPtr.Zero && PluginEntry.IsReady(_plugin))
            {
                var width = PluginEntry.GetTextureWidth(_plugin);
                var height = PluginEntry.GetTextureHeight(_plugin);

                if (width != source.width || height != source.height)
                {
                    Util.IssuePluginEvent(PluginEntry.Event.Dispose, _plugin);
                    _plugin = System.IntPtr.Zero;
                    _refreshDelayCounter = 0;
                }
            }

            // Hack
            _refreshDelayCounter++;
            if (_refreshDelayCounter >= 10000)
                _refreshDelayCounter = _refreshDelay; // Avoid overflow
            if (_refreshDelayCounter <= _refreshDelay)
                return;

            // Plugin lazy initialization
            if (_plugin == System.IntPtr.Zero)
            {
                _plugin = PluginEntry.CreateSender(name, source.width, source.height);
                if (_plugin == System.IntPtr.Zero) return; // Spout may not be ready.
            }

            if (_plugin != System.IntPtr.Zero)
            {
                // Blit shader lazy initialization
                if (_blitMaterial == null)
                {
                    _blitMaterial = new Material(Shader.Find("Hidden/Spout/Blit"));
                    _blitMaterial.hideFlags = HideFlags.DontSave;
                }

                // Blit shader parameters
                _blitMaterial.SetFloat("_ClearAlpha", _alphaSupport ? 0 : 1);

                // Commandbuffer lazy initialization
                if (_commandBuffer == null)
                {
                    _commandBuffer = new CommandBuffer();
                    _commandBuffer.name = name;

                    _middlemanRenderTextureID = Shader.PropertyToID("_SpoutSenderRT");
                }

                Util.UpdateWrapCache(Time.frameCount, _commandBuffer);

                // We can't directly blit to the shared texture (as it lacks
                // render buffer functionality), so we allocate a render
                // texture as a middleman, blit the source to it, then
                // pass it to the native plugin.
                _commandBuffer.GetTemporaryRT(
                    _middlemanRenderTextureID, source.width, source.height, 0,
                    FilterMode.Point,
                    RenderTextureFormat.ARGB32 // We only support this sender format on the native plugin side
                );
                _commandBuffer.Blit(source, _middlemanRenderTextureID, _blitMaterial, 0);

                // Set target sender on the native plugin side
                _commandBuffer.IssuePluginEventAndData(
                    PluginEntry.GetRenderEventFunc(),
                    (int)PluginEntry.Event.SetTargetObject,
                    _plugin
                );

                // Performs CopyTexture of the middleman RT to the DX shared texture.
                // On DX12, the middleman RT is first wrapped into a DX11 texture,
                // since DX shared textures are DX11/DX9 textures that we interface with
                // using the D3D11on12 feature.
                //
                // IssuePluginCustomBlit is used instead of IssuePluginEventAndData as
                // there's no other method to pass a temporary RT into the native side.
                // If we had a pointer to the temporary RT's native texture then
                // IssuePluginEventAndData could have been used.
                _commandBuffer.IssuePluginCustomBlit(
                    PluginEntry.GetCustomBlitFunc(),
                    (int)PluginEntry.BlitCommand.Send,
                    _middlemanRenderTextureID,
                    BuiltinRenderTextureType.CurrentActive, // Actually unused on the native plugin side
                    0,
                    0
                );

                _commandBuffer.ReleaseTemporaryRT(_middlemanRenderTextureID);

                // Schedule to run on the render thread
                Graphics.ExecuteCommandBuffer(_commandBuffer);
                _commandBuffer.Clear();
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
        }

        void OnDestroy()
        {
            Util.Destroy(_blitMaterial);
        }

        void Update()
        {
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
