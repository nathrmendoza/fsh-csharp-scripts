using UnityEngine;
using UnityEngine.UI;

public enum InteractionType
{
    None,           // No interaction possible
    Interact,       // Basic interaction (pointing hand)
    Examine,        // Examining objects (eye)
    Grab           // Grabbable objects (open hand)
}

public class CrosshairSystem : MonoBehaviour
{
    [Header("Crosshair Images")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Sprite interactCursor;    // Pointing hand
    [SerializeField] private Sprite examineCursor;     // Eye
    [SerializeField] private Sprite grabCursor;        // Open hand
    [SerializeField] private Sprite grabHoldCursor;    // Closed hand for when actively grabbing

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionMask;

    private Camera playerCamera;
    private IInteractable currentTarget;
    private PlayerInputActions playerInputActions;
    private bool isGrabbing;

    private void Awake()
    {
        playerCamera = Camera.main;
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();

        playerInputActions.Player.Interact.performed += _ => TryInteract();
        playerInputActions.Player.Interact.canceled += _ => StopInteract();

        // Hide crosshair initially
        crosshairImage.enabled = false;
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    private void Update()
    {
        if (!isGrabbing)
        {
            CheckForInteractable();
        }
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionMask))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null && interactable.CanInteract)
            {
                if (currentTarget != interactable)
                {
                    currentTarget?.OnLoseFocus();
                    currentTarget = interactable;
                    currentTarget.OnFocus();
                    UpdateCrosshairForTarget(currentTarget);
                }
            }
            else
            {
                HideCrosshair();
            }
        }
        else
        {
            HideCrosshair();
        }
    }

    private void UpdateCrosshairForTarget(IInteractable target)
    {
        crosshairImage.enabled = true;

        switch (target.InteractionType)
        {
            case InteractionType.Interact:
                crosshairImage.sprite = interactCursor;
                break;
            case InteractionType.Examine:
                crosshairImage.sprite = examineCursor;
                break;
            case InteractionType.Grab:
                crosshairImage.sprite = isGrabbing ? grabHoldCursor : grabCursor;
                break;
            default:
                crosshairImage.enabled = false;
                break;
        }
    }

    private void TryInteract()
    {
        if (currentTarget != null && currentTarget.CanInteract)
        {
            if (currentTarget.InteractionType == InteractionType.Grab)
            {
                isGrabbing = true;
                crosshairImage.sprite = grabHoldCursor;
            }
            currentTarget.OnInteract();
        }
    }

    private void StopInteract()
    {
        if (isGrabbing)
        {
            isGrabbing = false;
            if (currentTarget != null)
            {
                UpdateCrosshairForTarget(currentTarget);
            }
            else
            {
                HideCrosshair();
            }
        }
    }

    private void HideCrosshair()
    {
        if (currentTarget != null)
        {
            currentTarget.OnLoseFocus();
            currentTarget = null;
        }
        crosshairImage.enabled = false;
    }
}