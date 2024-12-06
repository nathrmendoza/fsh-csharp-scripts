using UnityEngine;

public class PlayerStepSmoothing : MonoBehaviour
{
    [Header("Step Detection")]
    [SerializeField] private float stepHeight = 0.35f;
    [SerializeField] private float stepSearchDistance = 0.3f;
    [SerializeField] private float stepForce = 25f;
    [SerializeField] private LayerMask stepDetectionLayers;

    [Header("Probe Configuration")]
    [SerializeField] private float frontOffset = 0.2f;
    [SerializeField] private float probeWidth = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private Color hitColor = Color.green;
    [SerializeField] private Color missColor = Color.red;

    private CharacterController characterController;
    private PlayerMovement playerMovement;
    private float currentStepOffset;
    private bool isStepping;
    private Vector3[] probePoints;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        characterController.stepOffset = 0.01f;

        // Initialize three probe points (left, center, right)
        probePoints = new Vector3[]
        {
            new Vector3(-probeWidth, 0, frontOffset),
            new Vector3(0, 0, frontOffset),
            new Vector3(probeWidth, 0, frontOffset)
        };
    }

    private void Update()
    {
        if (!characterController.isGrounded || playerMovement.IsJumping)
        {
            isStepping = false;
            return;
        }

        HandleStepDetection();
    }

    private void HandleStepDetection()
    {
        Vector3 moveDirection = characterController.velocity.normalized;
        moveDirection.y = 0;

        if (moveDirection.magnitude < 0.1f)
        {
            isStepping = false;
            return;
        }

        float detectedStepHeight = DetectStepHeight(moveDirection);

        if (detectedStepHeight > 0 && detectedStepHeight <= stepHeight)
        {
            if (!isStepping)
            {
                isStepping = true;
            }

            // Add extra height to help clear the step
            float targetStepHeight = detectedStepHeight + 0.05f;

            // More aggressive vertical movement
            Vector3 stepUpMove = Vector3.up * targetStepHeight;
            characterController.Move(stepUpMove * Time.deltaTime * stepForce);

            // Add forward movement to help clear the step
            Vector3 pushForward = moveDirection * (detectedStepHeight * 0.5f);
            characterController.Move(pushForward * Time.deltaTime * stepForce);
        }
        else
        {
            isStepping = false;
        }
    }

    private float DetectStepHeight(Vector3 moveDirection)
    {
        float maxStepHeight = 0f;
        Vector3 basePosition = transform.position;
        Quaternion rotation = Quaternion.LookRotation(moveDirection);

        foreach (Vector3 probePoint in probePoints)
        {
            // Transform probe point based on movement direction
            Vector3 rotatedProbe = rotation * probePoint;
            Vector3 probePosition = basePosition + rotatedProbe;

            // Lower ray
            if (Physics.Raycast(probePosition, Vector3.down, out RaycastHit groundHit, 1f, stepDetectionLayers))
            {
                Vector3 lowerPoint = groundHit.point + Vector3.up * 0.05f;

                // Forward ray at foot level
                if (Physics.Raycast(lowerPoint, moveDirection, out RaycastHit forwardLowHit, stepSearchDistance, stepDetectionLayers))
                {
                    // Upper ray from collision point
                    Vector3 upperOrigin = forwardLowHit.point + Vector3.up * stepHeight;
                    if (!Physics.Raycast(upperOrigin, moveDirection, stepSearchDistance, stepDetectionLayers))
                    {
                        // Downward ray to find actual step height
                        Vector3 topCheckPoint = forwardLowHit.point + (Vector3.up * stepHeight) + (moveDirection * 0.1f);
                        if (Physics.Raycast(topCheckPoint, Vector3.down, out RaycastHit heightHit, stepHeight, stepDetectionLayers))
                        {
                            float stepHeight = heightHit.point.y - forwardLowHit.point.y;
                            maxStepHeight = Mathf.Max(maxStepHeight, stepHeight);

                            if (showDebugRays)
                            {
                                // Draw successful detection
                                Debug.DrawLine(lowerPoint, forwardLowHit.point, hitColor);
                                Debug.DrawLine(upperOrigin, upperOrigin + moveDirection * stepSearchDistance, hitColor);
                                Debug.DrawLine(topCheckPoint, heightHit.point, hitColor);
                            }
                        }
                    }
                    else if (showDebugRays)
                    {
                        // Draw failed upper check
                        Debug.DrawLine(lowerPoint, forwardLowHit.point, missColor);
                        Debug.DrawLine(upperOrigin, upperOrigin + moveDirection * stepSearchDistance, missColor);
                    }
                }
                else if (showDebugRays)
                {
                    // Draw failed forward check
                    Debug.DrawLine(lowerPoint, lowerPoint + moveDirection * stepSearchDistance, missColor);
                }
            }
        }

        return maxStepHeight;
    }
}