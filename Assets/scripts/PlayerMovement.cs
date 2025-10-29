using Steamworks.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera; // Reference to the camera
    public float moveSpeed = 15f;
    public float jumpForce = 15f;
    public float sneakSpeed = 2f;
    public Vector3 cameraOffset = new Vector3(0, 5, -10);
    public float cameraObstructionCheckRadius = 0.5f; // Radius for sphere cast
    private Rigidbody rb;
    private Animator animator;
    private bool isSneaking = false;
    public GameObject PlayerHead;
    public float mouseSensitivity = 1f;
    private NetworkPlayerObject NetworkplayerOBJ;
    
    private float rotationX = 0f;
    private float rotationY = 0f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    
    // Body rotation delay settings
    public float bodyRotationSpeed = 5f; // How fast the body follows the camera
    private float targetBodyRotationY = 0f;
    
    // Character position offset in camera view (unified system)
    public float screenOffsetDistance = 2f; // How far from camera pivot the offset is applied
    
    // Smooth screen offset transition
    private Vector2 currentScreenOffset = Vector2.zero; // Current interpolated offset
    private Vector2 targetScreenOffset = Vector2.zero; // Target offset to transition to
    public float screenOffsetTransitionSpeed = 5f; // How fast the offset transitions

    private Vector2 PickingUpOffset = new Vector2(3f, 0);
    public float gravityScale = 3f; // Controls fall speed
    
    private bool isGrounded = false;
    private bool hasUsedAirAction = false; // Track if air action was already used this jump
    
    // Movement force settings
    public float maxHorizontalSpeed = 15f; // Maximum horizontal movement speed
    public float movementForceMultiplier = 50f; // Force applied for movement
    
    // Wall collision prevention
    public float wallCheckDistance = 0.5f; // Distance to check for walls
    public LayerMask wallLayer = -1; // Layer mask for walls (default: everything)

    public List<item> Inventory = new List<item>();
    public MeshRenderer BodyRenderer;
    public Material[] BodyMaterial;

    // Input Actions
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sneakAction;
    
    // Cached input values
    private Vector2 moveInput;
    private Vector2 lookInput;

    public void OnPickUpItem(item Item)
    {
        Inventory.Add(Item);
        SetCharacterScreenOffset(PickingUpOffset);
    }
    
    public void OnDropItem(item Item)
    {
        Inventory.Remove(Item);
        SetCharacterScreenOffset(Vector2.zero);
    }
    
    public void OnInitialized(int NetID)
    {
        if (BodyRenderer != null && NetID < BodyMaterial.Length)
        {
            BodyRenderer.material = BodyMaterial[NetID];
        }
    }
    
    public void InGameSetup()
    {
        gamecore.instance.InLobby = false;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }
    
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        NetworkplayerOBJ = GetComponent<NetworkPlayerObject>();
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        SetupInputActions();
    }

    void SetupInputActions()
    {
        // Movement input (WASD)
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        
        // Mouse look input
        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
        
        // Jump input
        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        
        // Sneak input
        sneakAction = new InputAction("Sneak", InputActionType.Button);
        sneakAction.AddBinding("<Keyboard>/leftShift");
        
        // Subscribe to events
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        
        lookAction.performed += OnLook;
        lookAction.canceled += OnLook;
        
        jumpAction.performed += OnJump;
        
        sneakAction.started += OnSneakStarted;
        sneakAction.canceled += OnSneakCanceled;
        
        // Enable all actions
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        sneakAction.Enable();
    }

    void OnDestroy()
    {
        // Clean up input actions
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
            moveAction.Disable();
            moveAction.Dispose();
        }
        
        if (lookAction != null)
        {
            lookAction.performed -= OnLook;
            lookAction.canceled -= OnLook;
            lookAction.Disable();
            lookAction.Dispose();
        }
        
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.Disable();
            jumpAction.Dispose();
        }
        
        if (sneakAction != null)
        {
            sneakAction.started -= OnSneakStarted;
            sneakAction.canceled -= OnSneakCanceled;
            sneakAction.Disable();
            sneakAction.Dispose();
        }
    }

    // Input callbacks
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    
    private void OnJump(InputAction.CallbackContext context)
    {
        if (!gamecore.instance.InLobby && NetworkplayerOBJ.IsLocal && !gamecore.instance.InDialogue)
        {
            if (isGrounded)
            {
                PreJump();
            }
            else if (!hasUsedAirAction)
            {
                PreDoubleJump();
            }
        }
    }
    
    private void OnSneakStarted(InputAction.CallbackContext context)
    {
        isSneaking = true;
    }
    
    private void OnSneakCanceled(InputAction.CallbackContext context)
    {
        isSneaking = false;
    }

    void Update()
    {
        if (!gamecore.instance.InLobby && NetworkplayerOBJ.IsLocal && !gamecore.instance.InDialogue)
        {
            HandleCameraRotation();
            UpdateScreenOffset();

            UpdateCamera();
            UpdateHeadRotation();
            UpdateBodyRotation();

            if (Physics.Raycast(transform.position, Vector3.down, gamecore.instance.groundCheckDistance, gamecore.instance.groundLayer))
            {
                IsGrounded = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (!gamecore.instance.InLobby && NetworkplayerOBJ.IsLocal)
        {
            // Apply additional downward force for faster falling
            rb.AddForce(Physics.gravity * (gravityScale - 1f) * rb.mass);
            
            // Handle movement in FixedUpdate for physics-based movement
            MovePlayer();
        }
    }

    void HandleCameraRotation()
    {
        // Use cached look input from event
        if (lookInput == Vector2.zero)
            return;
        
        // Horizontal rotation (around Y axis)
        rotationY += lookInput.x * mouseSensitivity * 0.1f;
        
        // Vertical rotation (around X axis) with clamping
        rotationX -= lookInput.y * mouseSensitivity * 0.1f;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
    }

    void MovePlayer()
    {
        // Use cached move input from event
        float horizontal = moveInput.x;
        float vertical = moveInput.y;
        
        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        Vector3 direction = (forward * vertical + right * horizontal).normalized;
        
        float speed = isSneaking ? sneakSpeed : moveSpeed;
        
        // Apply force for movement only if not blocked by wall
        if (direction.magnitude > 0.1f)
        {
            // Check if there's a wall in the movement direction
            bool canMove = !IsWallInDirection(direction);
            
            if (canMove)
            {
                rb.AddForce(direction * speed * movementForceMultiplier);
            }
            else
            {
                // If blocked by wall, dampen velocity in that direction to prevent sticking
                Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                float velocityInMoveDirection = Vector3.Dot(currentHorizontalVelocity, direction);

                if (velocityInMoveDirection > 0f)
                {
                    // Remove velocity component toward the wall
                    Vector3 velocityTowardWall = direction * velocityInMoveDirection;
                    rb.linearVelocity = new Vector3(
                        rb.linearVelocity.x - velocityTowardWall.x,
                        rb.linearVelocity.y,
                        rb.linearVelocity.z - velocityTowardWall.z
                    );
                }
            }
        }
        
        // Restrict max horizontal velocity
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxHorizontalSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxHorizontalSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }
    
    /// <summary>
    /// Check if there's a wall in the given direction
    /// </summary>
    private bool IsWallInDirection(Vector3 direction)
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f; // Check from mid-height
        
        // Use SphereCast to detect walls
        if (Physics.SphereCast(origin, 1f, direction, out hit, wallCheckDistance, gamecore.instance.groundLayer))
        {
            // Check if the hit surface is vertical (a wall, not ground/ceiling)
            //float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
            return true; // Consider it a wall if angle is steep
        }
        
        return false;
    }
    
    public void PreJump()
    {
        animator.Play("jump");
    }
    
    public void Jump() //Triggered by animation event!
    {
        // Jump when on ground
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        hasUsedAirAction = false; // Reset air action on new jump
    }
    
    public void PreDoubleJump()
    {
        animator.Play("doublejump");
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }
    
    public void DoubleJump()
    {
        // Set vertical velocity to zero when in air (only once per jump)
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        hasUsedAirAction = true; // Mark as used
    }
    
    private void OnCollisionStay(Collision collision)
    {
        // Check if we're touching something below us
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f) // Normal pointing upward means we're on top
            {
                isGrounded = true;
                return;
            }
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
    
    /// <summary>
    /// Check if player is on the ground using raycast
    /// </summary>
    public bool IsGrounded;
    
    
    void UpdateScreenOffset()
    {
        // Smoothly interpolate current offset towards target offset
        currentScreenOffset = Vector2.Lerp(currentScreenOffset, targetScreenOffset, screenOffsetTransitionSpeed * Time.deltaTime);
    }

    void UpdateCamera()
    {
        if (playerCamera != null)
        {
            // Apply rotation based on mouse movement
            Quaternion targetRotation = Quaternion.Euler(rotationX, rotationY, 0f);
            playerCamera.transform.rotation = targetRotation;
            
            // Calculate pivot position with screen offset applied
            Vector3 pivotPosition = CalculateCameraPivotPosition();
            
            // Calculate desired camera position from pivot
            Vector3 desiredCameraPosition = pivotPosition + playerCamera.transform.rotation * cameraOffset;
            
            // Check for obstructions between pivot and camera
            Vector3 directionToCamera = desiredCameraPosition - pivotPosition;
            float desiredDistance = directionToCamera.magnitude;
                RaycastHit hit;
                // Use SphereCast for better collision detection
                if (Physics.SphereCast(pivotPosition, cameraObstructionCheckRadius, directionToCamera.normalized, out hit, desiredDistance))
                {
                    // Something is blocking the camera, move it closer
                    float safeDistance = hit.distance - cameraObstructionCheckRadius * 0.5f;
                    safeDistance = Mathf.Max(safeDistance, 1f); // Minimum distance of 1 unit
                    
                    // Place camera at safe distance
                    playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position,pivotPosition + directionToCamera.normalized * safeDistance,5*Time.deltaTime);
                }
                else
                {
                    // No obstruction, place camera at desired position
                    playerCamera.transform.position = desiredCameraPosition;
                }
            
        }
    }

    void UpdateHeadRotation()
    {
        if (PlayerHead == null || playerCamera == null)
            return;

        // Make head follow camera rotation
        Vector3 cameraForward = playerCamera.transform.forward;
        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
        PlayerHead.transform.rotation = Quaternion.Slerp(
            PlayerHead.transform.rotation,
            targetRotation,
            10f * Time.deltaTime
        );

        // Check angle between head forward and body forward
        Vector3 headForward = PlayerHead.transform.forward;
        Vector3 bodyForward = transform.forward;
        
        // Flatten to horizontal plane for comparison
        headForward.y = 0f;
        bodyForward.y = 0f;
        headForward.Normalize();
        bodyForward.Normalize();

        float angle = Vector3.Angle(bodyForward, headForward);

        // If angle exceeds 45 degrees, update target body rotation
        if (angle > 45f)
        {
            targetBodyRotationY = rotationY;
        }
    }
    
    void UpdateBodyRotation()
    {
        // Smoothly rotate body towards target rotation with delay
        float currentBodyRotationY = transform.eulerAngles.y;
        float smoothRotationY = Mathf.LerpAngle(currentBodyRotationY, targetBodyRotationY, bodyRotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, smoothRotationY, 0f);
    }
    
    /// <summary>
    /// Calculates the camera pivot position with screen offset applied
    /// This is the unified method for all offset calculations
    /// </summary>
    /// <returns>The pivot position for the camera in world space</returns>
    private Vector3 CalculateCameraPivotPosition()
    {
        Vector3 basePosition = transform.position;
        
        // No offset needed if zero
        if (currentScreenOffset == Vector2.zero)
        {
            return basePosition;
        }
        
        // Get camera's right vector (horizontal plane only)
        Vector3 cameraRight = playerCamera.transform.right;
        cameraRight.y = 0f; // Keep horizontal
        cameraRight.Normalize();
        
        Vector3 cameraUp = Vector3.up; // Use world up for vertical offset
        
        // Calculate offset in world space
        Vector3 horizontalOffset = cameraRight * currentScreenOffset.x * screenOffsetDistance;
        Vector3 verticalOffset = cameraUp * currentScreenOffset.y * screenOffsetDistance;
        
        return basePosition + horizontalOffset + verticalOffset;
    }
    
    /// <summary>
    /// Sets the character's position offset in camera view
    /// </summary>
    /// <param name="screenOffset">X = horizontal offset (negative = left, positive = right), Y = vertical offset (negative = down, positive = up)</param>
    public void SetCharacterScreenOffset(Vector2 screenOffset)
    {
        targetScreenOffset = screenOffset;
    }
}
