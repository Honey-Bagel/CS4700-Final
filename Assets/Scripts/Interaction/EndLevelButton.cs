using UnityEngine;

public class EndLevelButton : I_Button
{
    public override void Interact(GameObject interactor)
    {
        Debug.Log("End Level Button Pressed by: " + interactor.name);
        GameManager.Instance.CompleteLevel();
    }

    public override string GetInteractableName()
    {
        return "End Level Button";
    }

    public override string GetInteractableDescription()
    {
        return "Press to end the level and proceed to the next one.";
    }
}