using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StaminaComponent))]
[RequireComponent(typeof(HealthComponent))]
public class PlayerController : MonoBehaviour, ISaveable, IDamageable
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

    private float verticalViewClamp = 90f;

    CharacterController characterController;
    private Vector3 movementVelocity = Vector3.zero;
    private float xRotation = 0;

    //Interaction stuff
    [Header("UI Elements")]
    public GameObject interactionTooltip;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public RectTransform canvasRect;
    public TextMeshProUGUI scrapCountText;
    public TextMeshProUGUI scrapCountTarget;
    public LayerMask interactableLayerMask;

    private float interactionRange = 4f;
    private I_Interactable currentInteractable;

    // Components
    public StaminaComponent staminaComponent;
    public float runningStaminaRate = 10.0f;
    public float jumpStaminaUse = 25.0f;
    
    void Start()
    {
        GameManager.OnLevelSetupFinished += OnLevelReady;
        GameManager.Instance.IsPlayerReady = false;
    }

    void OnEnable()
    {
        HealthComponent.OnDeath += OnPlayerDeath;
    }

    void OnDisable()
    {
        HealthComponent.OnDeath -= OnPlayerDeath;
    }

    private void OnLevelReady()
    {
        characterController = GetComponent<CharacterController>();
        playerInventory = GetComponent<InventoryHandler>();
        staminaComponent = GetComponent<StaminaComponent>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Notify the GameManager that the player is ready
        GameManager.Instance.SetPlayerReady();

        scrapCountTarget.text = GameManager.Instance.TargetScrapCount.ToString();

        if(interactionTooltip != null)
        {
            interactionTooltip.SetActive(false);
        }

        GameManager.OnLevelSetupFinished -= OnLevelReady;
    }

    void Update()
    {
        if(GameManager.Instance == null || GameManager.Instance.IsPlayerReady == false)
        {
            return;
        }
        if(scrapCountText != null)
        {
            scrapCountText.text = GameManager.Instance.ScrapTowardsTarget.ToString();
        }
            // Update scrap count in UI
        
        // Movement code remains the same
        Vector3 forwardVector = transform.TransformDirection(Vector3.forward);
        Vector3 rightVector = transform.TransformDirection(Vector3.right);

        Vector3 inputVector = new Vector3(Input.GetAxis("Vertical"), 0f, Input.GetAxis("Horizontal"));

        bool isRunning = Input.GetKey(KeyCode.LeftShift) && inputVector.magnitude > 0f && staminaComponent.UseStamina(runningStaminaRate * Time.deltaTime, true);
        float xVelocity = (isRunning ? runSpeed : walkSpeed) * inputVector.x * Time.deltaTime;
        float zVelocity = (isRunning ? runSpeed : walkSpeed) * inputVector.z * Time.deltaTime;
        float yVelocity = movementVelocity.y;
        movementVelocity = (forwardVector * xVelocity) + (rightVector * zVelocity);

        if (Input.GetKey(KeyCode.Space) && characterController.isGrounded && staminaComponent.UseStamina(jumpStaminaUse, false))
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

        CheckForInteractable();
        
        // pickup interaction
        if (Input.GetKeyDown(KeyCode.E))
        {

            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            Debug.DrawRay(ray.origin, ray.direction * 2, Color.red, 2f);

            if (Physics.Raycast(ray, out hitInfo, 4, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore)){

                I_Interactable interactable = hitInfo.collider.gameObject.GetComponent<I_Interactable>();
                if(interactable != null)
                {
                    interactable.Interact(gameObject);

                    if(interactable is PickableItem item)
                    {
                        print("pick up " + item.name);
                        playerInventory.Equip(item);
                        Destroy(item.gameObject);

                        currentInteractable = null;

                        if(interactionTooltip != null)
                        {
                            interactionTooltip.SetActive(false);
                        }
                    }
                }
            }

        }

        // drop interaction
        if (Input.GetKeyDown(KeyCode.G))
        {
            playerInventory.Drop();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            playerInventory.UsePrimary();
        }
    }

    private void CheckForInteractable()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        // Debug ray
        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.yellow);

        if (Physics.Raycast(ray, out hitInfo, interactionRange, interactableLayerMask, QueryTriggerInteraction.Ignore))
        {
            I_Interactable interactable = hitInfo.collider.gameObject.GetComponent<I_Interactable>();
            if (interactable != null)
            {
                // Found an interactable
                currentInteractable = interactable;
                
                // Update tooltip text
                if (interactionTooltip != null)
                {
                    // Set text content
                    itemNameText.text = interactable.GetInteractableName();
                    itemDescriptionText.text = interactable.GetInteractableDescription();
                    
                    // Check if the interactable has a custom tooltip anchor
                    Vector3 tooltipPosition;
                    Transform tooltipAnchor = interactable.GetTooltipAnchor();
                    
                    if (tooltipAnchor != null)
                    {
                        // Use the custom anchor position
                        tooltipPosition = tooltipAnchor.position;
                    }
                    else
                    {
                        // Fall back to the default positioning method
                        float offsetPosY = hitInfo.transform.position.y + 0.5f;
                        tooltipPosition = new Vector3(hitInfo.transform.position.x, offsetPosY, hitInfo.transform.position.z);
                    }
                    
                    // Convert world position to screen position
                    Vector2 screenPoint = playerCamera.WorldToScreenPoint(tooltipPosition);
                    
                    // Convert screen position to Canvas local position
                    Vector2 canvasPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out canvasPos);
                    
                    // Set tooltip position in canvas space
                    interactionTooltip.GetComponent<RectTransform>().localPosition = canvasPos;
                    
                    // Make tooltip visible
                    interactionTooltip.SetActive(true);
                }
            }
            else
            {
                // Not looking at an interactable
                currentInteractable = null;
                if (interactionTooltip != null)
                    interactionTooltip.SetActive(false);
            }
        }
        else
        {
            // Nothing hit
            currentInteractable = null;
            if (interactionTooltip != null)
                interactionTooltip.SetActive(false);
        }
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

    public void ApplyUpgrades(Dictionary<Upgrade, int> upgrades) {
        Debug.Log("Adding upgrades to Player");
        foreach(var upgrade in upgrades) {
            switch(upgrade.Key.upgradeType) {
                case UpgradeType.Health:
                    HealthComponent healthComponent = GetComponent<HealthComponent>();
                    healthComponent.MaxHealth += upgrade.Key.modifier * upgrade.Value;
                    break;
                case UpgradeType.Speed:
                    runSpeed += upgrade.Key.modifier * upgrade.Value;
                    walkSpeed += upgrade.Key.modifier * upgrade.Value;
                    break;
                case UpgradeType.Damage:
                    // Apply damage upgrade logic here
                    break;
                case UpgradeType.StaminaIncrease:
                    staminaComponent.MaxStamina += upgrade.Key.modifier * upgrade.Value;
                    break;
                case UpgradeType.StaminaRecharge:
                    staminaComponent.staminaRegenRate += upgrade.Key.modifier * upgrade.Value;
                    break;
                default:
                    Debug.LogWarning("Unknown upgrade type: " + upgrade.Key.upgradeType);
                    break;
            }
        }
    }

    void OnPlayerDeath()
    {
        Debug.Log("Player has died.");
        GameManager.Instance.LevelFailed();
    }

    public void TakeDamage(int damageAmount)
    {
        HealthComponent healthComponent = GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(damageAmount);
        }
    }
}

[Serializable]
public class PlayerData : SaveableData
{
    // Add player-specific fields here
    // Deaths are now tracked in GameManager
}