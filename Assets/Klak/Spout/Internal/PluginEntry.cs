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
        public static extern int CreateSender(string name, int width, int height);

        [DllImport("KlakSpout")]
        public static extern int CreateReceiver(string name);

        [DllImport("KlakSpout")]
        public static extern void Destroy(int id);

        [DllImport("KlakSpout")]
        public static extern int GetTextureWidth(int id);

        [DllImport("KlakSpout")]
        public static extern int GetTextureHeight(int id);

        [DllImport("KlakSpout")]
        public static extern System.IntPtr GetTexturePtr(int id);

        [DllImport("KlakSpout")]
        public static extern int CountSharedTextures();

        [DllImport("KlakSpout")]
        public static extern System.IntPtr GetSharedTextureName(int index);

        public static string GetSharedTextureNameString(int index)
        {
            var ptr = GetSharedTextureName(index);
            return ptr != System.IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
        }

        [DllImport("KlakSpout")]
        public static extern System.IntPtr SearchSharedTextureName(string keyword);

        public static string SearchSharedTextureNameString(string keyword)
        {
            var ptr = SearchSharedTextureName(keyword);
            return ptr != System.IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
        }

        #endregion
    }
}
