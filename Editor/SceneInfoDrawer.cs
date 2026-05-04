#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneInfo))]
public class SceneEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // find the property of sceneName
        SerializedProperty sceneNameProperty = property.FindPropertyRelative("sceneName");

        // replace default display name with the sceneName
        string displayName = string.IsNullOrEmpty(sceneNameProperty.stringValue) ? ("Additive Scene " + label.text[label.text.Length-1])  : sceneNameProperty.stringValue;

        EditorGUI.PropertyField(position, property, new GUIContent(displayName), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }
}
#endif