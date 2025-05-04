using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true)]
public class SaveableObjectEditor : Editor
{
    SerializedProperty uniqueIdProperty;

    private void OnEnable()
    {
        if(target is ISaveable)
        {
            uniqueIdProperty = serializedObject.FindProperty("uniqueID");
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if(uniqueIdProperty != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Save System", EditorStyles.boldLabel);

            if(GUILayout.Button("Generate New ID")) {
                uniqueIdProperty.stringValue = System.Guid.NewGuid().ToString();
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}