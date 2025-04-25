using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomInfo : MonoBehaviour
{
    [HideInInspector]
    public int distanceFromStart = 0;

    public bool isEnemySpawnAllowed = true;

    public List<GameObject> itemSpawnPoints = new List<GameObject>();

    private void Awake()
    {
        itemSpawnPoints = GetComponentsInChildren<ItemSpawnPoint>().Select(x => x.gameObject).ToList();
    }

}