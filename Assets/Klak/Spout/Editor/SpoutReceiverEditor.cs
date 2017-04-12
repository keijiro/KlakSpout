// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;
using UnityEditor;

namespace Klak.Spout
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpoutReceiver))]
    public class SpoutReceiverEditor : Editor
    {
        SerializedProperty _nameFilter;
        SerializedProperty _targetTexture;
        SerializedProperty _targetRenderer;
        SerializedProperty _targetMaterialProperty;

        static GUIContent _labelMaterialProperty = new GUIContent("Material Property");

        void OnEnable()
        {
            _nameFilter = serializedObject.FindProperty("_nameFilter");
            _targetTexture = serializedObject.FindProperty("_targetTexture");
            _targetRenderer = serializedObject.FindProperty("_targetRenderer");
            _targetMaterialProperty = serializedObject.FindProperty("_targetMaterialProperty");
        }

        public override void OnInspectorGUI()
        {
            // Show the editor controls.
            serializedObject.Update();

            EditorGUILayout.PropertyField(_nameFilter);
            EditorGUILayout.PropertyField(_targetTexture);
            EditorGUILayout.PropertyField(_targetRenderer);

            if (_targetRenderer.hasMultipleDifferentValues ||
                _targetRenderer.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_targetMaterialProperty, _labelMaterialProperty);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
