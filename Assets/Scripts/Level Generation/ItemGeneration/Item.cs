using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public GameObject itemPrefab;
    public float spawnWeight = 1f;
    public int basePrice;
}