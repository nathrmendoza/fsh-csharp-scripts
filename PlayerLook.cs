using Unity.VisualScripting;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Range(0.01f, 0.1f)] private float sensitivity = 0.1f;
    [SerializeField] private float maxLookAngle = 90f;

    private PlayerInputActions playerInputActions;
    private float currentXRotation;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable() => playerInputActions.Enable();
    private void OnDisable() => playerInputActions.Disable();

    private void Update()
    {
        Vector2 lookDelta = playerInputActions.Player.Look.ReadValue<Vector2>();

        transform.Rotate(Vector3.up * lookDelta.x * sensitivity);

        currentXRotation -= lookDelta.y * sensitivity;
        currentXRotation = Mathf.Clamp(currentXRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(currentXRotation, 0, 0);
    }
}
