using UnityEngine;

public class GrabbableObject : MonoBehaviour, IInteractable
{
    [Header("Physical Properties")]
    [SerializeField] private float weightInKg = 5f;

    [Header("Joint Settings")]
    [SerializeField] private float springForce = 2000f;
    [SerializeField] private float springDamping = 50f;

    [Header("Throw Settings")]
    [SerializeField] private float throwForceMultiplier = 2f;
    [SerializeField] private float forwardThrowForce = 10f;

    private Rigidbody rb;
    private ConfigurableJoint joint;
    private Vector3 previousPosition;
    private Camera playerCamera;
    private float velocityTrackingTimer;

    public bool CanInteract { get; private set; } = true;
    public InteractionType InteractionType => InteractionType.Grab;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = weightInKg;
    }

    private void FixedUpdate()
    {
        if (joint != null)
        {
            // Track position for throw velocity
            previousPosition = transform.position;
        }
    }

    public void OnInteract() { }
    public void OnFocus() { }
    public void OnLoseFocus() { }

    public void StartGrab(Rigidbody grabPoint, Camera playerCam)
    {
        playerCamera = playerCam;
        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = grabPoint;

        // Lock all rotations
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        // Configure movement
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        // Configure spring forces
        var drive = new JointDrive
        {
            positionSpring = springForce / weightInKg,
            positionDamper = springDamping,
            maximumForce = Mathf.Infinity
        };

        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;

        // Store initial position
        previousPosition = transform.position;

        // Configure rigidbody
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

     public void ForwardThrow()
    {
        if (joint != null)
        {
            // Calculate base throw velocity
            Vector3 throwDirection = playerCamera.transform.forward;

            // Apply weight-based throw force
            float weightedForce = forwardThrowForce / Mathf.Sqrt(weightInKg);

            // Clean up joint
            Destroy(joint);

            // Apply throw force
            rb.useGravity = true;
            rb.linearVelocity = throwDirection * weightedForce;
        }
    }

    public void EndGrab()
    {
        if (joint != null)
        {
            // Calculate throw velocity
            Vector3 velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;

            // Clean up joint
            Destroy(joint);

            // Apply throw force
            rb.useGravity = true;
            rb.linearVelocity = velocity * throwForceMultiplier / Mathf.Sqrt(weightInKg);
        }
    }
}
