using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
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

    public bool isOpen = false;
    private Animator animator;
    private Quaternion defaultLocalRotation;  // Use local rotation instead of world
    private Vector3 rotationAxis = Vector3.up;  // Default rotation axis
    private bool isMoving = false;
    private bool openForward = true;  // Direction to open, will be set based on player position
    private BoxCollider boxCollider;
    private NavMeshObstacle navMeshObstacle;  // Optional NavMesh obstacle for door

    [Header("Door SFX")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    private AudioSource  _audio;

    void Awake()
    {
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider>();
        navMeshObstacle = GetComponent<NavMeshObstacle>();

        _audio = GetComponent<AudioSource>();
        _audio.playOnAwake = false;  // Don't play anything on start

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
        boxCollider.enabled = false;
        ToggleDoor();
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;

        if(boxCollider != null) {
            Debug.Log("Disabling collider while opening door");
            boxCollider.enabled = false;  // Disable the collider while opening
        }
        if(navMeshObstacle != null) {
            navMeshObstacle.enabled = false;  // Disable NavMesh obstacle if present
        }

        if(animator != null)
        {
            animator.SetBool("isOpen", isOpen);
        }
        else
        {
            StartCoroutine(AnimateDoor(isOpen));
        }

        if (_audio != null)
        {
            var clip = isOpen ? openClip : closeClip;
            if (clip != null)
                _audio.PlayOneShot(clip);
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
        boxCollider.enabled = true;  // Re-enable the collider after the animation
        navMeshObstacle.enabled = true;  // Re-enable NavMesh obstacle if present
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

    public bool IsEntityInFront(Vector3 entityPosition)
    {
        Vector3 doorToEntity = entityPosition - transform.position;
        doorToEntity.y = 0; // Ignore vertical difference
        return Vector3.Dot(transform.forward, doorToEntity) > 0;
    }

    // Get the center point of the doorway
    public Vector3 GetDoorwayCenter()
    {
        // Assuming the door pivot is at the edge of the door
        // Calculate the center point based on door size
        float doorWidth = 1.0f; // Adjust based on your door size
        Vector3 doorRight = transform.right;
        
        return doorPivot.position + (doorRight * doorWidth * 0.5f);
    }

    // Check if we should keep the door open
    public bool ShouldKeepOpen(Collider entity)
    {
        // Check if entity is within doorway bounds
        Vector3 doorCenter = GetDoorwayCenter();
        float distanceToDoor = Vector3.Distance(entity.bounds.center, doorCenter);
        
        // Consider entity in doorway if within threshold
        return distanceToDoor < 2.0f;
    }
}