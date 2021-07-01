using UnityEngine;
using UnityEngine.Rendering;
using RTID = UnityEngine.Rendering.RenderTargetIdentifier;

namespace Klak.Spout {

static class RendererOverride
{
    static MaterialPropertyBlock _block;

    public static void SetTexture
      (Renderer renderer, string property, Texture texture)
    {
        if (_block == null) _block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_block);
        _block.SetTexture(property, texture);
        renderer.SetPropertyBlock(_block);
    }
}

static class Blitter
{
    public static void Blit
      (SpoutResources resrc, Texture src, RenderTexture dst, bool alpha)
      => Graphics.Blit(src, dst, GetMaterial(resrc), alpha ? 0 : 1);

    public static void BlitVFlip
      (SpoutResources resrc, Texture src, RenderTexture dst, bool alpha)
      => Graphics.Blit(src, dst, GetMaterial(resrc), alpha ? 2 : 3);

    public static void Blit
      (SpoutResources resrc, CommandBuffer cb, RTID src, RTID dst, bool alpha)
      => cb.Blit(src, dst, GetMaterial(resrc), alpha ? 0 : 1);

    public static void BlitFromSrgb
      (SpoutResources resrc, Texture src, RenderTexture dst)
      => Graphics.Blit(src, dst, GetMaterial(resrc), 4);

    static Material _material;

    static Material GetMaterial(SpoutResources resrc)
    {
        if (_material == null)
        {
            _material = new Material(resrc.blitShader);
            _material.hideFlags = HideFlags.DontSave;
        }
        return _material;
    }
}

static class Utility
{
    public static void Destroy(Object obj)
    {
        if (obj == null) return;

        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }
}

} // namespace Klak.Spout
