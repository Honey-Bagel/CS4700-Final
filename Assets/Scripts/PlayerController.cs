using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, ISaveable
{
    [SerializeField]
    private string uniqueID;

    public Camera playerCamera;
    
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float gravity = .07f;
    public float maxFallSpeed = 0.15f;
    public float jumpForce = .04f;
    public InventoryHandler playerInventory;
    //Should probably pull this out to a setting eventually
    private float mouseSensitivity = 1f;

    private float verticalViewClamp = 45f;

    CharacterController characterController;
    private Vector3 movementVelocity = Vector3.zero;
    private float xRotation = 0;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerInventory = GetComponent<InventoryHandler>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Notify the GameManager that the player is ready
        GameManager.Instance.SetPlayerReady();
    }

    void Update()
    {
        // Movement code remains the same
        Vector3 forwardVector = transform.TransformDirection(Vector3.forward);
        Vector3 rightVector = transform.TransformDirection(Vector3.right);

        Vector3 inputVector = new Vector3(Input.GetAxis("Vertical"), 0f, Input.GetAxis("Horizontal"));

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float xVelocity = (isRunning ? runSpeed : walkSpeed) * inputVector.x * Time.deltaTime;
        float zVelocity = (isRunning ? runSpeed : walkSpeed) * inputVector.z * Time.deltaTime;
        float yVelocity = movementVelocity.y;
        movementVelocity = (forwardVector * xVelocity) + (rightVector * zVelocity);

        if (Input.GetKey(KeyCode.Space) && characterController.isGrounded)
        {
            movementVelocity.y = jumpForce;
        }
        else
        {
            movementVelocity.y = yVelocity;
        }

        if (!characterController.isGrounded)
        {
            movementVelocity.y = Mathf.Clamp(movementVelocity.y - gravity * Time.deltaTime, -maxFallSpeed, float.PositiveInfinity);
        }

        characterController.Move(movementVelocity);

        // Camera Rotation
        xRotation += -Input.GetAxis("Mouse Y") * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -verticalViewClamp, verticalViewClamp);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * mouseSensitivity, 0);
        
        // Rest of the code remains the same
        // pickup and drop interactions...
    }
    
    // ISaveable implementation
    public SaveableData SaveState()
    {
        return new PlayerData
        {
            // Only store player-specific data here
            // Game state data like deaths is managed by GameManager
        };
    }

    public void LoadState(SaveableData saveData)
    {
        if (saveData is PlayerData data)
        {
            // Only load player-specific data here
            // Game state data like deaths is managed by GameManager
        }
    }
    
    public string GetUniqueID()
    {
        return uniqueID;
    }
}

[Serializable]
public class PlayerData : SaveableData
{
    // Add player-specific fields here
    // Deaths are now tracked in GameManager
}