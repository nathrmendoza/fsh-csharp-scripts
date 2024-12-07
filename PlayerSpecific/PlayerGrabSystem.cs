using UnityEngine;

public class PlayerGrabSystem : MonoBehaviour
{
    [Header("Grab Settings")]
    [SerializeField] private float grabRange = 3f;
    [SerializeField] private LayerMask grabbableLayer;
    [SerializeField] private float scrollSensitivity = 0.1f;

    private Camera playerCamera;
    private PlayerInputActions playerInputActions;
    private GrabbableObject currentlyHeldObject;

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();

        // Subscribe to input events
        playerInputActions.Player.Interact.performed += _ => TryGrab();
        playerInputActions.Player.Interact.canceled += _ => Release();
        playerInputActions.Player.RotateGrabbable.performed += _ => StartRotating();
        playerInputActions.Player.RotateGrabbable.canceled += _ => StopRotating();
    }

    private void OnDestroy()
    {
        playerInputActions.Disable();
    }

    private void Update()
    {
        if (currentlyHeldObject != null)
        {
            // Handle scroll for distance
            float scrollDelta = playerInputActions.Player.Scroll.ReadValue<float>();
            if (scrollDelta != 0)
            {
                currentlyHeldObject.AdjustDistance(scrollDelta * scrollSensitivity);
            }

            // Handle rotation
            if (playerInputActions.Player.RotateGrabbable.IsPressed())
            {
                Vector2 lookDelta = playerInputActions.Player.Look.ReadValue<Vector2>();
                currentlyHeldObject.UpdateRotation(lookDelta);
            }

            // Update object position
            Vector3 targetPos = currentlyHeldObject.GetTargetPosition(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

            currentlyHeldObject.UpdateGrabPosition(targetPos);
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
                    currentlyHeldObject.StartGrab();
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

    private void StartRotating()
    {
        if (currentlyHeldObject != null)
        {
            currentlyHeldObject.SetRotationMode(true);
        }
    }

    private void StopRotating()
    {
        if (currentlyHeldObject != null)
        {
            currentlyHeldObject.SetRotationMode(false);
        }
    }
}