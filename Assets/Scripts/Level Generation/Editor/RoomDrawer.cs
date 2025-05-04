using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Room))]
public class RoomDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the index from the property path
        string path = property.propertyPath;
        int index = -1;
        
        // Extract the index number from the property path
        if (path.Contains("["))
        {
            string indexStr = path.Substring(path.IndexOf("[") + 1, path.IndexOf("]") - path.IndexOf("[") - 1);
            int.TryParse(indexStr, out index);
        }
        
        // Modify the label if we found a valid index
        if (index >= 0)
        {
            // Get the room prefab name
            SerializedProperty prefabProp = property.FindPropertyRelative("roomPrefab");
            GameObject prefab = (GameObject)prefabProp.objectReferenceValue;
            
            // Create a custom label
            string customLabel;
            if (prefab != null)
            {
                customLabel = $"{prefab.name}";
            }
            else
            {
                customLabel = $"(No Prefab)";
            }
            
            label.text = customLabel;
        }
        
        // Draw the default property field with our custom label
        EditorGUI.PropertyField(position, property, label, true);
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}