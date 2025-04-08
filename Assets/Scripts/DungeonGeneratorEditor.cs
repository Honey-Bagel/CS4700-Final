using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DungeonGenerator generator = (DungeonGenerator)target;

        if(!Application.isPlaying) {
            EditorGUILayout.HelpBox("You must be in Play mode to regenerate the level.", MessageType.Info);
        } else {
            if(GUILayout.Button("Re-generate Level")) {
                generator.ClearAndGenerateNewLevel();
            }
        }
        
    }
}