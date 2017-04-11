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
            return Marshal.PtrToStringAnsi(GetSharedTextureName(index));
        }

        #endregion
    }
}
