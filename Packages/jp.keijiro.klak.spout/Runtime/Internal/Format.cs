using UnityEngine;

namespace Klak.Spout {

// Texture format enumeration
// Should match with KlakSpout::Format (Receiver.h)
public enum Format : int
  { Unknown, RGBA32, RGBA32_SRGB, BGRA32, BGRA32_SRGB, RGBAHalf, RGBAFloat }

// Helper methods for Format enum
static class FormatUtil
{
    public static TextureFormat ToTextureFormat(this Format format)
    {
        switch (format)
        {
            case Format.RGBA32:
            case Format.RGBA32_SRGB:
                return TextureFormat.RGBA32;

            case Format.BGRA32:
            case Format.BGRA32_SRGB:
                return TextureFormat.BGRA32;

            case Format.RGBAHalf:
                return TextureFormat.RGBAHalf;

            case Format.RGBAFloat:
                return TextureFormat.RGBAFloat;

            default:
                Debug.LogError("Unknown texture format");
                return TextureFormat.RGBA32;
        }
    }

    public static bool IsSRGB(this Format format)
    {
        return format == Format.RGBA32_SRGB || format == Format.BGRA32_SRGB;
    }
}

} // namespace Klak.Spout
