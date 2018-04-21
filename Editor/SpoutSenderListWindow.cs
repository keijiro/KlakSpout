// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;
using UnityEditor;

namespace Klak.Spout
{
    // Spout sender list window
    public class SpoutSenderListWindow : EditorWindow
    {
        [MenuItem("Window/Klak/Spout Sender List")]
        static void Init()
        {
            EditorWindow.GetWindow<SpoutSenderListWindow>("Spout Senders").Show();
        }

        int _updateCount;

        void OnInspectorUpdate()
        {
            // Update once per eight calls.
            if ((_updateCount++ & 7) == 0) Repaint();
        }

        void OnGUI()
        {
            var count = PluginEntry.CountSharedObjects();

            EditorGUILayout.Space();
            EditorGUI.indentLevel++;

            if (count == 0)
                EditorGUILayout.LabelField("No sender detected.");
            else
                EditorGUILayout.LabelField(count + " sender(s) detected.");

            for (var i = 0; i < count; i++)
            {
                var name = PluginEntry.GetSharedObjectNameString(i);
                if (name != null) EditorGUILayout.LabelField("- " + name);
            }

            EditorGUI.indentLevel--;
        }
    }
}
