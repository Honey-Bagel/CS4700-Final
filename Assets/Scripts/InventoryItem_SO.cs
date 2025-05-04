using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryObject", menuName = "Scriptable Objects/InventoryObject")]

public class InventoryItem_SO : ScriptableObject {

    public GameObject objectPrefab;
    public string objectName;
    public Sprite sprite; // for in inventory
    public int objectPrice;

    public float weight;

    public Vector3 holdVector3;
    public Quaternion holdQuaternion;
}