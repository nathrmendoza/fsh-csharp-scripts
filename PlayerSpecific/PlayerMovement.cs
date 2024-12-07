using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 1.9f;
    [SerializeField] private float runSpeed = 7.5f;
    [SerializeField] private float runAcceleration = 4.25f;
    [SerializeField] private float runDeceleration = 8f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float backwardSpeedMultiplier = 0.6f;

    [Header("Jump Settings")]
    [SerializeField] private float minJumpForce = 2f;
    [SerializeField] private float maxJumpForce = 4.25f;
    [SerializeField] private float jumpHoldDuration = 0.3f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float fallGravity = -13.25f;
    [SerializeField] private float coyoteTime = 0.2f; //gives players time before falling to press jump

    [Header("Height Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float heightLerpSpeed = 8f;
    [SerializeField] private float cameraStandingHeight = 1.7f;
    [SerializeField] private float cameraCrouchHeight = 0.8f;

    [SerializeField] private Transform cameraTransform;
    private CharacterController characterController;
    private PlayerInputActions playerInputActions;
    private Transform cameraHolder;
    private Vector3 moveDirection;
    private Vector3 verticalVelocity;
    private bool isCrouching;
    private bool isJumping;
    private float currentRunSpeed;
    private float targetSpeed;
    private float jumpHoldTimer;
    private bool isJumpHeld;
    private float currentHeight;
    private float targetCameraHeight;
    private float currentCameraHeight;
    private float coyoteTimeCounter;
    private float lockedMovementSpeed;

    private StaminaSystem staminaSystem;
    private float speedMultiplier = 1f;

    public float WalkSpeed => walkSpeed;
    public float StandingHeight => standingHeight;
    public bool IsJumping => isJumping;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        cameraHolder = transform.GetComponentInChildren<Camera>().transform.parent;
        staminaSystem = GetComponent<StaminaSystem>();

        playerInputActions = new PlayerInputActions();

        currentHeight = standingHeight;
        currentCameraHeight = cameraStandingHeight;
        targetCameraHeight = cameraStandingHeight;

        characterController.height = standingHeight;
        characterController.center = new Vector3(0, standingHeight / 2f, 0);

    }

    private void Start()
    {
        currentRunSpeed = walkSpeed;
        targetSpeed = walkSpeed;
    }

    private void OnEnable() => playerInputActions.Enable();
    private void OnDisable() => playerInputActions.Disable();


    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        HandleJumpAndGravity();
        HandleCrouch();
    }

    private void HandleMovement()
    {
        Vector2 input = playerInputActions.Player.Move.ReadValue<Vector2>();
        bool wantsToRun  = playerInputActions.Player.Run.IsPressed();

        float targetSpeed;
        if (isJumping)
        {
            targetSpeed = lockedMovementSpeed;
        }
        else
        {
            bool canRun = wantsToRun && staminaSystem.CanRun;
            targetSpeed = isCrouching ? crouchSpeed : (canRun ? runSpeed : walkSpeed);
            targetSpeed *= speedMultiplier;
            lockedMovementSpeed = targetSpeed;

            if (canRun && moveDirection.magnitude > 0.1f)
            {
                staminaSystem.ConsumeRunningStamina();
            }
        }

        float accelerationRate = targetSpeed > currentRunSpeed ? runAcceleration : runDeceleration;
        currentRunSpeed = Mathf.Lerp(currentRunSpeed, targetSpeed, Time.deltaTime * accelerationRate);

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        moveDirection = (forward * input.y + right * input.x).normalized;

        //running backwards
        float finalSpeed = currentRunSpeed;
        if (input.y < 0)
        {
            float maxBackwardSpeed = Mathf.Lerp(walkSpeed, runSpeed, backwardSpeedMultiplier);
            finalSpeed = Mathf.Min(currentRunSpeed, maxBackwardSpeed);
        }

        moveDirection *= finalSpeed;

        Vector3 movement = moveDirection + verticalVelocity;
        characterController.Move(movement * Time.deltaTime);
    }

    private void HandleJumpAndGravity()
    {
        bool isGrounded = characterController.isGrounded;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            if (isJumping)
            {
                isJumping = false;
                currentRunSpeed = walkSpeed;
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (playerInputActions.Player.Jump.WasPressedThisFrame())
        {
            if (isCrouching)
            {
                isCrouching = false;
                targetCameraHeight = cameraStandingHeight;
            }
            else if (coyoteTimeCounter > 0 && staminaSystem.CanJump && staminaSystem.TryConsumeJumpStamina())
            {
                verticalVelocity.y = minJumpForce;
                coyoteTimeCounter = 0;
                isJumping = true;
                isJumpHeld = true;
                jumpHoldTimer = 0f;

                lockedMovementSpeed = isCrouching ? crouchSpeed : currentRunSpeed;

            }
        }

        if (isJumping && isJumpHeld)
        {
            if (playerInputActions.Player.Jump.IsPressed() && jumpHoldTimer < jumpHoldDuration)
            {
                jumpHoldTimer += Time.deltaTime;

                // Calculate and apply additional force
                float jumpProgress = jumpHoldTimer / jumpHoldDuration;
                float targetJumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, jumpProgress);
                verticalVelocity.y = targetJumpForce;
            }
            else
            {
                // Jump button released or max hold time reached
                isJumpHeld = false;
            }
        }

        if (!isGrounded)
        {
            float currentGravity = verticalVelocity.y < 0 ? fallGravity : gravity;
            verticalVelocity.y += currentGravity * Time.deltaTime;
        }
        else if (!isJumping)
        {
            verticalVelocity.y = -2f;
        }
    }

    private void HandleCrouch()
    {
        if (playerInputActions.Player.Crouch.WasPressedThisFrame() && !isJumping)
        {
            isCrouching = !isCrouching;
            targetCameraHeight = isCrouching ? cameraCrouchHeight : cameraStandingHeight;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * heightLerpSpeed);
        characterController.height = currentHeight;

        characterController.center = new Vector3(0, currentHeight / 2f, 0);

        currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * heightLerpSpeed);
        UpdateCameraPosition(currentCameraHeight);
    }

    private void UpdateCameraPosition(float height)
    {
        Vector3 cameraPos = cameraHolder.localPosition;
        cameraPos.y = height;
        cameraHolder.localPosition = cameraPos;
    }


    // Add this new method for fatigue system
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

}
