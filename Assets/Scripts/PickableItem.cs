using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PickableItem : MonoBehaviour
{
    // Start is called before the first frame update
    public InventoryItem_SO inventoryItemSO;
    public int health;
    public bool isUsable = false;

    public virtual void PrimaryUse(){
        print("USE");
    }
}
