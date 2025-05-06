using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StaminaComponent))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(AudioSource))] 
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

    // Should probably pull this out to a setting eventually
    private float mouseSensitivity = 1f;
    private float verticalViewClamp = 90f;

    // Core components
    private CharacterController characterController;
    private Vector3 movementVelocity = Vector3.zero;
    private float   xRotation        = 0f;

    // Interaction stuff
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

    // Stamina & Health
    public StaminaComponent staminaComponent;
    public float runningStaminaRate = 10f;
    public float jumpStaminaUse     = 25f;

    // Footstep one-shots
    [Header("Footstep SFX")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float      stepDistance = 2f;
    private AudioSource _sfx;
    private Vector3     _lastPos;
    private float       _accumDistance;

    // ── Walking Loop ──
    [Header("Walking Loop")]
    [SerializeField] private AudioClip walkLoopClip;
    private AudioSource _walkLoopAudio;
    // ─────────────────

    void Awake()
    {
        // Cache CharacterController early
        characterController = GetComponent<CharacterController>();

        // Setup one-shot SFX source
        _sfx = GetComponent<AudioSource>();
        _sfx.playOnAwake = false;

        // Setup looping walk source
        _walkLoopAudio             = gameObject.AddComponent<AudioSource>();
        _walkLoopAudio.clip        = walkLoopClip;
        _walkLoopAudio.loop        = true;
        _walkLoopAudio.playOnAwake = false;
        _walkLoopAudio.Stop();

        // Initialize distance tracker
        _lastPos = transform.position;
    }

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
        // Finish grabbing components after level load
        playerInventory  = GetComponent<InventoryHandler>();
        staminaComponent = GetComponent<StaminaComponent>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Notify GameManager
        GameManager.Instance.SetPlayerReady();
        scrapCountTarget.text = GameManager.Instance.TargetScrapCount.ToString();

        if (interactionTooltip != null)
            interactionTooltip.SetActive(false);

        GameManager.OnLevelSetupFinished -= OnLevelReady;
    }

    void Update()
    {
        // Wait until GameManager says we’re good
        if (GameManager.Instance == null || !GameManager.Instance.IsPlayerReady)
            return;

        // Update UI scrap count
        if (scrapCountText != null)
            scrapCountText.text = GameManager.Instance.ScrapTowardsTarget.ToString();

        // Calculate movement
        Vector3 forwardVector = transform.TransformDirection(Vector3.forward);
        Vector3 rightVector   = transform.TransformDirection(Vector3.right);
        Vector3 inputVector   = new Vector3(Input.GetAxis("Vertical"), 0f, Input.GetAxis("Horizontal"));

        bool isRunning = Input.GetKey(KeyCode.LeftShift)
                         && inputVector.magnitude > 0f
                         && staminaComponent.UseStamina(runningStaminaRate * Time.deltaTime, true);

        float xVel = (isRunning ? runSpeed : walkSpeed) * inputVector.x * Time.deltaTime;
        float zVel = (isRunning ? runSpeed : walkSpeed) * inputVector.z * Time.deltaTime;
        float yVel = movementVelocity.y;

        movementVelocity = forwardVector * xVel + rightVector * zVel;

        // Jump logic
        if (Input.GetKey(KeyCode.Space)
            && characterController.isGrounded
            && staminaComponent.UseStamina(jumpStaminaUse, false))
        {
            movementVelocity.y = jumpForce;
        }
        else
        {
            movementVelocity.y = yVel;
        }

        // Apply gravity
        if (!characterController.isGrounded)
        {
            movementVelocity.y = Mathf.Clamp(
                movementVelocity.y - gravity * Time.deltaTime,
               -maxFallSpeed,
                float.PositiveInfinity
            );
        }

        // Move character
        characterController.Move(movementVelocity);

        // ── Play/Stop looping walk sound ──
        bool isWalking = characterController.isGrounded && movementVelocity.magnitude > 0.1f;
        if (isWalking && !_walkLoopAudio.isPlaying)
            _walkLoopAudio.Play();
        else if (!isWalking && _walkLoopAudio.isPlaying)
            _walkLoopAudio.Stop();
        // ──────────────────────────────────

        // Footstep one-shots (optional)
        float delta = Vector3.Distance(transform.position, _lastPos);
        _accumDistance += delta;
        if (_accumDistance >= stepDistance && IsWalking())
        {
            // PlayFootstep();
            // _accumDistance = 0f;
        }
        _lastPos = transform.position;

        // Camera rotation
        xRotation += -Input.GetAxis("Mouse Y") * mouseSensitivity;
        xRotation  = Mathf.Clamp(xRotation, -verticalViewClamp, verticalViewClamp);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * mouseSensitivity, 0);

        // Interactions
        CheckForInteractable();
        if (Input.GetKeyDown(KeyCode.E))      HandlePickup();
        else if (Input.GetKeyDown(KeyCode.G)) playerInventory.Drop();
        else if (Input.GetKeyDown(KeyCode.Mouse0)) playerInventory.UsePrimary();
    }

    private void CheckForInteractable()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.yellow);

        if (Physics.Raycast(ray, out hitInfo, interactionRange, interactableLayerMask, QueryTriggerInteraction.Ignore))
        {
            var interactable = hitInfo.collider.GetComponent<I_Interactable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                ShowTooltip(interactable, hitInfo);
                return;
            }
        }

        currentInteractable = null;
        interactionTooltip?.SetActive(false);
    }

    private void ShowTooltip(I_Interactable interactable, RaycastHit hitInfo)
    {
        itemNameText.text        = interactable.GetInteractableName();
        itemDescriptionText.text = interactable.GetInteractableDescription();

        Vector3 tooltipWorldPos = interactable.GetTooltipAnchor()?.position
            ?? hitInfo.transform.position + Vector3.up * 0.5f;

        Vector2 screenPoint = playerCamera.WorldToScreenPoint(tooltipWorldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPoint, null, out Vector2 canvasPos);

        interactionTooltip.GetComponent<RectTransform>().localPosition = canvasPos;
        interactionTooltip.SetActive(true);
    }

    private void HandlePickup()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 4f, LayerMask.GetMask("Default")))
        {
            var interactable = hit.collider.GetComponent<I_Interactable>();
            if (interactable != null)
            {
                interactable.Interact(gameObject);
                if (interactable is PickableItem item)
                {
                    Debug.Log($"pick up {item.name}");
                    playerInventory.Equip(item);
                    Destroy(item.gameObject);
                    interactionTooltip?.SetActive(false);
                }
            }
        }
    }

    // Footstep helpers
    private bool IsWalking()
    {
        return characterController.isGrounded
            && (Input.GetAxis("Vertical")   != 0 ||
                Input.GetAxis("Horizontal") != 0);
    }

    private void PlayFootstep()
    {
        if (footstepSounds?.Length > 0)
            _sfx.PlayOneShot(footstepSounds[UnityEngine.Random.Range(0, footstepSounds.Length)]);
    }

    // ISaveable
    public SaveableData SaveState()       => new PlayerData();
    public void LoadState(SaveableData d) { if (d is PlayerData) {/*…*/} }
    public string GetUniqueID()           => uniqueID;

    // Upgrades & damage
    public void ApplyUpgrades(Dictionary<Upgrade,int> ups)
    {
        Debug.Log("Adding upgrades to Player");
        foreach (var kv in ups)
        {
            switch (kv.Key.upgradeType)
            {
                case UpgradeType.Health:
                    GetComponent<HealthComponent>().MaxHealth += kv.Key.modifier * kv.Value;
                    break;
                case UpgradeType.Speed:
                    runSpeed += kv.Key.modifier * kv.Value;
                    walkSpeed += kv.Key.modifier * kv.Value;
                    break;
                case UpgradeType.Damage:
                    // …
                    break;
                case UpgradeType.StaminaIncrease:
                    staminaComponent.MaxStamina += kv.Key.modifier * kv.Value;
                    break;
                case UpgradeType.StaminaRecharge:
                    staminaComponent.staminaRegenRate += kv.Key.modifier * kv.Value;
                    break;
                default:
                    Debug.LogWarning($"Unknown upgrade: {kv.Key.upgradeType}");
                    break;
            }
        }
    }

    void OnPlayerDeath()
    {
        Debug.Log("Player has died.");
        GameManager.Instance.LevelFailed();
    }

    public void TakeDamage(int dmg)
    {
        GetComponent<HealthComponent>()?.TakeDamage(dmg);
    }
}

[Serializable]
public class PlayerData : SaveableData
{
    // Add player-specific fields here
    // Deaths are now tracked in GameManager
}
