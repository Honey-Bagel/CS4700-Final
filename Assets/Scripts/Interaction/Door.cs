using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour, I_Interactable
{
    public bool isLocked = false;
    public Transform doorPivot;
    public DoorSize doorSize = DoorSize.Default;
    public bool reverseDirection = false;  // Controls default direction if player check fails
    public float openAngle = 90f;
    public float smooth = 2.0f;
    public Transform TooltipAnchor;
    public bool usePlayerPositionForDirection = true;  // Whether to use player position for door direction

    private bool isOpen = false;
    private Animator animator;
    private Quaternion defaultLocalRotation;  // Use local rotation instead of world
    private Vector3 rotationAxis = Vector3.up;  // Default rotation axis
    private bool isMoving = false;
    private bool openForward = true;  // Direction to open, will be set based on player position

    void Awake()
    {
        animator = GetComponent<Animator>();

        if(doorPivot == null)
        {
            doorPivot = transform;
        }

        // Store the initial local rotation
        defaultLocalRotation = doorPivot.localRotation;
        
        // Determine rotation axis based on the door's original orientation
        // Usually doors rotate around the world up axis (0,1,0)
        rotationAxis = doorPivot.parent != null ? doorPivot.parent.up : Vector3.up;
    }

    public void Interact(GameObject interactor)
    {
        if(isMoving || isLocked) return;
        
        // Determine which way the door should open based on player position
        if (usePlayerPositionForDirection && interactor != null)
        {
            Vector3 doorForward = transform.forward;
            Vector3 toPlayer = interactor.transform.position - transform.position;
            toPlayer.y = 0; // Ignore height difference

            // Check which side of the door the player is on using dot product
            float dot = Vector3.Dot(doorForward, toPlayer);
            openForward = dot > 0;

            // If user wants to manually reverse direction, flip it
            if (reverseDirection)
                openForward = !openForward;
            
            Debug.Log($"Door opening {(openForward ? "forward" : "backward")} based on player position. Dot: {dot}");
        }
        else
        {
            // If not using player position, just use the reverseDirection setting
            openForward = !reverseDirection;
        }
        
        isMoving = true;
        ToggleDoor();
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;

        if(animator != null)
        {
            animator.SetBool("isOpen", isOpen);
        }
        else
        {
            StartCoroutine(AnimateDoor(isOpen));
        }

        Debug.Log(isOpen ? "Door opened" : "Door closed");
    }

    private IEnumerator AnimateDoor(bool open)
    {
        // Calculate target rotation directly when needed instead of storing
        Quaternion targetRotation;
        
        if (open)
        {
            // Use AngleAxis to create rotation around specific axis
            float angle = !openForward ? openAngle : -openAngle;
            Quaternion rotationDelta = Quaternion.AngleAxis(angle, rotationAxis);
            targetRotation = defaultLocalRotation * rotationDelta;
        }
        else
        {
            targetRotation = defaultLocalRotation;
        }
        
        float timer = 0;
        Quaternion startRotation = doorPivot.localRotation;

        while(timer < 1)
        {
            timer += Time.deltaTime * smooth;
            doorPivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, timer);
            yield return null;
        }
        doorPivot.localRotation = targetRotation;
        isMoving = false;
    }

    public string GetInteractableName()
    {
        return isLocked ? "Locked Door" : "Door";
    }

    public string GetInteractableDescription()
    {
        if(isLocked)
        {
            return "Cannot open door";
        } else {
            return "Press E to " + (isOpen ? "close" : "open") + " door";
        }
    }

    public Transform GetTooltipAnchor()
    {
        return TooltipAnchor != null ? TooltipAnchor : transform;
    }
}