using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This is an interface for all physical items that can be picked up by the player and placed into the inventory
*/
public interface PickableItem
{
    string name;

    void OnPickup();
}
