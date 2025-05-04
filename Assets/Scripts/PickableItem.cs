using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PickableItem : MonoBehaviour, I_Interactable
{
    // Start is called before the first frame update
    public InventoryItem_SO inventoryItemSO;
    public int health = -1;
    public bool isUsable = false;

    private int _price;

    void Awake()
    {
        _price = inventoryItemSO.objectPrice;
    }

    public virtual void PrimaryUse(){
        print("USE");
    }

    public void Interact(GameObject ineractor)
    {
        // Do nothing, handling interaction pickup in player controller
    }

    public void SetRandomizedPrice(float difficulty, int levelNumber)
    {
        int basePrice = inventoryItemSO.objectPrice;
        float modifier = Random.Range(0.8f, 1.2f);
        float difficultyFactor = 1.0f + (difficulty * 0.1f);
        float levelFactor = 1.0f + (levelNumber * 0.05f);

        _price = Mathf.RoundToInt(basePrice * modifier * difficultyFactor * levelFactor);

        _price = Mathf.Max(_price, 1);
    }

    public string GetInteractableName()
    {
        //return inventoryItemSO.itemName;
        return inventoryItemSO ? inventoryItemSO.objectName : "Unknown Item";
    }

    public string GetInteractableDescription()
    {
        // Should be price for items
        return inventoryItemSO ? $"${_price}" : "$-0";
    }

    public int GetPrice()
    {
        return _price;
    }

    public void SetPrice(int price)
    {
        _price = price;
    }
    
    public Transform GetTooltipAnchor()
    {
        return null;
    }
}
