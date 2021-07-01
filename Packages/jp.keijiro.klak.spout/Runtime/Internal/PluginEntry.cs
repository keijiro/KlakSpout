// KlakSpout - Spout video frame sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;
using System.Runtime.InteropServices;

namespace Klak.Spout
{
    static class PluginEntry
    {
        internal enum Event { Dispose, SetTargetObject, UpdateWrapCache }
        internal enum BlitCommand { Send = 2, Receive = 3 }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SenderInfo
        {
            public int width;
            public int height;
            public int format;
            public bool exists;
            public bool isSameSize;
        }

        internal enum DXTextureFormat
        {
            DXGI_FORMAT_UNKNOWN = 0,
            DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
            D3DFMT_A8R8G8B8 = 21, // DX9 shared tex, interpret as DXGI_FORMAT_B8G8R8A8_UNORM
            DXGI_FORMAT_R10G10B10A2_UNORM = 24,
            DXGI_FORMAT_R8G8B8A8_UNORM = 28,
            DXGI_FORMAT_B8G8R8A8_UNORM = 87, // Default DX11 shared tex
        }

        // Tedious, but we only need to support a few formats
        // (the rest aren't compatible with the DX sharing feature)
        internal static bool DXToRenderTextureFormat(DXTextureFormat dxFormat, ref RenderTextureFormat rtFormat)
        {
            if (dxFormat == DXTextureFormat.DXGI_FORMAT_UNKNOWN ||
                dxFormat == DXTextureFormat.D3DFMT_A8R8G8B8 ||
                dxFormat == DXTextureFormat.DXGI_FORMAT_B8G8R8A8_UNORM)
            {
                rtFormat = RenderTextureFormat.BGRA32;
                return true;
            }
            else if (dxFormat == DXTextureFormat.DXGI_FORMAT_R16G16B16A16_FLOAT)
            {
                rtFormat = RenderTextureFormat.ARGBHalf;
                return true;
            }
            else if (dxFormat == DXTextureFormat.DXGI_FORMAT_R10G10B10A2_UNORM)
            {
                rtFormat = RenderTextureFormat.ARGB2101010;
                return true;
            }
            else if (dxFormat == DXTextureFormat.DXGI_FORMAT_R8G8B8A8_UNORM)
            {
                rtFormat = RenderTextureFormat.ARGB32;
                return true;
            }

            return false;
        }

        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR_OSX

        internal static bool IsAvailable {
            get {
                return
                    SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 ||
                    SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12;
            }
        }

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr GetRenderEventFunc();

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr GetCustomBlitFunc();

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr CreateSender(string name, int width, int height);

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr CreateReceiver(string name);

        [DllImport("KlakSpout")]
        internal static extern int GetTextureWidth(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        internal static extern int GetTextureHeight(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        internal static extern int GetTextureFormat(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        internal static extern bool IsReady(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr GetSenderInfo(string name, out SenderInfo info);

        [DllImport("KlakSpout")]
        internal static extern int ScanSharedObjects();

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr GetSharedObjectName(int index);

        internal static string GetSharedObjectNameString(int index)
        {
            var ptr = GetSharedObjectName(index);
            return ptr != System.IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
        }

        #else

        internal static bool IsAvailable { get { return false; } }

        internal static System.IntPtr GetRenderEventFunc()
        { return System.IntPtr.Zero; }

        internal static System.IntPtr GetCustomBlitFunc()
        { return System.IntPtr.Zero; }

        internal static System.IntPtr CreateSender(string name, int width, int height)
        { return System.IntPtr.Zero; }

        internal static System.IntPtr CreateReceiver(string name)
        { return System.IntPtr.Zero; }

        internal static int GetTextureWidth(System.IntPtr ptr)
        { return 0; }

        internal static int GetTextureHeight(System.IntPtr ptr)
        { return 0; }

        internal static int GetTextureFormat(System.IntPtr ptr)
        { return 0; }

        internal static bool IsReady(System.IntPtr ptr)
        { return false; }

        internal static System.IntPtr GetSenderInfo(string name, out SenderInfo info)
        { return System.IntPtr.Zero; }

        internal static int ScanSharedObjects()
        { return 0; }

        internal static string GetSharedObjectNameString(int index)
        { return null; }

        #endif
    }
}
