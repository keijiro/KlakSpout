// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;

namespace Klak.Spout
{
    /// Spout sender class
    [AddComponentMenu("Klak/Spout/Spout Sender")]
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class SpoutSender : MonoBehaviour {

        [SerializeField] bool _clearAlpha = true;
        [SerializeField] protected SpoutSenderTexture.Data data;

        [SerializeField] BoolEvent EnabledOnEnable;
        [SerializeField] BoolEvent EnabledOnDisable;
        [SerializeField] RenderTextureEvent EventOnUpdateTexture;

        protected Camera targetCam;

        protected SpoutSenderTexture senderTexture;
		protected ResizableRenderTexture temporaryTexture;
        protected Material _fixupMaterial;

        public bool clearAlpha {
            get { return _clearAlpha; }
            set { _clearAlpha = value; }
        }

        public SpoutSenderTexture.Data Data { get { return data; } set { data = value; } }

        #region MonoBehaviour functions
        void OnEnable()
        {
            targetCam = GetComponent<Camera>();
            senderTexture = new SpoutSenderTexture();
			temporaryTexture = new ResizableRenderTexture(
				readWrite: RenderTextureReadWrite.sRGB,
				antiAliasing: QualitySettings.antiAliasing);

            EnabledOnEnable.Invoke(enabled);
            EnabledOnDisable.Invoke(!enabled);
        }
        void OnDisable()
        {
            if (senderTexture != null) {
                senderTexture.Dispose();
                senderTexture = null;
            }
			if (temporaryTexture != null) {
				SetTargetTexture(null);
				temporaryTexture.Dispose();
				temporaryTexture = null;
			}
            EnabledOnEnable.Invoke(enabled);
            EnabledOnDisable.Invoke(!enabled);
        }

        void Update()
        {
            senderTexture.Prepare(data);
			temporaryTexture.Size = data.Size;
            SetTargetTexture(temporaryTexture.Texture);
            PluginEntry.Poll();
        }
        
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            var sharedTexture = senderTexture.SharedTexture();
            if (sharedTexture != null)
            {
                // Lazy initialization for the fix-up shader.
                if (_fixupMaterial == null)
                    _fixupMaterial = new Material(Shader.Find("Hidden/Spout/Fixup"));

                // Parameters for the fix-up shader.
                _fixupMaterial.SetFloat("_ClearAlpha", _clearAlpha ? 1 : 0);

                // Apply the fix-up shader.
                var tempRT = RenderTexture.GetTemporary(sharedTexture.width, sharedTexture.height);
                Graphics.Blit(source, tempRT, _fixupMaterial, 0);

                // Copy the result to the shared texture.
                Graphics.CopyTexture(tempRT, sharedTexture);

                // Release temporaries.
                RenderTexture.ReleaseTemporary(tempRT);
            }

        }

        #endregion

        protected virtual void SetTargetTexture(RenderTexture tex) {
            targetCam.targetTexture = tex;
            EventOnUpdateTexture.Invoke(tex);
		}

		[System.Serializable]
        public class BoolEvent : UnityEngine.Events.UnityEvent<bool> { }
        [System.Serializable]
        public class RenderTextureEvent : UnityEngine.Events.UnityEvent<RenderTexture> { }
    }
}
