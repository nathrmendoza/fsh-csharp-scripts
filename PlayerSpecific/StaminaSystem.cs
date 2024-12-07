using UnityEngine;
using System;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float runStaminaDrain = 15f;           // Stamina drain per second while running
    [SerializeField] private float baseJumpStaminaCost = 20f;       // Base cost for jumping
    [SerializeField] private float jumpStaminaMultiplier = 1.5f;    // Multiplier for consecutive jumps
    [SerializeField] private float jumpCooldownTime = 1f;           // Time before jump multiplier starts resetting
    [SerializeField] private float jumpMultiplierResetRate = 0.5f;  // How fast the multiplier resets per second

    [Header("Regeneration Settings")]
    [SerializeField] private float staminaRegenDelay = 1f;          // Delay before stamina starts regenerating
    [SerializeField] private float normalRegenRate = 20f;           // Normal stamina regen per second
    [SerializeField] private float fatigueRegenRate = 5f;           // Fatigue stamina regen per second

    [Header("Fatigue Settings")]
    [SerializeField] private float fatigueMovementPenalty = 0.5f;   // Movement speed multiplier when fatigued
    [SerializeField] private float fatigueThreshold = 10f;          // Stamina threshold where fatigue kicks in

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    [SerializeField] private Vector2 debugPosition = new Vector2(10, 10);

    // Events
    public event Action OnFatigueStart;
    public event Action OnFatigueEnd;
    public event Action<float> OnStaminaChanged;

    // Properties
    public bool IsFatigued { get; private set; }
    public float CurrentStamina { get; private set; }
    public float StaminaPercentage => CurrentStamina / maxStamina;
    public bool CanRun => !IsFatigued && CurrentStamina > fatigueThreshold;
    public bool CanJump => !IsFatigued && CurrentStamina >= GetCurrentJumpCost();

    // Private variables
    private float lastStaminaUseTime;
    private float currentJumpMultiplier = 1f;
    private float lastJumpTime;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        CurrentStamina = maxStamina;
    }

    private void Update()
    {
        HandleJumpMultiplierReset();
        HandleStaminaRegeneration();
        UpdateFatigueState();
    }

    private void HandleJumpMultiplierReset()
    {
        if (Time.time - lastJumpTime > jumpCooldownTime)
        {
            currentJumpMultiplier = Mathf.MoveTowards(
                currentJumpMultiplier,
                1f,
                jumpMultiplierResetRate * Time.deltaTime
            );
        }
    }

    private void HandleStaminaRegeneration()
    {
        if (Time.time - lastStaminaUseTime < staminaRegenDelay) return;

        float regenRate = IsFatigued ? fatigueRegenRate : normalRegenRate;
        ModifyStamina(regenRate * Time.deltaTime);
    }

    private void UpdateFatigueState()
    {
        if (!IsFatigued && CurrentStamina <= fatigueThreshold)
        {
            EnterFatigueState();
        }
        else if (IsFatigued && CurrentStamina > fatigueThreshold * 2) // Hysteresis to prevent rapid state changes
        {
            ExitFatigueState();
        }
    }

    private void EnterFatigueState()
    {
        IsFatigued = true;
        OnFatigueStart?.Invoke();
        ApplyFatigueEffects();
    }

    private void ExitFatigueState()
    {
        IsFatigued = false;
        OnFatigueEnd?.Invoke();
        RemoveFatigueEffects();
    }

    private void ApplyFatigueEffects()
    {
        // Apply movement penalties
        playerMovement.SetSpeedMultiplier(fatigueMovementPenalty);
    }

    private void RemoveFatigueEffects()
    {
        // Remove movement penalties
        playerMovement.SetSpeedMultiplier(1f);
    }

    public void ConsumeRunningStamina()
    {
        ModifyStamina(-runStaminaDrain * Time.deltaTime);
    }

    public bool TryConsumeJumpStamina()
    {
        float cost = GetCurrentJumpCost();
        if (CurrentStamina >= cost)
        {
            ModifyStamina(-cost);
            currentJumpMultiplier *= jumpStaminaMultiplier;
            lastJumpTime = Time.time;
            return true;
        }
        return false;
    }

    private float GetCurrentJumpCost()
    {
        return baseJumpStaminaCost * currentJumpMultiplier;
    }

    private void ModifyStamina(float amount)
    {
        if (amount < 0)
        {
            lastStaminaUseTime = Time.time;
        }

        float newStamina = Mathf.Clamp(CurrentStamina + amount, 0, maxStamina);

        if (newStamina != CurrentStamina)
        {
            CurrentStamina = newStamina;
            OnStaminaChanged?.Invoke(CurrentStamina);
        }
    }
    private void OnGUI()
    {
        if (!showDebug) return;

        GUILayout.BeginArea(new Rect(debugPosition.x, debugPosition.y, 200, 200));
        GUI.backgroundColor = Color.black;
        GUI.contentColor = Color.white;

        GUILayout.BeginVertical("box");
        GUILayout.Label($"Stamina: {CurrentStamina:F1} / {maxStamina}");
        GUILayout.Label($"Stamina %: {(StaminaPercentage * 100):F1}%");
        GUILayout.Label($"Fatigue State: {(IsFatigued ? "YES" : "NO")}");
        GUILayout.Label($"Can Run: {(CanRun ? "YES" : "NO")}");
        GUILayout.Label($"Can Jump: {(CanJump ? "YES" : "NO")}");
        GUILayout.Label($"Jump Cost: {GetCurrentJumpCost():F1}");
        GUILayout.Label($"Jump Multiplier: {currentJumpMultiplier:F2}x");
        GUILayout.EndVertical();

        GUILayout.EndArea();
    }
}