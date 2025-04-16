using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemFrame : MonoBehaviour {
     
    public InventorySlot slot;
    [SerializeField] Image iconImage;

    void Update()
    {
        if (slot.SOReference == null) return;
        iconImage.sprite = slot.SOReference.sprite;
    }
}