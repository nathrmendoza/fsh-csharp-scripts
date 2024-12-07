using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionMask;

    private Camera playerCamera;
    private IInteractable currentTarget;
    private PlayerInputActions playerInputActions;

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();

        // Subscribe to the interact action
        playerInputActions.Player.Interact.performed += _ => TryInteract();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    private void Update()
    {
        CheckForInteractable();
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
                }
            }
            else
            {
                currentTarget?.OnLoseFocus();
                currentTarget = null;
            }
        }
        else
        {
            currentTarget?.OnLoseFocus();
            currentTarget = null;
        }
    }

    private void TryInteract()
    {
        if (currentTarget != null && currentTarget.CanInteract)
        {
            currentTarget.OnInteract();
        }
    }
}