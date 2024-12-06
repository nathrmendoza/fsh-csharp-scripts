using UnityEngine;
using System.Collections;

public class FatigueEffects : MonoBehaviour
{
    [Header("Effect Thresholds")]
    [SerializeField] private float effectStartThreshold = 0.25f;
    [SerializeField] private float fatigueThreshold = 0.1f;

    [Header("FOV Settings")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float fatigueFOV = 55f;
    [SerializeField] private float fovChangeSpeed = 3f;

    [Header("Breathing Effect")]
    [SerializeField] private float breathingSpeed = 3f;
    [SerializeField] private float maxBreathingIntensity = 2f;
    [SerializeField] private AnimationCurve breathingCurve;
    [SerializeField] private Vector3 breathingRotation = new Vector3(1f, 0.3f, 0.5f);
    [SerializeField] private Vector3 breathingPosition = new Vector3(0f, 0.15f, 0f);
    [SerializeField] private float transitionSpeed = 2f;

    [Header("References")]
    [SerializeField] private Transform breathingHolder;  // Assign in inspector

    private Camera playerCamera;
    private StaminaSystem staminaSystem;
    private float currentEffectIntensity;
    private float targetFOV;
    private Vector3 originalHolderPosition;
    private Quaternion originalHolderRotation;
    private float breathingTimer;
    private bool isBreathingActive;
    private float currentTransitionWeight;

    private void Awake()
    {
        breathingCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 2f),
            new Keyframe(0.3f, 1f, 0f, 0f),
            new Keyframe(0.7f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -2f, -2f)
        );
    }

    private void Start()
    {
        if (breathingHolder == null)
        {
            Debug.LogError("Breathing Holder reference is missing! Please assign it in the inspector.");
            enabled = false;
            return;
        }

        playerCamera = GetComponentInChildren<Camera>();
        staminaSystem = GetComponent<StaminaSystem>();

        StartCoroutine(InitializePositions());

        targetFOV = defaultFOV;
        playerCamera.fieldOfView = defaultFOV;

        staminaSystem.OnStaminaChanged += HandleStaminaChanged;
    }

    private IEnumerator InitializePositions()
    {
        yield return new WaitForEndOfFrame();
        originalHolderPosition = breathingHolder.localPosition;
        originalHolderRotation = breathingHolder.localRotation;
    }

    private void OnDestroy()
    {
        if (staminaSystem != null)
        {
            staminaSystem.OnStaminaChanged -= HandleStaminaChanged;
        }
    }

    private void HandleStaminaChanged(float currentStamina)
    {
        float staminaPercentage = staminaSystem.StaminaPercentage;

        if (staminaPercentage <= effectStartThreshold)
        {
            float range = effectStartThreshold - fatigueThreshold;
            currentEffectIntensity = 1f - ((staminaPercentage - fatigueThreshold) / range);
            currentEffectIntensity = Mathf.Pow(Mathf.Clamp01(currentEffectIntensity), 0.7f);
            isBreathingActive = true;

            targetFOV = Mathf.Lerp(defaultFOV, fatigueFOV, currentEffectIntensity);
        }
        else
        {
            currentEffectIntensity = 0f;
            targetFOV = defaultFOV;
            isBreathingActive = false;
        }
    }

    private void LateUpdate()
    {
        // Smooth transition weight
        float targetWeight = isBreathingActive ? 1f : 0f;
        currentTransitionWeight = Mathf.Lerp(currentTransitionWeight, targetWeight, Time.deltaTime * transitionSpeed);

        // Only apply effects if there's any weight
        if (currentTransitionWeight > 0.001f)
        {
            breathingTimer += Time.deltaTime * breathingSpeed;

            float breathProgress = breathingCurve.Evaluate((Mathf.Sin(breathingTimer) + 1f) * 0.5f);
            float scaledIntensity = currentEffectIntensity * maxBreathingIntensity * currentTransitionWeight;

            Vector3 targetPosition = originalHolderPosition + (breathingPosition * breathProgress * scaledIntensity);
            Quaternion targetRotation = originalHolderRotation * Quaternion.Euler(breathingRotation * breathProgress * scaledIntensity);

            // Apply to breathing holder instead of camera
            breathingHolder.localPosition = targetPosition;
            breathingHolder.localRotation = targetRotation;
        }
        else if (currentTransitionWeight < 0.001f)
        {
            breathingHolder.localPosition = originalHolderPosition;
            breathingHolder.localRotation = originalHolderRotation;
            breathingTimer = 0f;
        }

        // Update FOV
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            Time.deltaTime * fovChangeSpeed
        );
    }
}