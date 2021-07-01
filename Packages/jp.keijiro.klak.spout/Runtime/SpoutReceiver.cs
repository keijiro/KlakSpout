// KlakSpout - Spout video frame sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;
using UnityEngine.Rendering;

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
            set { _targetMaterialProperty = value; }
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
        PluginEntry.SenderInfo _senderInfo = new PluginEntry.SenderInfo();
        CommandBuffer _commandBuffer;
        int _middlemanRenderTextureID;
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
        }

        void OnDestroy()
        {
            Util.Destroy(_blitMaterial);
            Util.Destroy(_receivedTexture);
        }

        void Update()
        {
            PluginEntry.GetSenderInfo(_sourceName, out _senderInfo);
            
            // Release the plugin instance when the previously established
            // connection is now invalid.
            if (_plugin != System.IntPtr.Zero && PluginEntry.IsReady(_plugin))
            {
                var width = PluginEntry.GetTextureWidth(_plugin);
                var height = PluginEntry.GetTextureHeight(_plugin);
                
                if (!_senderInfo.exists || (width != _senderInfo.width || height != _senderInfo.height))
                {
                    Util.IssuePluginEvent(PluginEntry.Event.Dispose, _plugin);
                    _plugin = System.IntPtr.Zero;
                }
            }

            var dxFormat = (PluginEntry.DXTextureFormat)_senderInfo.format;
            var rtFormat = RenderTextureFormat.ARGB32;
            var supportedFormat = PluginEntry.DXToRenderTextureFormat(dxFormat, ref rtFormat);

            // Skip if no sender found with target name or if it has invalid parameters
            if (!_senderInfo.exists || _senderInfo.width <= 0 || _senderInfo.height <= 0 || !supportedFormat)
                return;

            // Plugin lazy initialization
            if (_plugin == System.IntPtr.Zero)
            {
                _plugin = PluginEntry.CreateReceiver(_sourceName);
                if (_plugin == System.IntPtr.Zero) return; // Spout may not be ready.
            }

            // Texture format conversion with the blit shader
            // Blit shader lazy initialization
            if (_blitMaterial == null)
            {
                _blitMaterial = new Material(Shader.Find("Hidden/Spout/Blit"));
                _blitMaterial.hideFlags = HideFlags.DontSave;
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

            // Commandbuffer lazy initialization
            if (_commandBuffer == null)
            {
                _commandBuffer = new CommandBuffer();
                _commandBuffer.name = name;

                _middlemanRenderTextureID = Shader.PropertyToID("_SpoutReceiverRT");
            }

            Util.UpdateWrapCache(Time.frameCount, _commandBuffer);

            _commandBuffer.GetTemporaryRT(
                _middlemanRenderTextureID, _senderInfo.width, _senderInfo.height, 0,
                FilterMode.Point, rtFormat,
                RenderTextureReadWrite.Linear // DX shared textures are always linear
            );

            // Set target sender on the native plugin side.
            _commandBuffer.IssuePluginEventAndData(
                PluginEntry.GetRenderEventFunc(),
                (int)PluginEntry.Event.SetTargetObject,
                _plugin
            );

            // Performs CopyTexture of the DX shared texture to the middleman RT.
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
                (int)PluginEntry.BlitCommand.Receive,
                _middlemanRenderTextureID,
                BuiltinRenderTextureType.CurrentActive, // Actually unused on the native plugin side
                0,
                0
            );

            if (_targetTexture != null)
            {
                // Blit the middleman RT to the target texture.
                _commandBuffer.Blit(_middlemanRenderTextureID, _targetTexture, _blitMaterial, 1);
            }
            else
            {
                // Receiver texture lazy initialization
                if (_receivedTexture == null)
                {
                    _receivedTexture = new RenderTexture(_senderInfo.width, _senderInfo.height, 0);
                    _receivedTexture.hideFlags = HideFlags.DontSave;
                }

                // Blit the middleman RT to the receiver texture.
                _commandBuffer.Blit(_middlemanRenderTextureID, _receivedTexture, _blitMaterial, 1);
            }

            _commandBuffer.ReleaseTemporaryRT(_middlemanRenderTextureID);

            // Schedule to run on the render thread.
            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
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
