using UnityEngine;

public class PlayerGrabSystem : MonoBehaviour
{
    [Header("Grab Settings")]
    [SerializeField] private float grabRange = 3f;
    [SerializeField] private LayerMask grabbableLayer;
    [SerializeField] private float minGrabDistance = 1f;
    [SerializeField] private float maxGrabDistance = 3f;
    [SerializeField] private float scrollSensitivity = 0.1f;

    [Header("References")]
    [SerializeField] private Transform grabPointPrefab;

    private Camera playerCamera;
    private PlayerInputActions playerInputActions;
    private GrabbableObject currentlyHeldObject;
    private Transform grabPoint;
    private float currentGrabDistance;
    private Rigidbody grabPointRb;

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();

        // Create grab point
        grabPoint = Instantiate(grabPointPrefab, transform.position, Quaternion.identity);
        grabPointRb = grabPoint.GetComponent<Rigidbody>();
        grabPointRb.isKinematic = true;

        // Subscribe to input events
        playerInputActions.Player.Interact.performed += _ => TryGrab();
        playerInputActions.Player.Interact.canceled += _ => Release();
        playerInputActions.Player.RotateGrabbable.performed += _ => ForwardThrow();
    }

    private void ForwardThrow()
    {
        if (currentlyHeldObject != null)
        {
            currentlyHeldObject.ForwardThrow();
            currentlyHeldObject = null;  // Clear reference after throwing
        }
    }

    private void Update()
    {
        if (currentlyHeldObject != null)
        {
            // Handle scroll for distance
            float scrollDelta = playerInputActions.Player.Scroll.ReadValue<float>();
            if (scrollDelta != 0)
            {
                currentGrabDistance = Mathf.Clamp(
                    currentGrabDistance + scrollDelta * scrollSensitivity,
                    minGrabDistance,
                    maxGrabDistance
                );
            }

            // Update grab point position
            Vector3 targetPos = playerCamera.transform.position +
                              playerCamera.transform.forward * currentGrabDistance;
            grabPoint.position = targetPos;
        }
    }

    private void TryGrab()
    {
        if (currentlyHeldObject == null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, grabRange, grabbableLayer))
            {
                GrabbableObject grabbable = hit.collider.GetComponent<GrabbableObject>();
                if (grabbable != null && grabbable.CanInteract)
                {
                    currentlyHeldObject = grabbable;
                    currentGrabDistance = Vector3.Distance(playerCamera.transform.position, hit.point);
                    currentGrabDistance = Mathf.Clamp(currentGrabDistance, minGrabDistance, maxGrabDistance);

                    // Position grab point and start grab
                    grabPoint.position = hit.point;
                    currentlyHeldObject.StartGrab(grabPointRb, playerCamera);
                }
            }
        }
    }

    private void Release()
    {
        if (currentlyHeldObject != null)
        {
            currentlyHeldObject.EndGrab();
            currentlyHeldObject = null;
        }
    }

    private void OnDestroy()
    {
        playerInputActions.Disable();
        if (grabPoint != null)
        {
            Destroy(grabPoint.gameObject);
        }
    }
}
