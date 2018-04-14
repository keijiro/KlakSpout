using UnityEngine;

namespace Klak.Spout {

    public class ResizableRenderTexture : System.IDisposable {
        public const int DEFAULT_ANTIALIASING = 1;

        public event System.Action<RenderTexture> AfterCreateTexture;
        public event System.Action<RenderTexture> BeforeDestroyTexture;

		protected bool valid = false;
		protected RenderTexture tex;

		protected RenderTextureReadWrite readWrite;
		protected RenderTextureFormat format;
		protected TextureWrapMode wrapMode;
		protected FilterMode filterMode;
		protected int antiAliasing;
		protected Vector2Int size;
		protected int depth;

		public ResizableRenderTexture(int depth = 24,
			RenderTextureFormat format = RenderTextureFormat.ARGB32,
			RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
			int antiAliasing = 0,
			FilterMode filterMode = FilterMode.Bilinear,
			TextureWrapMode wrapMode = TextureWrapMode.Clamp) {
			this.depth = depth;
			this.format = format;
			this.readWrite = readWrite;
			this.antiAliasing = ParseAntiAliasing(antiAliasing);
			this.filterMode = FilterMode;
			this.wrapMode = wrapMode;
		}

		#region IDisposable implementation
		public void Dispose() {
			ReleaseTexture();
		}
		#endregion

		#region public
		public Vector2Int Size {
			get { return size; }
			set {
				if (size != value) {
					Invalidate();
					size = value;
				}
			}
		}
		public RenderTexture Texture {
			get {
				Validate();
				return tex;
			}
		}
        public FilterMode FilterMode {
			get { return filterMode; }
			set {
				if (filterMode != value) {
					Invalidate();
					filterMode = value;
				}
			}
		}
        public TextureWrapMode WrapMode {
			get { return wrapMode; }
			set {
				if (wrapMode != value) {
					Invalidate();
					wrapMode = value;
				}
			}
		}
		
        public void Clear(Color color, bool clearDepth = true, bool clearColor = true) {
            var active = RenderTexture.active;
            RenderTexture.active = tex;
            GL.Clear (clearDepth, clearColor, color);
            RenderTexture.active = active;
        }
		#endregion

		#region private
		protected void CreateTexture(int width, int height) {
            ReleaseTexture();

			if (width < 2 || height < 2) {
				Debug.LogFormat("Texture size too small : {0}x{1}", width, height);
				return;
			}

            tex = new RenderTexture (width, height, depth, format, readWrite);
            tex.filterMode = FilterMode;
            tex.wrapMode = WrapMode;
            tex.antiAliasing = antiAliasing;
			Debug.LogFormat("ResizableRenderTexture.Create size={0}x{1}", width, height);
            NotifyAfterCreateTexture ();
        }
        protected void NotifyAfterCreateTexture() {
            if (AfterCreateTexture != null)
                AfterCreateTexture (tex);
        }
        protected void NotifyBeforeDestroyTexture() {
            if (BeforeDestroyTexture != null)
                BeforeDestroyTexture (tex);
        }

        protected void ReleaseTexture() {
            NotifyBeforeDestroyTexture ();
            Release(tex);
            tex = null;
        }
		protected virtual void Invalidate() {
			valid = false;
		}
		protected virtual void Validate() {
			if (!valid) {
				CreateTexture(size.x, size.y);
				valid = CheckValidity();
			}
		}
		protected virtual bool CheckValidity() {
			return tex != null && tex.width == size.x && tex.height == size.y;
		}
		#endregion

		public static void Release(Object obj) {
			if (Application.isPlaying)
				Object.Destroy(obj);
			else
				Object.DestroyImmediate(obj);
		}
		public static int ParseAntiAliasing(int antiAliasing) {
			return (antiAliasing > 0 ? antiAliasing : QualitySettings.antiAliasing);
		}
	}
}
