using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class I_Button : MonoBehaviour, I_Interactable
{
    public UnityEvent<GameObject> onButtonPressed;

    public virtual void Interact(GameObject interactor)
    {
        onButtonPressed.Invoke(interactor);
    }

    public virtual string GetInteractableName()
    {
        return "Grind";
    }

    public virtual string GetInteractableDescription()
    {
        return "Grind objects into scrap.";
    }

    public Transform GetTooltipAnchor()
    {
        return null;
    }
}
