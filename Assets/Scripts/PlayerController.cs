
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StaminaComponent))]
[RequireComponent(typeof(StaminaComponent))]


public class PlayerController : MonoBehaviour
{

    public Camera playerCamera;

    public StaminaComponent playerStamina;
    public float runningStaminaRate = 10.0f; // stamina points per second
    public float jumpStaminaUse = 25.0f;

    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float gravity = .13f;
    public float maxFallSpeed = 0.15f;
    public float jumpForce = .04f;

    //Should probably pull this out to a setting eventually
    private float mouseSensitivity = 1f;


    private float verticalViewClamp = 45f;


    CharacterController characterController;
    private Vector3 movementVelocity = Vector3.zero;
    private float xRotation = 0;
    
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerStamina = GetComponent<StaminaComponent>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {

        //Get the current forward and right directions from the player transform
        Vector3 forwardVector = transform.TransformDirection(Vector3.forward);
        Vector3 rightVector = transform.TransformDirection(Vector3.right);

        Vector3 inputVector = new Vector3(Input.GetAxis("Vertical"),0f,Input.GetAxis("Horizontal"));

        bool isRunning = Input.GetKey(KeyCode.LeftShift) && inputVector.magnitude>0f && playerStamina.UseStamina(runningStaminaRate*Time.deltaTime,true);
        float xVelocity = (isRunning ? runSpeed : walkSpeed) * inputVector.x *Time.deltaTime;
        float zVelocity = (isRunning ? runSpeed : walkSpeed) * inputVector.z *Time.deltaTime;
        float yVelocity = movementVelocity.y;
        movementVelocity = (forwardVector * xVelocity) + (rightVector * zVelocity);


        if (Input.GetKey(KeyCode.Space) && characterController.isGrounded && playerStamina.UseStamina(jumpStaminaUse,false))
        {
            movementVelocity.y = jumpForce;
        }
        else
        {
            movementVelocity.y = yVelocity;
        }

        if (!characterController.isGrounded)
        {
            movementVelocity.y =Mathf.Clamp(movementVelocity.y-gravity*Time.deltaTime,-maxFallSpeed,float.PositiveInfinity);
            print(movementVelocity.y);
        }


        characterController.Move(movementVelocity);
        

        //Camera Rotation
        
        xRotation += -Input.GetAxis("Mouse Y") * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -verticalViewClamp, verticalViewClamp);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * mouseSensitivity, 0);
        

    }
}