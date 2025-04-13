using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemFrame : MonoBehaviour {
     
    public InventoryItem_SO itemScriptableObject;
    [SerializeField] Image iconImage;

    void Update()
    {
        iconImage.sprite = itemScriptableObject.sprite;
    }
}