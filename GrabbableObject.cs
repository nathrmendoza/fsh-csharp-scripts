using UnityEngine;

public class GrabbableObject : MonoBehaviour, IInteractable
{
    [Header("Physical Properties")]
    [SerializeField] private float weightInKg = 5f;

    [Header("Movement Settings")]
    [SerializeField] private float baseMovementSpeed = 25f;
    [SerializeField] private float throwForceMultiplier = 10f;
    [SerializeField] private float maxGrabDistance = 3f;
    [SerializeField] private float minGrabDistance = 1f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float movementDamping = 0.1f;  // Higher value = more stable

    private Rigidbody rb;
    private bool isGrabbed;
    private bool isBeingRotated;
    private float currentGrabDistance;
    private Vector3 lastPosition;
    private Camera playerCamera;
    private Quaternion initialRotation;
    private Vector3 currentVelocity; // For SmoothDamp

    public bool CanInteract { get; private set; } = true;
    public InteractionType InteractionType => InteractionType.Grab;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = weightInKg;

        // Set higher angular drag to reduce unwanted rotation
        rb.angularDamping = 5f;
    }

    public void OnInteract()
    {
        if (!isGrabbed) StartGrab();
    }

    public void OnFocus() { }
    public void OnLoseFocus() { }

    public void StartGrab()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        isGrabbed = true;
        currentGrabDistance = Vector3.Distance(playerCamera.transform.position, transform.position);
        currentGrabDistance = Mathf.Clamp(currentGrabDistance, minGrabDistance, maxGrabDistance);

        rb.useGravity = false;  // Disable gravity while grabbed
        rb.angularVelocity = Vector3.zero;  // Stop any existing rotation
        initialRotation = transform.rotation;
        lastPosition = transform.position;

        // Reset smoothing velocity
        currentVelocity = Vector3.zero;
    }

    public void UpdateGrabPosition(Vector3 targetPosition)
    {
        if (!isGrabbed) return;

        // Check if we're too far from the target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget > maxGrabDistance * 1.5f)
        {
            EndGrab();
            return;
        }

        // Store last position for throw calculation
        lastPosition = transform.position;

        // Calculate weight-based movement speed
        float weightedSpeed = baseMovementSpeed / Mathf.Sqrt(weightInKg);

        // Use SmoothDamp for movement
        Vector3 newPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            movementDamping,
            weightedSpeed  // Add speed limit
        );


        // Update rigidbody position
        rb.MovePosition(newPosition);

        // If not being rotated, stabilize rotation
        if (!isBeingRotated)
        {
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * 5f));
        }
    }

    public void UpdateRotation(Vector2 rotationDelta)
    {
        if (!isGrabbed || !isBeingRotated) return;

        float weightedRotationSpeed = rotationSpeed / Mathf.Sqrt(weightInKg);

        // Calculate rotation
        Quaternion deltaRotation = Quaternion.Euler(
            rotationDelta.y * weightedRotationSpeed,
            -rotationDelta.x * weightedRotationSpeed,
            0
        );

        // Apply rotation directly
        rb.MoveRotation(rb.rotation * deltaRotation);
        initialRotation = rb.rotation; // Update initial rotation to prevent snapping
    }

    public void EndGrab()
    {
        if (!isGrabbed) return;

        isGrabbed = false;
        isBeingRotated = false;
        rb.useGravity = true;

        // Apply throw force
        Vector3 throwVelocity = (transform.position - lastPosition) / Time.deltaTime;
        float throwMultiplier = throwForceMultiplier / Mathf.Sqrt(weightInKg);
        rb.linearVelocity = throwVelocity * throwMultiplier;
    }

    public void SetRotationMode(bool rotating)
    {
        isBeingRotated = rotating;
        if (rotating)
        {
            rb.angularVelocity = Vector3.zero; // Reset any existing rotation
        }
    }

    public void AdjustDistance(float scrollDelta)
    {
        if (!isGrabbed) return;

        currentGrabDistance = Mathf.Clamp(
            currentGrabDistance - scrollDelta,
            minGrabDistance,
            maxGrabDistance
        );
    }

    public Vector3 GetTargetPosition(Vector3 playerPosition, Vector3 lookDirection)
    {
        return playerPosition + lookDirection * currentGrabDistance;
    }
}