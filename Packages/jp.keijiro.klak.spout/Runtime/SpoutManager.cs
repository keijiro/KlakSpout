using System.Runtime.InteropServices;
using IntPtr = System.IntPtr;

namespace Klak.Spout {

public static class SpoutManager
{
    //
    // GetSourceNames - Enumerates names of all available Spout sources
    //
    // This method invokes GC memory allocations every time, so it's
    // recommended to cache the results for frequent use.
    //
    public static string[] GetSourceNames()
    {
        // Retrieve an array of string pointers from the plugin.
        IntPtr[] pointers;
        int count;
        Plugin.GetSenderNames(out pointers, out count);

        // Convert them into managed strings.
        var names = new string[count];
        for (var i = 0; i < count; i++)
        {
            names[i] = Marshal.PtrToStringAnsi(pointers[i]);
            Marshal.FreeCoTaskMem(pointers[i]);
        }

        return names;
    }
}

} // namespace Klak.Spout
