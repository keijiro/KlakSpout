// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

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

        static GUIContent _labelProperty = new GUIContent("Property");

        string[] _propertyList; // cached property list
        Shader _cachedShader;   // shader used to cache the list

        // Retrieve shader from a target renderer.
        Shader RetrieveTargetShader(UnityEngine.Object target)
        {
            var renderer = target as Renderer;
            if (renderer == null) return null;

            var material = renderer.sharedMaterial;
            if (material == null) return null;

            return material.shader;
        }

        // Cache properties of a given shader if it's
        // different from a previously given one.
        void CachePropertyList(Shader shader)
        {
            if (_cachedShader == shader) return;

            var temp = new List<string>();

            var count = ShaderUtil.GetPropertyCount(shader);
            for (var i = 0; i < count; i++)
            {
                var propType = ShaderUtil.GetPropertyType(shader, i);
                if (propType == ShaderUtil.ShaderPropertyType.TexEnv)
                    temp.Add(ShaderUtil.GetPropertyName(shader, i));
            }

            _propertyList = temp.ToArray();
            _cachedShader = shader;
        }

        // Material property drop-down list.
        void ShowMaterialPropertyDropDown()
        {
            // Try to retrieve the target shader.
            var shader = RetrieveTargetShader(_targetRenderer.objectReferenceValue);

            if (shader != null)
            {
                // Cache the property list of the target shader.
                CachePropertyList(shader);

                // If there are suitable candidates...
                if (_propertyList.Length > 0)
                {
                    // Show the drop-down list.
                    var index = Array.IndexOf(_propertyList, _targetMaterialProperty.stringValue);
                    var newIndex = EditorGUILayout.Popup("Property", index, _propertyList);

                    // Update the property if the selection was changed.
                    if (index != newIndex)
                        _targetMaterialProperty.stringValue = _propertyList[newIndex];
                }
                else
                    _targetMaterialProperty.stringValue = ""; // reset on failure
            }
            else
                _targetMaterialProperty.stringValue = ""; // reset on failure
        }

        void OnEnable()
        {
            _nameFilter = serializedObject.FindProperty("_nameFilter");
            _targetTexture = serializedObject.FindProperty("_targetTexture");
            _targetRenderer = serializedObject.FindProperty("_targetRenderer");
            _targetMaterialProperty = serializedObject.FindProperty("_targetMaterialProperty");
        }

        void OnDisable()
        {
            _propertyList = null;
            _cachedShader = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_nameFilter);
            EditorGUILayout.PropertyField(_targetTexture);
            EditorGUILayout.PropertyField(_targetRenderer);

            EditorGUI.indentLevel++;

            if (_targetRenderer.hasMultipleDifferentValues)
            {
                // Show a simple text field if there are multiple values.
                EditorGUILayout.PropertyField(_targetMaterialProperty, _labelProperty);
            }
            else if (_targetRenderer.objectReferenceValue != null)
            {
                // Show the material property drop-down list.
                ShowMaterialPropertyDropDown();
            }

            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
