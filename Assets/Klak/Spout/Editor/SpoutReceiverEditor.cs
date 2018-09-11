// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

using UnityEngine;
using UnityEditor;

namespace Klak.Spout
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpoutReceiver))]
    sealed class SpoutReceiverEditor : Editor
    {
        SerializedProperty _nameFilter;
        SerializedProperty _targetTexture;
        SerializedProperty _targetRenderer;
        SerializedProperty _targetMaterialProperty;

        double _prevRepaintTime;

        static class Labels
        {
            public static readonly GUIContent Property = new GUIContent("Property");
            public static readonly GUIContent Select = new GUIContent("Select");
        }

        // Request receiver reconnection with flip-flopping.
        void RequestReconnect()
        {
            foreach (SpoutReceiver receiver in targets)
            {
                receiver.enabled = false;
                receiver.enabled = true;
            }
        }

        // Check the elapsed time and request repaint with 0.1s interval.
        void CheckRepaint()
        {
            var time = EditorApplication.timeSinceStartup;
            if (time - _prevRepaintTime < 0.1) return;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            _prevRepaintTime = time;
        }

        // Create and show the Spout sender name dropdown.
        void ShowSenderNameDropdown(Rect rect)
        {
            var menu = new GenericMenu();
            var count = PluginEntry.ScanSharedObjects();
            for (var i = 0; i < count; i++)
            {
                var name = PluginEntry.GetSharedObjectNameString(i);
                menu.AddItem(new GUIContent(name), false, OnSelectSenderName, name);
            }
            menu.DropDown(rect);
        }

        // Sender name selection callback: Called from the dropdown.
        void OnSelectSenderName(object name)
        {
            serializedObject.Update();
            _nameFilter.stringValue = (string)name;
            serializedObject.ApplyModifiedProperties();
            RequestReconnect();
        }

        void OnEnable()
        {
            _nameFilter = serializedObject.FindProperty("_nameFilter");
            _targetTexture = serializedObject.FindProperty("_targetTexture");
            _targetRenderer = serializedObject.FindProperty("_targetRenderer");
            _targetMaterialProperty = serializedObject.FindProperty("_targetMaterialProperty");

           EditorApplication.update += CheckRepaint;
        }

        void OnDisable()
        {
            EditorApplication.update -= CheckRepaint;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();

            // Name filter edit box
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedTextField(_nameFilter);
            if (EditorGUI.EndChangeCheck()) RequestReconnect();

            // Name filter dropdown
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(60));
            if (EditorGUI.DropdownButton(rect, Labels.Select, FocusType.Keyboard))
                ShowSenderNameDropdown(rect);

            EditorGUILayout.EndHorizontal();

            // Target texture/renderer
            EditorGUILayout.PropertyField(_targetTexture);
            EditorGUILayout.PropertyField(_targetRenderer);

            EditorGUI.indentLevel++;

            if (_targetRenderer.hasMultipleDifferentValues)
            {
                // Show a simple text field if there are multiple values.
                EditorGUILayout.PropertyField(_targetMaterialProperty, Labels.Property);
            }
            else if (_targetRenderer.objectReferenceValue != null)
            {
                // Show the material property selection dropdown.
                MaterialPropertySelector.DropdownList(_targetRenderer, _targetMaterialProperty);
            }

            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
