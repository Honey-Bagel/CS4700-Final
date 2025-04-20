using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Room
{
    [Tooltip("Room prefab.")]
    public GameObject roomPrefab;
    [Range(0f, 1f), Tooltip("Weight of the room. (Chance for room to be picked over others)")]
    public float weight = 1.0f;
    //public AnimationCurve depthWeightScale = AnimationCurve.Linear(0, 1, 1, 1); // for variable weight based on distance from start room
}

public class RoomInfo : MonoBehaviour
{
    [HideInInspector]
    public int distanceFromStart = 0;

    public bool isEnemySpawnAllowed = true;
}