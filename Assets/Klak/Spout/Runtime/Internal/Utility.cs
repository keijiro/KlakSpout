// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;

namespace Klak.Spout
{
    // Internal utilities
    static class Util
    {
        internal static void Destroy(Object obj)
        {
            if (obj != null)
                if (Application.isPlaying)
                    Object.Destroy(obj);
                else
                    Object.DestroyImmediate(obj);
        }
    }
}
