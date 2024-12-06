using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Camera Smoothing")]
    [SerializeField] private float tiltAmount = 2f;
    [SerializeField] private float tiltSpeed = 4f;
    [SerializeField] private float landingDipAmount = 0.5f;
    [SerializeField] private float landingDipSpeed = 10f;

    [Header("Head Bob")]
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float bobAmplitude = 0.02f;
    [SerializeField] private float runBobMultiplier = 1.5f; // Increased bob when running

    [Header("Landing Shake")]
    [SerializeField] private float minFallDistance = 3f; // Minimum fall distance to trigger shake
    [SerializeField] private float shakeIntensity = 0.2f;
    [SerializeField] private float shakeDuration = 0.2f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private float verticalOffset;
    private float bobTimer;
    private Vector3 originalPosition;
    private bool wasGrounded;
    private float lastGroundedY;
    private float shakeTimer;
    private Vector3 shakeOffset;

    private void Start()
    {
        originalPosition = transform.localPosition;
    }

    public void UpdateCamera(Vector2 movement, bool isGrounded, bool isRunning, float playerY)
    {
        HandleTilt(movement, isRunning);
        HandleHeadBob(movement, isRunning);
        HandleLanding(isGrounded, playerY);
        ApplyCameraEffects();

        wasGrounded = isGrounded;
        if (isGrounded) lastGroundedY = playerY;
    }

    private void HandleTilt(Vector2 movement, bool isRunning)
    {
        // Forward tilt when running
        if (isRunning && movement.magnitude > 0)
        {
            targetRotation.x = -tiltAmount;
        }
        else
        {
            targetRotation.x = 0f;
        }

        currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * tiltSpeed);
    }

    private void HandleHeadBob(Vector2 movement, bool isRunning)
    {
        if (movement.magnitude > 0)
        {
            // Increment bob timer based on movement
            bobTimer += Time.deltaTime * (isRunning ? bobFrequency * runBobMultiplier : bobFrequency);

            // Calculate bob offset
            float bobOffset = Mathf.Sin(bobTimer) * (isRunning ? bobAmplitude * runBobMultiplier : bobAmplitude);
            verticalOffset = bobOffset;
        }
        else
        {
            // Reset bob when not moving
            bobTimer = 0;
            verticalOffset = Mathf.Lerp(verticalOffset, 0, Time.deltaTime * landingDipSpeed);
        }
    }

    private void HandleLanding(bool isGrounded, float playerY)
    {
        if (isGrounded && !wasGrounded)
        {
            float fallDistance = lastGroundedY - playerY;

            // Apply landing dip
            verticalOffset = -landingDipAmount;

            // Check if fall distance warrants screen shake
            if (fallDistance > minFallDistance)
            {
                StartShake();
            }
        }
    }

    private void StartShake()
    {
        shakeTimer = shakeDuration;
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            float progress = shakeTimer / shakeDuration;

            // Calculate random shake offset with decreasing intensity
            shakeOffset = Random.insideUnitSphere * shakeIntensity * progress;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    private void ApplyCameraEffects()
    {
        UpdateShake();

        // Combine all effects
        transform.localRotation = Quaternion.Euler(currentRotation);
        transform.localPosition = originalPosition + new Vector3(0, verticalOffset, 0) + shakeOffset;
    }
}
