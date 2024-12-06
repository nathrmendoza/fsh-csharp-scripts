using System.Runtime.InteropServices;
using UnityEngine;

public class PlayerLean : MonoBehaviour
{
    [Header("Lean Settings")]
    [SerializeField] private float maxLeanAngle = 15f;
    [SerializeField] private float maxLeanTranslation = 0.5f;
    [SerializeField] private float leanSpeed = 20f;
    [SerializeField] private float scrollSensitivity = 10f;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionOffset = 0.2f;

    [SerializeField] private Transform cameraHolder;
    private PlayerInputActions playerInputActions;
    private float currentLeanAmount;
    private float targetLeanAmount;
    private float defaultZRotation;
    private Vector3 defaultPosition;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        defaultZRotation = cameraHolder.localRotation.eulerAngles.z;
        defaultPosition = cameraHolder.localPosition;
    }

    private void OnEnable() => playerInputActions.Enable();
    private void OnDisable() => playerInputActions.Disable();

    private void Update()
    {
        HandleLeaning();
        ApplyLean();
    }

    private void HandleLeaning()
    {
        float scroll = playerInputActions.Player.LeanScroll.ReadValue<float>();

        if (playerInputActions.Player.LeanLeft.IsPressed())
        {
            targetLeanAmount += scroll * scrollSensitivity * Time.deltaTime;
            targetLeanAmount = Mathf.Clamp(targetLeanAmount, 0f, 1f);
            Debug.Log($"Left Lean - Target: {targetLeanAmount}");
        }

        else if (playerInputActions.Player.LeanRight.IsPressed())
        {
            targetLeanAmount += -scroll * scrollSensitivity * Time.deltaTime;
            targetLeanAmount = Mathf.Clamp(targetLeanAmount, -1f, 0f);
            Debug.Log($"Right Lean - Target: {targetLeanAmount}");
        }

        else
        {
            targetLeanAmount = 0;
        }
    }

    private void ApplyLean()
    {
        currentLeanAmount = Mathf.Lerp(currentLeanAmount, targetLeanAmount, Time.deltaTime * leanSpeed);

        // Calculate desired lean position
        Vector3 leanDirection = -transform.right * Mathf.Sign(currentLeanAmount);
        float desiredLeanDistance = Mathf.Abs(currentLeanAmount * maxLeanTranslation);

        // Start position for raycast (from the camera/head position)
        Vector3 rayStart = transform.position;

        // Check for obstacles
        if (Physics.SphereCast(rayStart, 0.2f, leanDirection, out RaycastHit hit, desiredLeanDistance + collisionOffset, collisionMask))
        {
            // If we hit something, adjust the lean amount
            float adjustedDistance = Mathf.Max(0, hit.distance - collisionOffset);
            float adjustedLeanAmount = (adjustedDistance / maxLeanTranslation) * Mathf.Sign(currentLeanAmount);
            currentLeanAmount = adjustedLeanAmount;

            Debug.DrawLine(rayStart, rayStart + leanDirection * hit.distance, Color.red);
        }
        else
        {
            Debug.DrawLine(rayStart, rayStart + leanDirection * (desiredLeanDistance + collisionOffset), Color.green);
        }

        // Apply rotation and position
        Vector3 targetRotation = new Vector3(
            cameraHolder.localRotation.eulerAngles.x,
            cameraHolder.localRotation.eulerAngles.y,
            defaultZRotation + (currentLeanAmount * maxLeanAngle)
        );

        Vector3 targetPosition = defaultPosition + (-transform.right * (currentLeanAmount * maxLeanTranslation));

        cameraHolder.localRotation = Quaternion.Euler(targetRotation);
        cameraHolder.localPosition = targetPosition;
    }
}

