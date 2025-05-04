using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private GameManager gameManager;

    private void OnEnable()
    {
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        // Only show runtime values during play mode
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Values", EditorStyles.boldLabel);
            
            GUI.enabled = false; // Make these read-only
            EditorGUILayout.IntField("Current Level", GameManager.Instance.CurrentLevel);
            EditorGUILayout.IntField("Death Count", GameManager.Instance.DeathCount);
            EditorGUILayout.FloatField("Total Playtime (minutes)", GameManager.Instance.TotalPlaytimeMinutes);
            EditorGUILayout.IntField("Scrap Count", GameManager.Instance.TargetScrapCount);
            GUI.enabled = true;
        }
    }
}