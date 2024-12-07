using UnityEngine;
using System.Collections.Generic;

public class GrabbableObject : MonoBehaviour, IInteractable
{
    [Header("Physical Properties")]
    [SerializeField] private float weightInKg = 5f;

    [Header("Joint Settings")]
    [SerializeField] private float springForce = 1000f;
    [SerializeField] private float springDamping = 5f;

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

        positionSamples.Clear();
        lastPosition = transform.position;

        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = grabPoint;

        // Configure movement - slightly looser constraints
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        // Allow free rotation since we control it directly
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        // Softer spring settings
        var drive = new JointDrive
        {
            positionSpring = springForce,
            positionDamper = springDamping,
            maximumForce = springForce * 2f
        };

        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;

        // Stop any existing motion
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        if (joint != null && playerCamera != null)
        {
            // Update rotation to match camera orientation
            Quaternion targetRotation = playerCamera.transform.rotation * offsetRotation;
            transform.rotation = targetRotation;
        }
    }

    private void FixedUpdate()
    {
        if (joint != null)
        {
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

            Destroy(joint);
            rb.useGravity = true;
            rb.linearVelocity = averageVelocity * throwForceMultiplier;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ForwardThrow()
    {
        if (joint != null)
        {
            Vector3 throwDirection = playerCamera.transform.forward;
            float weightedForce = forwardThrowForce / Mathf.Sqrt(weightInKg);

            Destroy(joint);
            rb.useGravity = true;
            rb.linearVelocity = throwDirection * weightedForce;
            rb.angularVelocity = Vector3.zero;
        }
    }
}