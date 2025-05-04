using System.Collections.Generic;
using UnityEngine;

public class ScrapGrinder : MonoBehaviour
{
    public Transform toolTipAnchor;
    public float timePerItem = 1f;

    public void GrindItems(GameObject interactor)
    {
        Debug.Log("Grinding items in the area...");

        InventoryHandler inventoryHandler = interactor.GetComponent<InventoryHandler>();
        if (inventoryHandler == null)
        {
            Debug.LogWarning("Interactor does not have an InventoryHandler component.");
            return;
        }

        if(inventoryHandler.heldGameObject != null) {
            PickableItem item = inventoryHandler.heldGameObject.GetComponent<PickableItem>();
            if(item != null)
            {
                int scrapValue = item.GetPrice();

                Debug.Log($"Scrap price: {scrapValue}");

                inventoryHandler.DestroyHeldItem();

                GameManager.Instance.AddScrap(scrapValue);
            }
        }

    }


}