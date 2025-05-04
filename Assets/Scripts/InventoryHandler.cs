using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InventoryHandler : MonoBehaviour {

    public static InventoryItemFrame[] inventoryFrames = new InventoryItemFrame[4];
    public static InventorySlot[] inventory = {new InventorySlot(), new InventorySlot(), new InventorySlot(), new InventorySlot()};
    public static int selectedSlot = 0; //what the player is currently selecting to use
    public static int lastSelectedSlot = 0; //what the player last chosen
    public Canvas guiReference; 
    public PickableItem heldGameObject;
    public Camera renderCamera;
    //TODO have inventory contain the inventory slots somehow (in a multiplayer safe way?)
    //this will very much not work in multiplayer im assuming though im not comfortable with how references work for 
    
    public void Start(){
        Transform baseReference = guiReference.transform.GetChild(0).transform;
       
        for (int i = 0; i < baseReference.childCount; i++){
            inventoryFrames[i] = baseReference.GetChild(i).gameObject.GetComponent<InventoryItemFrame>();
            inventoryFrames[i].slot = inventory[i];
            //color in inv
            if (selectedSlot == i) inventoryFrames[i].isSelected = true;
        }

        // i've heard this is a bad idea but im just like ;-; rn
        renderCamera = transform.Find("PlayerCamera").Find("ItemRenderCamera").GetComponent<Camera>();

    }

    //fires when player picks up object
    public void Equip(PickableItem item)
    {
        InventorySlot invSlot = inventory[selectedSlot];
        int newSelectedSlot = selectedSlot;
        // Check if the selected slot is avalible; if so, put it in
        
        if (invSlot.SOReference != null){
            //look for an open slot in inventory otherwise
            for (int i = 0; i < inventory.Length; i++){
                InventorySlot slot = inventory[i];
                
                if (slot.SOReference == null){ //if it finds one, break and use that slot
                    invSlot = slot;
                    newSelectedSlot = i;
                    break;
                }
            }
        }
       
       //if the slot is occupied already, call drop on it

        if (invSlot.SOReference != null){
            Drop();
        }
        
        invSlot.SOReference = item.inventoryItemSO;
        
        invSlot.state = new PickableItemState();
        invSlot.state.CaptureFrom(item);

        print(item.inventoryItemSO);
        print(inventory[selectedSlot]);
        
        selectedSlot = newSelectedSlot;
        ChangeModel();
        // reference the SO of the item to the inventoryitemframe
    }

    //fires when player drops object (either told or attempts to pick up an object when inventory is full, in which they will drop the currently held one)
    //if we ever want to force an invSlot unequip
    public void Drop(int inventorySlot)
    {
        InventorySlot unequippingSlot = inventory[inventorySlot];
        if (unequippingSlot.SOReference == null) return; //if nothing then just don't do anything

        
        Vector3 heldPos = transform.position;
        Quaternion heldRot = transform.rotation;
        //destroy held reference
        if (heldGameObject != null){
            heldPos = heldGameObject.transform.position;
            heldRot = heldGameObject.transform.rotation;
            Destroy(heldGameObject.gameObject);
            heldGameObject = null;
        }

        GameObject thing = Instantiate(unequippingSlot.SOReference.objectPrefab, heldPos, heldRot);
        //so it always renders
        // set references onto them
        PickableItem reference = thing.GetComponent<PickableItem>();
        reference.health = unequippingSlot.health;
        //else, instantiate thing, set it in front of the player
        //TODO: find out if this is even a good way to handle items

        unequippingSlot.state.ApplyTo(reference);

        unequippingSlot.SOReference = null;
        unequippingSlot.health = 0;
    }

    public void Drop()
    {
        Drop(selectedSlot);
    }

    public void DestroyHeldItem()
    {
        InventorySlot currentSlot = inventory[selectedSlot];

        if(currentSlot.SOReference == null) return; //if nothing then just don't do anything

        if(heldGameObject != null)
        {
            Destroy(heldGameObject.gameObject);
            heldGameObject = null;
        }

        currentSlot.SOReference = null;
        currentSlot.state = new PickableItemState();
    }

    private void ChangeModel(){

        //delete the existing held model if any
        if (heldGameObject != null){
            inventory[selectedSlot].state.CaptureFrom(heldGameObject);
            Destroy(heldGameObject.gameObject);
            heldGameObject = null;
        }
        
        // reference the current equipping inventory slot
        InventorySlot invSlot = inventory[selectedSlot];
        if (invSlot.SOReference == null) return;

        //get gameObj ref first
        GameObject heldObj = Instantiate(invSlot.SOReference.objectPrefab, renderCamera.transform);
        heldGameObject = heldObj.GetComponent<PickableItem>();

        invSlot.state.ApplyTo(heldGameObject);

        //make it show in held render
        heldObj.layer = LayerMask.NameToLayer("HeldRender");
        print(heldObj.layer);

        // set held positions relative to camera
        heldGameObject.transform.localPosition = invSlot.SOReference.holdVector3; 
        heldGameObject.transform.localRotation = invSlot.SOReference.holdQuaternion;

        //mark it as usuable so it can be used
        PickableItem refPickable = heldGameObject.GetComponent<PickableItem>();
        refPickable.isUsable = true;

        // disable physics interactions
        Rigidbody heldRigid = heldGameObject.GetComponent<Rigidbody>();
        BoxCollider boxCollide = heldGameObject.GetComponent<BoxCollider>();
        boxCollide.enabled = false;
        heldRigid.isKinematic = false;
        heldRigid.useGravity = false;

    }

    public void Update() { // get selected 
        // imple
        if (Input.GetKeyDown(KeyCode.Alpha1))
        { 
            selectedSlot = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedSlot = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedSlot = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            selectedSlot = 3;
        }
        
        // color in / use image for selected obj
        if (lastSelectedSlot != selectedSlot){
            inventoryFrames[lastSelectedSlot].isSelected = false;
            inventoryFrames[selectedSlot].isSelected = true;
            lastSelectedSlot = selectedSlot;
            ChangeModel();
        }
    }

    public void UsePrimary(){
        if (heldGameObject == null) return;
        heldGameObject.PrimaryUse();
    }
}

[System.Serializable]
public class PickableItemState {
    public int health;
    public bool isUsable;
    public int price;

    public void CaptureFrom(PickableItem item) {
        health = item.health;
        isUsable = item.isUsable;
        price = item.GetPrice();
    }

    public void ApplyTo(PickableItem item) {
        item.health = health;
        item.isUsable = isUsable;
        item.SetPrice(price);
    }
}