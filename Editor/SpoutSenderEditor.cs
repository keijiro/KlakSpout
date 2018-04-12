// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;
using UnityEditor;

namespace Klak.Spout
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpoutSender))]
    public class SpoutSenderEditor : Editor
    {
        SerializedProperty _clearAlpha;

        void OnEnable()
        {
            _clearAlpha = serializedObject.FindProperty("_clearAlpha");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_clearAlpha);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
