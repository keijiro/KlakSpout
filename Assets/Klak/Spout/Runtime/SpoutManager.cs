// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;
using System.Collections.Generic;

namespace Klak.Spout
{
    public static class SpoutManager
    {
        // Scan available sources and return their names with a string array.
        public static string[] GetSourceNames()
        {
            var count = Klak.Spout.PluginEntry.ScanSharedObjects();
            var names = new string [count];
            for (var i = 0; i < count; i++)
                names[i] = PluginEntry.GetSharedObjectNameString(i);
            return names;
        }

        // Scan available sources and store their names into a given collection object.
        public static void GetSourceNames(ICollection<string> dest)
        {
            dest.Clear();
            var count = Klak.Spout.PluginEntry.ScanSharedObjects();
            for (var i = 0; i < count; i++)
                dest.Add(PluginEntry.GetSharedObjectNameString(i));
        }
    }
}
