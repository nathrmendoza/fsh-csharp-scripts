using UnityEngine;
using System.Collections.Generic;

public class GrabbableObject : MonoBehaviour, IInteractable
{
    [Header("Physical Properties")]
    [SerializeField] private float weightInKg = 5f;

    [Header("Joint Settings")]
    [SerializeField] private float springForce = 3000f;     // Increased for snappier response
    [SerializeField] private float springDamping = 10f;     // Adjusted for balance

    [Header("Throw Settings")]
    [SerializeField] private float throwForceMultiplier = 2f;
    [SerializeField] private float forwardThrowForce = 10f;
    [SerializeField] private int velocitySamples = 5;

    private Rigidbody rb;
    private ConfigurableJoint joint;
    private Camera playerCamera;
    private Quaternion offsetRotation;
    private Queue<Vector3> positionSamples;
    private Vector3 lastPosition;
    private float fixedTimeStep;
    private Quaternion initialJointRotation;
    private Quaternion targetRotation;

    public bool CanInteract { get; private set; } = true;
    public InteractionType InteractionType => InteractionType.Grab;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = weightInKg;
        positionSamples = new Queue<Vector3>();
        fixedTimeStep = Time.fixedDeltaTime;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void OnInteract() { }
    public void OnFocus() { }
    public void OnLoseFocus() { }

    public void StartGrab(Rigidbody grabPoint, Camera playerCam)
    {
        playerCamera = playerCam;
        offsetRotation = Quaternion.Inverse(playerCam.transform.rotation) * transform.rotation;
        targetRotation = transform.rotation;

        positionSamples.Clear();
        lastPosition = transform.position;

        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = grabPoint;

        // More restrictive movement for accuracy
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        // Stronger drive for more responsive movement
        var drive = new JointDrive
        {
            positionSpring = springForce,
            positionDamper = springDamping,
            maximumForce = springForce * 2f
        };

        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;

        // Lock current rotation
        joint.configuredInWorldSpace = true;
        initialJointRotation = transform.rotation;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        if (joint != null && playerCamera != null)
        {
            // Direct rotation control
            // Calculate and store target rotation
            targetRotation = playerCamera.transform.rotation * offsetRotation;
            transform.rotation = targetRotation;
        }
    }

    private void FixedUpdate()
    {
        if (joint != null)
        {
            // Track positions for throw velocity
            positionSamples.Enqueue(transform.position);
            if (positionSamples.Count > velocitySamples)
            {
                positionSamples.Dequeue();
            }
            lastPosition = transform.position;
        }
    }

    public void EndGrab()
    {
        if (joint != null)
        {
            Vector3 averageVelocity = Vector3.zero;
            if (positionSamples.Count >= 2)
            {
                Vector3 oldestPos = positionSamples.Peek();
                Vector3 currentPos = transform.position;
                float timeSpan = fixedTimeStep * (positionSamples.Count - 1);
                averageVelocity = (currentPos - oldestPos) / timeSpan;
            }

            // Get current state before destroying joint
            Quaternion finalRotation = transform.rotation;
            Vector3 finalVelocity = averageVelocity * throwForceMultiplier;

            // Clean up joint
            rb.rotation = targetRotation;
            Destroy(joint);

            // Apply final state
            rb.useGravity = true;
            transform.rotation = finalRotation;
            rb.linearVelocity = finalVelocity;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ForwardThrow()
    {
        if (joint != null)
        {
            // Get current state
            rb.rotation = targetRotation;
            Quaternion finalRotation = transform.rotation;
            Vector3 throwVelocity = playerCamera.transform.forward * (forwardThrowForce / Mathf.Sqrt(weightInKg));

            // Clean up joint
            Destroy(joint);

            // Apply final state
            rb.useGravity = true;
            transform.rotation = finalRotation;
            rb.linearVelocity = throwVelocity;
            rb.angularVelocity = Vector3.zero;
        }
    }
}