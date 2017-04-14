// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;
using System.Runtime.InteropServices;

namespace Klak.Spout
{
    public static class PluginEntry
    {
        #region Plugin polling

        static int _lastUpdateFrame = -1;

        public static void Poll()
        {
            if (Time.frameCount != _lastUpdateFrame)
            {
                GL.IssuePluginEvent(GetRenderEventFunc(), 0);
                _lastUpdateFrame = Time.frameCount;
            }
        }

        #endregion

        #region Native plugin interface

        [DllImport("KlakSpout")]
        public static extern System.IntPtr GetRenderEventFunc();

        [DllImport("KlakSpout")]
        public static extern System.IntPtr CreateSender(string name, int width, int height);

        [DllImport("KlakSpout")]
        public static extern System.IntPtr CreateReceiver(string name);

        [DllImport("KlakSpout")]
        public static extern void DestroySharedObject(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        public static extern bool DetectDisconnection(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        public static extern System.IntPtr GetTexturePointer(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        public static extern int GetTextureWidth(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        public static extern int GetTextureHeight(System.IntPtr ptr);

        [DllImport("KlakSpout")]
        public static extern int CountSharedObjects();

        [DllImport("KlakSpout")]
        public static extern System.IntPtr GetSharedObjectName(int index);

        public static string GetSharedObjectNameString(int index)
        {
            var ptr = GetSharedObjectName(index);
            return ptr != System.IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
        }

        [DllImport("KlakSpout")]
        public static extern System.IntPtr SearchSharedObjectName(string keyword);

        public static string SearchSharedObjectNameString(string keyword)
        {
            var ptr = SearchSharedObjectName(keyword);
            return ptr != System.IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
        }

        #endregion
    }
}
