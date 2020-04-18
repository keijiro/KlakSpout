// KlakSpout - Spout video frame sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;
using System.Collections.Generic;

namespace Klak.Spout
{
    public static class SpoutManager
    {
        // Scan available Spout sources and return their names via a newly
        // allocated string array.
        public static string[] GetSourceNames()
        {
            var count = PluginEntry.ScanSharedObjects();
            var names = new string [count];
            for (var i = 0; i < count; i++)
                names[i] = PluginEntry.GetSharedObjectNameString(i);
            return names;
        }

        // Scan available Spout sources and store their names into the given
        // collection object.
        public static void GetSourceNames(ICollection<string> store)
        {
            store.Clear();
            var count = PluginEntry.ScanSharedObjects();
            for (var i = 0; i < count; i++)
                store.Add(PluginEntry.GetSharedObjectNameString(i));
        }
    }
}
