using System;
using UnityEngine;

public class Container : MonoBehaviour
{
    [Header("Container Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private string uniqueID;
    [SerializeField] private ContainerItemLocation[] items;
    
    // Tooltip settings
    [SerializeField] private Transform tooltipAnchor;

    void Awake()
    {
        items = GetComponentsInChildren<ContainerItemLocation>();
    }

    public Transform GetTooltipAnchor()
    {
        return tooltipAnchor;
    }

    public SaveableData SaveState()
    {
        return new ContainerData
        {
            isOpen = isOpen,
            items = items
        };
    }

    public void LoadState(SaveableData data)
    {
        if (data is ContainerData containerData)
        {
            isOpen = containerData.isOpen;
            items = containerData.items;
        }
    }

    public string GetUniqueID()
    {
        return uniqueID;
    }
}

[Serializable]
public class ContainerData : SaveableData
{
    public bool isOpen;
    public ContainerItemLocation[] items;
}