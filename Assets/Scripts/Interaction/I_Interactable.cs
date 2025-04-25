using UnityEngine;

public interface I_Interactable
{
    void Interact(GameObject interactor);
    string GetInteractableName();
    string GetInteractableDescription();

    Transform GetTooltipAnchor();
}