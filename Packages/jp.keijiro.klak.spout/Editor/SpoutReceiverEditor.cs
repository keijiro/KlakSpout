using UnityEngine;
using UnityEditor;

namespace Klak.Spout.Editor {

[CanEditMultipleObjects]
[CustomEditor(typeof(SpoutReceiver))]
sealed class SpoutReceiverEditor : UnityEditor.Editor
{
    SerializedProperty _sourceName;
    SerializedProperty _targetTexture;
    SerializedProperty _targetRenderer;
    SerializedProperty _targetMaterialProperty;

    static class Labels
    {
        public static Label Property = "Property";
        public static Label Select = "Select";
    }

    // Create and show the source name dropdown.
    void ShowSourceNameDropdown(Rect rect)
    {
        var menu = new GenericMenu();
        var sources = SpoutManager.GetSourceNames();

        if (sources.Length > 0)
        {
            foreach (var name in sources)
                menu.AddItem(new GUIContent(name), false, OnSelectSource, name);
        }
        else
        {
            menu.AddItem(new GUIContent("No source available"), false, null);
        }

        menu.DropDown(rect);
    }

    // Source name selection callback
    void OnSelectSource(object nameObject)
    {
        var name = (string)nameObject;
        serializedObject.Update();
        _sourceName.stringValue = name;
        serializedObject.ApplyModifiedProperties();
        RequestRestart();
    }

    // Receiver restart request
    void RequestRestart()
    {
        // Dirty trick: We only can restart receivers by modifying the
        // sourceName property, so we modify it by an invalid name, then
        // revert it.
        foreach (SpoutReceiver recv in targets)
        {
            recv.sourceName = "";
            recv.sourceName = _sourceName.stringValue;
        }
    }

    void OnEnable()
    {
        var finder = new PropertyFinder(serializedObject);
        _sourceName = finder["_sourceName"];
        _targetTexture = finder["_targetTexture"];
        _targetRenderer = finder["_targetRenderer"];
        _targetMaterialProperty = finder["_targetMaterialProperty"];
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginHorizontal();

        // Source name text field
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.DelayedTextField(_sourceName);
        var restart = EditorGUI.EndChangeCheck();

        // Source name dropdown
        var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(60));
        if (EditorGUI.DropdownButton(rect, Labels.Select, FocusType.Keyboard))
            ShowSourceNameDropdown(rect);

        EditorGUILayout.EndHorizontal();

        // Target texture/renderer
        EditorGUILayout.PropertyField(_targetTexture);
        EditorGUILayout.PropertyField(_targetRenderer);

        EditorGUI.indentLevel++;

        if (_targetRenderer.hasMultipleDifferentValues)
        {
            // Multiple renderers selected: Show a simple text field.
            EditorGUILayout.PropertyField(_targetMaterialProperty, Labels.Property);
        }
        else if (_targetRenderer.objectReferenceValue != null)
        {
            // Single renderer: Show the material property selection dropdown.
            MaterialPropertySelector.DropdownList(_targetRenderer, _targetMaterialProperty);
        }

        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();

        if (restart) RequestRestart();
    }
}

} // namespace Klak.Spout.Editor
