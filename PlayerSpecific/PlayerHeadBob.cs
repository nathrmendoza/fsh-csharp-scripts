using UnityEngine;

public class PlayerHeadBob : MonoBehaviour
{
    [Header("Bob Parameters")]
    [SerializeField] private float walkBobFrequency = 13f;
    [SerializeField] private float runBobFrequency = 20f;
    [SerializeField] private float crouchBobFrequency = 10f;

    [SerializeField] private float walkBobAmount = 0.085f;
    [SerializeField] private float runBobAmount = 0.2f;
    [SerializeField] private float crouchBobAmount = 0.05f;

    [Header("Tilt Parameters")]
    [SerializeField] private float tiltAmount = 0.5f;
    [SerializeField] private float smoothTiltSpeed = 8f;

    [Header("Smoothing")]
    [SerializeField] private float transitionSpeed = 6f;

    private PlayerMovement playerMovement;
    private CharacterController characterController;
    private float defaultYPos;
    private float timer;
    private float currentBobAmount;
    private float currentBobFrequency;
    private float targetTilt;
    private float currentTilt;
    private Vector3 lastPosition;

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        characterController = GetComponentInParent<CharacterController>();
        defaultYPos = transform.localPosition.y;
        lastPosition = transform.parent.position;
    }

    private void Update()
    {
        // Only bob when grounded and moving
        Vector3 horizontalMove = transform.parent.position - lastPosition;
        horizontalMove.y = 0;
        float horizontalSpeed = horizontalMove.magnitude / Time.deltaTime;
        lastPosition = transform.parent.position;

        if (characterController.isGrounded && horizontalSpeed > 0.1f)
        {
            // Update bob parameters based on movement state
            UpdateBobParameters();

            // Calculate head bob
            timer += Time.deltaTime * currentBobFrequency;
            float bobOffset = Mathf.Sin(timer) * currentBobAmount;

            // Calculate tilt based on input direction
            Vector2 input = new Vector2(
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Vertical")
            );

            targetTilt = input.x * tiltAmount;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * smoothTiltSpeed);

            // Apply both bob and tilt
            Vector3 targetPos = new Vector3(
                transform.localPosition.x,
                defaultYPos + bobOffset,
                transform.localPosition.z
            );

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPos,
                Time.deltaTime * transitionSpeed
            );

            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                Quaternion.Euler(0, 0, -currentTilt),
                Time.deltaTime * smoothTiltSpeed
            );
        }
        else
        {
            // Return to default position when not moving
            timer = 0f;
            targetTilt = 0f;

            Vector3 targetPos = new Vector3(
                transform.localPosition.x,
                defaultYPos,
                transform.localPosition.z
            );

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPos,
                Time.deltaTime * transitionSpeed
            );

            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                Quaternion.identity,
                Time.deltaTime * smoothTiltSpeed
            );
        }
    }

    private void UpdateBobParameters()
    {
        // Get movement state from PlayerMovement
        bool isRunning = characterController.velocity.magnitude > playerMovement.WalkSpeed + 0.1f;
        bool isCrouching = characterController.height < playerMovement.StandingHeight - 0.1f;

        if (isCrouching)
        {
            currentBobFrequency = crouchBobFrequency;
            currentBobAmount = crouchBobAmount;
        }
        else if (isRunning)
        {
            currentBobFrequency = runBobFrequency;
            currentBobAmount = runBobAmount;
        }
        else
        {
            currentBobFrequency = walkBobFrequency;
            currentBobAmount = walkBobAmount;
        }
    }
}