using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemFrame : MonoBehaviour {
     
    public InventorySlot slot;
    [SerializeField] Image iconImage;
    public bool isSelected;
    private Image selfImage;

    void Start()
    {
     selfImage = gameObject.GetComponent<Image>();   
    }

    void Update()
    {
        //color if selected
        if (isSelected) selfImage.color = Color.red;
        else selfImage.color = new Color(1,0.73f,0.73f);

        if (slot == null) return;
        if (slot.SOReference == null) 
        {
            iconImage.sprite = null; //make it empty
            return;
        }

        iconImage.sprite = slot.SOReference.sprite;
    }
}