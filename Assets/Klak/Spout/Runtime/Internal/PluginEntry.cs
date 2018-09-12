// KlakSpout - Spout video frame sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;
using System.Runtime.InteropServices;

namespace Klak.Spout
{
    static class PluginEntry
    {
        #region Plugin polling

        static int _lastUpdateFrame = -1;

        internal static void Poll()
        {
            if (Time.frameCount != _lastUpdateFrame || !Application.isPlaying)
            {
                GL.IssuePluginEvent(GetRenderEventFunc(), 0);
                _lastUpdateFrame = Time.frameCount;
            }
        }

        #endregion

        #region Native plugin interface

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr GetRenderEventFunc();

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr CreateSender(string name, int width, int height);

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr TryCreateReceiver(string name);

        [DllImport("KlakSpout")]
        internal static extern void DestroySharedObject(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        internal static extern bool DetectDisconnection(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr GetTexturePointer(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        internal static extern int GetTextureWidth(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        internal static extern int GetTextureHeight(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        internal static extern int ScanSharedObjects();

        [DllImport("KlakSpout")]
        internal static extern System.IntPtr GetSharedObjectName(int index);

        internal static string GetSharedObjectNameString(int index)
        {
            var ptr = GetSharedObjectName(index);
            return ptr != System.IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
        }

        #endregion
    }
}
