using UnityEditor;
using UnityEngine;

//https://stackoverflow.com/questions/78964086/how-to-display-a-private-field-in-the-unity-inspector-as-read-only
public class InspectorUtilities
{
    public class DisplayWithoutEdit : PropertyAttribute
    {
    }

    [CustomPropertyDrawer(typeof(DisplayWithoutEdit))]
    public class DisplayWithoutEditDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
