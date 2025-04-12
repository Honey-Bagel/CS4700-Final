
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Camera playerCamera;
    private float walkSpeed = 4f;
    private float runSpeed = 8f;
    private float gravity = .07f;
    private float maxFallSpeed = 0.15f;
    private float jumpForce = .04f;



    private float mouseSensitivity = 1f;
    private float verticalViewClamp = 45f;


    CharacterController characterController;
    
    
    public Vector3 movementVelocity = Vector3.zero;
    private float xRotation = 0;
    
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {

        Vector3 forwardVector = transform.TransformDirection(Vector3.forward);
        Vector3 rightVector = transform.TransformDirection(Vector3.right);

        Vector3 inputVector = new Vector3(Input.GetAxis("Vertical"),0f,Input.GetAxis("Horizontal"));

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float xVelocity = (isRunning ? runSpeed : walkSpeed) * inputVector.x *Time.deltaTime;
        float zVelocity = (isRunning ? runSpeed : walkSpeed) * inputVector.z *Time.deltaTime;
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
            movementVelocity.y =Mathf.Clamp(movementVelocity.y-gravity*Time.deltaTime,-maxFallSpeed,float.PositiveInfinity);
            print(movementVelocity.y);
        }


        characterController.Move(movementVelocity);

        {
            xRotation += -Input.GetAxis("Mouse Y") * mouseSensitivity;
            xRotation = Mathf.Clamp(xRotation, -verticalViewClamp, verticalViewClamp);
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * mouseSensitivity, 0);
        }

    }
}