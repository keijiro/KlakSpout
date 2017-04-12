// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;
using UnityEditor;

namespace Klak.Spout
{
    // Spout sender list window
    public class SenderNameListWindow : EditorWindow
    {
        [MenuItem("Window/Klak/Spout Sender List")]
        static void Init()
        {
            EditorWindow.GetWindow<SenderNameListWindow>("Spout Senders").Show();
        }

        void OnGUI()
        {
            var count = PluginEntry.CountSharedTextures();

            EditorGUILayout.Space();
            EditorGUI.indentLevel++;

            if (count == 0)
                EditorGUILayout.LabelField("No sender detected.");
            else
                EditorGUILayout.LabelField(count + " sender(s) detected.");

            for (var i = 0; i < count; i++)
            {
                var name = PluginEntry.GetSharedTextureNameString(i);
                if (name != null) EditorGUILayout.SelectableLabel(name);
            }

            EditorGUI.indentLevel--;
        }
    }
}
