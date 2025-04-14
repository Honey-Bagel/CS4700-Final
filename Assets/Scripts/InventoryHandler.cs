using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryHandler : MonoBehaviour {

    public static InventoryItemFrame[] inventory = new InventoryItemFrame[4];
    public static int selectedSlot = 0; //what the player is currently selecting to use
    public Canvas guiReference;
    //TODO have inventory contain the inventory slots somehow (in a multiplayer safe way?)
    //this will very much not work in multiplayer im assuming though im not comfortable with how references work for 
    
    public void Start(){
        Transform baseReference = guiReference.transform.GetChild(0).transform;
       
        for (int i = 0; i < baseReference.childCount; i++){
            inventory[i] = baseReference.GetChild(i).gameObject.GetComponent<InventoryItemFrame>();
            print(inventory[i]);
        }
    }

    //fires when player picks up object
    public void Equip(PickableItem item)
    {
        print(item.inventoryItemSO);
        print(inventory[selectedSlot].name);
        // reference the SO of the item to the inventoryitemframe
    }

    //fires when player drops object (either told or attempts to pick up an object when inventory is full, in which they will drop the currently held one)
    public void Drop(PickableItem item)
    {
        
        //refernec the inventoryitemframe's inventoryitem_SO reference to instantiate the object
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
    }

}