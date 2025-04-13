using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryHandler : MonoBehaviour {

    public static InventoryItemFrame[] inventory = new InventoryItemFrame[4];
    public static int selectedSlot = 1; //what the player is currently selecting to use
    //TODO have inventory contain the inventory slots somehow (in a multiplayer safe way?)
    //this will very much not work in multiplayer im assuming though im not comfortable with how references work for 
    
    //fires when player picks up object
    public void EquipToSlot(){
        // reference the SO of the item to the inventoryitemframe
    }

    //fires when player drops object (either told or attempts to pick up an object when inventory is full, in which they will drop the currently held one)
    public void DropSlot(){
        //refernec the inventoryitemframe's inventoryitem_SO reference to instantiate the object
    }

}