using System;
using UnityEngine;

[Serializable]
public class ContainerItemLocation : MonoBehaviour, I_Interactable
{
    public PickableItem item;
    public GameObject itemPrefab;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }

    public void Interact(GameObject player)
    {
        Debug.Log("test");
        if (item != null)
        {
            player.GetComponent<InventoryHandler>().Equip(item);
            Destroy(item.gameObject);
            item = null;
            itemPrefab = null;
        } else {
            Debug.Log("trying to place item");
            if(player.GetComponent<InventoryHandler>().heldGameObject == null) return; //if nothing then just don't do anything
            itemPrefab = player.GetComponent<InventoryHandler>().heldGameObject.inventoryItemSO.objectPrefab;
            GameObject gameObject = Instantiate(itemPrefab, transform.position, Quaternion.identity);
            gameObject.GetComponent<BoxCollider>().enabled = false;
            gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            item = gameObject.GetComponent<PickableItem>();
            player.GetComponent<InventoryHandler>().DestroyHeldItem();
        }
    }

    public Transform GetTooltipAnchor()
    {
        return transform; // Return the item's position as the tooltip anchor
    }

    public string GetInteractableName()
    {
        return item != null ? item.GetInteractableName() : "Empty Slot";
    }

    public string GetInteractableDescription()
    {
        return item != null ? "Interact to pick up item" : "Interact to store item";
    }
}