using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This is an interface for all physical items that can be picked up by the player and placed into the inventory
*/
public abstract class PickableItem : ScriptableObject
{
    public GameObject objectPrefab;
    public string objectName;
    public Sprite sprite; // for in inventory ig?

    public abstract PickableItem Collect();
}
