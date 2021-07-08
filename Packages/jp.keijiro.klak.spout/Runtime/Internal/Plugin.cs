using System.Runtime.InteropServices;
using IntPtr = System.IntPtr;

namespace Klak.Spout {

static class Plugin
{
    // Receiver interop data structure
    // Should match with KlakSpout::Receiver::InteropData (Receiver.h)
    [StructLayout(LayoutKind.Sequential)]
    public struct ReceiverData
    {
        public uint width, height;
        public IntPtr texturePointer;
    }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

    [DllImport("KlakSpout")]
    public static extern IntPtr GetRenderEventCallback();

    [DllImport("KlakSpout")]
    public static extern IntPtr CreateSender(string name, int width, int height);

    [DllImport("KlakSpout")]
    public static extern IntPtr CreateReceiver(string name);

    [DllImport("KlakSpout")]
    public static extern ReceiverData GetReceiverData(IntPtr receiver);

    [DllImport("KlakSpout")]
    public static extern void GetSenderNames
      ([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
       out IntPtr[] names, out int count);

#else

    public static IntPtr GetRenderEventCallback()
      => IntPtr.Zero;

    public static IntPtr CreateSender(string name, int width, int height)
      => IntPtr.Zero;

    public static IntPtr CreateReceiver(string name)
      => IntPtr.Zero;

    public static ReceiverData GetReceiverData(IntPtr receiver)
      => new ReceiverData();

    public static void GetSenderNames
      ([Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
       out IntPtr[] names, out int count)
    {
        names = null;
        count = 0;
    }

#endif
}

} // namespace Klak.Spout
