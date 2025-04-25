using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Generator))]
public class GeneratorEditor : Editor
{
    private Generator newGenerator;
    private bool showRoomSettings = true;
    private bool showGenerationSettings = true;
    private bool showDebugSettings = true;
    private bool showDoorSettings = true;

    // Properties
    // Generation Props
    private SerializedProperty seedProp;
    private SerializedProperty roomCountProp;
    private SerializedProperty roomParentProp;
    // Room Props
    private SerializedProperty roomLayerMaskProp;
    private SerializedProperty startRoomPrefabProp;
    private SerializedProperty roomListProp;
    // Debug Props
    private SerializedProperty showDebugProp;
    private SerializedProperty showLogsProp;
    private SerializedProperty debugLineDurationProp;
    // Item Props
    private SerializedProperty itemSpawnerProp;
    private SerializedProperty doorPrefabsProp;

    private void OnEnable()
    {
        newGenerator = (Generator)target;

        seedProp = serializedObject.FindProperty("seed");
        roomCountProp = serializedObject.FindProperty("numberOfRooms");
        roomLayerMaskProp = serializedObject.FindProperty("roomLayerMask");
        startRoomPrefabProp = serializedObject.FindProperty("startRoomPrefab");
        roomParentProp = serializedObject.FindProperty("roomParent");
        showDebugProp = serializedObject.FindProperty("showDebug");
        showLogsProp = serializedObject.FindProperty("showLogs");
        debugLineDurationProp = serializedObject.FindProperty("debugLineDuration");
        roomListProp = serializedObject.FindProperty("roomList");
        itemSpawnerProp = serializedObject.FindProperty("itemSpawner");
        doorPrefabsProp = serializedObject.FindProperty("doorPrefabs");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Level Generator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Generate procedural levels using prefab rooms", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        DrawGenerationSettings();
        DrawRoomSettings();
        DrawDebugSettings();

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(itemSpawnerProp);

        EditorGUILayout.Space();

        DrawDoorSettings();

        EditorGUILayout.Space();

        DrawGenerateButtons();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGenerationSettings()
    {
        showGenerationSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGenerationSettings, "Generation Settings");

        if(showGenerationSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(seedProp);
            EditorGUILayout.PropertyField(roomCountProp);
            EditorGUILayout.PropertyField(roomParentProp);

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawRoomSettings()
    {
        showRoomSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRoomSettings, "Room Settings");

        EditorGUILayout.EndFoldoutHeaderGroup();

        if(showRoomSettings) {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(roomLayerMaskProp);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(startRoomPrefabProp);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(roomListProp, new GUIContent("Rooms", "List of rooms to generate in the level."));

            if(roomListProp.arraySize == 0) {
                EditorGUILayout.HelpBox("You need at least one room.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawDebugSettings()
    {
        showDebugSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDebugSettings, "Debug Settings");

        if(showDebugSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(showLogsProp);
            EditorGUILayout.PropertyField(showDebugProp);

            if(showDebugProp.boolValue)
            {
                EditorGUILayout.PropertyField(debugLineDurationProp);
            }

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawDoorSettings()
    {
        showDoorSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDoorSettings, "Door Settings");

        EditorGUILayout.EndFoldoutHeaderGroup();

        if(showDoorSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(doorPrefabsProp);

            EditorGUILayout.Space();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

    }

    private void DrawGenerateButtons()
    {
        if(EditorApplication.isPlaying) {
            if (GUILayout.Button("Re-Generate Level", GUILayout.Height(30))) {
                newGenerator.GenerateDungeon();
                SceneView.RepaintAll();
            }
        } else {
            EditorGUILayout.HelpBox("You must be playing the game to re-generate the level", MessageType.Warning);
        }
    }
}