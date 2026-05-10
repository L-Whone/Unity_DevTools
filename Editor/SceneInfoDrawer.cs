#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneInfo))]
public class SceneEntryDrawer : PropertyDrawer
{
    // cached per property path since this drawer is shared across all SceneInfo instances
    private Dictionary<string, ReorderableList> reorderableLists = new Dictionary<string, ReorderableList>();

    private ReorderableList GetList(SerializedProperty prefabsProperty)
    {
        string key = prefabsProperty.propertyPath;

        if (!reorderableLists.TryGetValue(key, out ReorderableList list))
        {
            list = new ReorderableList(prefabsProperty.serializedObject, prefabsProperty, true, true, true, true);

            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Prefabs To Spawn");
            };

            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = prefabsProperty.GetArrayElementAtIndex(index);
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
                element.objectReferenceValue = EditorGUI.ObjectField(
                    rect,
                    "Element " + index,
                    element.objectReferenceValue,
                    typeof(GameObject),
                    false // lock to assets only, no scene objects
                );
            };

            reorderableLists[key] = list;
        }

        // always keep the list pointing at the current serialized object
        list.serializedProperty = prefabsProperty;
        return list;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // find the property of sceneName
        SerializedProperty sceneNameProperty = property.FindPropertyRelative("sceneName");
        SerializedProperty prefabsProperty = property.FindPropertyRelative("prefabsToSpawn");

        // replace default display name with the sceneName
        string displayName = string.IsNullOrEmpty(sceneNameProperty.stringValue) ? ("Additive Scene " + label.text[label.text.Length - 1]) : sceneNameProperty.stringValue;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float y = position.y;

        EditorGUI.BeginProperty(position, label, property);

        Rect foldoutRect = new Rect(position.x, y, position.width, lineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, displayName, true);
        y += lineHeight + spacing;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // draw sceneName field
            Rect sceneNameRect = new Rect(position.x, y, position.width, lineHeight);
            EditorGUI.PropertyField(sceneNameRect, sceneNameProperty);
            y += lineHeight + spacing;

            // draw prefabsToSpawn reorderable list
            ReorderableList list = GetList(prefabsProperty);
            float indentOffset = EditorGUI.indentLevel * 15f;
            Rect listRect = new Rect(position.x + indentOffset, y, position.width - indentOffset, list.GetHeight());
            list.DoList(listRect);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        if (!property.isExpanded)
            return lineHeight;

        SerializedProperty prefabsProperty = property.FindPropertyRelative("prefabsToSpawn");
        ReorderableList list = GetList(prefabsProperty);

        float height = lineHeight + spacing; // foldout header
        height += lineHeight + spacing;      // sceneName
        height += list.GetHeight();          // reorderable list

        return height;
    }
}
#endif