using UnityEngine.LowLevel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Klak.Spout {

//
// "Memory pool" class without actual memory pool functionality
// At the moment, it only provides the delayed destruction method.
//
static class MemoryPool
{
    #region Public method

    public static void FreeOnEndOfFrame(GCHandle gch)
      => _toBeFreed.Push(gch);

    #endregion

    #region Delayed destruction

    static Stack<GCHandle> _toBeFreed = new Stack<GCHandle>();

    static void OnEndOfFrame()
    {
        while (_toBeFreed.Count > 0) _toBeFreed.Pop().Free();
    }

    #endregion

    #region PlayerLoopSystem implementation

    static MemoryPool()
    {
        InsertPlayerLoopSystem();

    #if UNITY_EDITOR
        // We use not only PlayerLoopSystem but also the
        // EditorApplication.update callback because the PlayerLoop events are
        // not invoked in the edit mode.
        UnityEditor.EditorApplication.update += OnEndOfFrame;
    #endif
    }

    static void InsertPlayerLoopSystem()
    {
        var customSystem = new PlayerLoopSystem()
          { type = typeof(MemoryPool), updateDelegate = OnEndOfFrame };

        var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

        for (var i = 0; i < playerLoop.subSystemList.Length; i++)
        {
            ref var phase = ref playerLoop.subSystemList[i];
            if (phase.type == typeof(UnityEngine.PlayerLoop.PostLateUpdate))
            {
                phase.subSystemList = phase.subSystemList
                  .Concat(new [] { customSystem }).ToArray();
                break;
            }
        }

        PlayerLoop.SetPlayerLoop(playerLoop);
    }

    #endregion
}

} // namespace Klak.Spout
