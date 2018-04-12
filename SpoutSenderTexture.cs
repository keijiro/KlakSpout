using System.Collections.Generic;
using UnityEngine;

namespace Klak.Spout {

    public class SpoutSenderTexture : System.IDisposable {
        protected bool currValidity = false;
        protected Data curr;

        protected System.IntPtr _sender = System.IntPtr.Zero;
        protected Texture2D _sharedTexture = null;
        protected RenderTexture temporaryTexture = null;

        public SpoutSenderTexture() {
            Invalidate();
        }

        #region IDisposable
        public virtual void Dispose() {
            ClearSender();
            ClearSharedTexture();
			ClearTemporaryTexture();
		}
        #endregion
        
        public static bool InputValidity(Data data) {
            return
                !string.IsNullOrEmpty(data.name)
                && data.width >= 4
                && data.height >= 4;
        }
        public virtual void Invalidate() {
            currValidity = false;
            curr = default(Data);
        }
        public virtual void Prepare(Data next) {
            if (!currValidity || !curr.Equals(next)) {
                ClearSender();
                ClearSharedTexture();
                ClearTemporaryTexture();
                Invalidate();
                if (InputValidity(next) && TryBuildSender(next, out _sender)) {
                    currValidity = true;
                    curr = next.Clone();
                }
            }
        }
        public virtual RenderTexture GetTemporary() {
            if (currValidity && temporaryTexture == null) {
                temporaryTexture = RenderTexture.GetTemporary(curr.width, curr.height);
				temporaryTexture.antiAliasing = QualitySettings.antiAliasing;
				temporaryTexture.useMipMap = false;
            }
            return temporaryTexture;
        }
        public virtual Texture2D SharedTexture() {
            if (currValidity && _sharedTexture == null)
                TryBuildTexture(_sender, out _sharedTexture);
            return _sharedTexture;
        }

        public static bool TryBuildSender(Data next, out System.IntPtr sender) {
            sender = PluginEntry.CreateSender(next.name, next.width, next.height);
            return sender != System.IntPtr.Zero;
        }
        public static bool TryBuildTexture(System.IntPtr sender, out Texture2D sharedTexture) {
            sharedTexture = null;

            var ptr = PluginEntry.GetTexturePointer(sender);
            if (ptr == System.IntPtr.Zero)
                return false;

            var width = PluginEntry.GetTextureWidth(sender);
            var height = PluginEntry.GetTextureHeight(sender);
            sharedTexture = Texture2D.CreateExternalTexture(width, height, 
                TextureFormat.ARGB32, false, false, ptr);
            Debug.LogFormat("Build Texture ({0}x{1})", width, height);
            return true;
        }
        protected virtual void ClearTemporaryTexture() {
            if(temporaryTexture != null) {
                RenderTexture.ReleaseTemporary(temporaryTexture);
                temporaryTexture = null;
            }
        }
        protected virtual void ClearSharedTexture() {
            if (_sharedTexture != null) {
                DestroyObject(_sharedTexture);
                _sharedTexture = null;
            }
        }
        protected virtual void ClearSender() {
            if (_sender != System.IntPtr.Zero) {
                PluginEntry.DestroySharedObject(_sender);
                _sender = System.IntPtr.Zero;
            }
        }

        public static void DestroyObject(Object obj) {
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        #region Classes
        [System.Serializable]
        public struct Data {
            public string name;
            public int width;
            public int height;
            

            public override bool Equals(object obj) {
                var b = (Data)obj;
                var result = name == b.name
                    && width == b.width
                    && height == b.height;
                return result;
            }
            public override string ToString() {
                return string.Format("Data <name={0} size=({1}x{2})", name, width, height);
            }
            public override int GetHashCode() {
                var hashCode = -1072973697;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
                hashCode = hashCode * -1521134295 + width.GetHashCode();
                hashCode = hashCode * -1521134295 + height.GetHashCode();
                return hashCode;
            }

            public Data Clone() {
                return new Data() { name = this.name, width = this.width, height = this.height };
            }
        }
        #endregion
    }
}
