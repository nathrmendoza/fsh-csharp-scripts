using Unity.VisualScripting;
using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Step Timing")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.7f;

    [Header("Volume Settings")]
    [SerializeField] private float walkVolume = 0.15f;
    [SerializeField] private float runVolume = 0.25f;
    [SerializeField] private float crouchVolume = 0.05f;
    [SerializeField] private float landingVolume = 0.5f;
    [SerializeField] private float volumeVariation = 0.03f;

    [Header("Sound References")]
    [SerializeField] private AudioClip[] woodSteps;
    [SerializeField] private AudioClip[] concreteSteps;
    [SerializeField] private AudioClip[] metalSteps;

    [Header("Components")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private LayerMask surfaceCheckMask;

    private CharacterController characterController;
    private PlayerMovement playerMovement;
    private float stepTimer;
    private float currentStepInterval;
    private bool wasInAir;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();

        if (footstepSource == null)
        {
            // Create and configure audio source with quieter, more realistic settings
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.spatialBlend = 1f;            // Full 3D sound
            footstepSource.playOnAwake = false;
            footstepSource.volume = 0.2f;                // Reduced master volume
            footstepSource.minDistance = 0.5f;           // Closer min distance
            footstepSource.maxDistance = 10f;            // Reduced max distance
            footstepSource.rolloffMode = AudioRolloffMode.Linear; // Linear falloff
            footstepSource.dopplerLevel = 0f;            // No doppler effect for footsteps
        }
    }

    private void Update()
    {
        HandleLandingSound();

        if (!characterController.isGrounded || playerMovement.IsJumping)
        {
            stepTimer = 0f;
            wasInAir = true;
            return;
        }

        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        // Only play footsteps if moving
        if (characterController.velocity.magnitude > 0.1f)
        {
            // Determine step interval based on movement state
            bool isCrouching = characterController.height < playerMovement.StandingHeight - 0.1f;
            bool isRunning = characterController.velocity.magnitude > playerMovement.WalkSpeed + 0.1f;

            if (isCrouching)
            {
                currentStepInterval = crouchStepInterval;
            }
            else if (isRunning)
            {
                currentStepInterval = runStepInterval;
            }
            else
            {
                currentStepInterval = walkStepInterval;
            }

            stepTimer += Time.deltaTime;

            if (stepTimer >= currentStepInterval)
            {
                PlayFootstepSound(isCrouching, isRunning);
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = currentStepInterval; // Ready to play sound when movement resumes
        }
    }

    private void HandleLandingSound()
    {
        if (wasInAir && characterController.isGrounded && !playerMovement.IsJumping)
        {
            if (characterController.velocity.y < -1f)
            {
                PlayLandingSound();
            }
            wasInAir = false;
        }
    }

    private void PlayFootstepSound(bool isCrouching, bool isRunning)
    {
        // Check surface type
        AudioClip[] currentSurfaceSteps = GetSurfaceSteps();
        if (currentSurfaceSteps == null || currentSurfaceSteps.Length == 0) return;

        // Select random step sound
        AudioClip stepSound = currentSurfaceSteps[Random.Range(0, currentSurfaceSteps.Length)];

        // Determine volume based on movement state
        float baseVolume;
        if (isCrouching)
            baseVolume = crouchVolume;
        else if (isRunning)
            baseVolume = runVolume;
        else
            baseVolume = walkVolume;

        // Add slight volume variation
        float finalVolume = baseVolume + Random.Range(-volumeVariation, volumeVariation);
        finalVolume = Mathf.Clamp(finalVolume, 0.1f, 1f);

        // Play the sound
        footstepSource.pitch = Random.Range(0.95f, 1.05f); // Slight pitch variation
        footstepSource.PlayOneShot(stepSound, finalVolume);
    }

    private void PlayLandingSound()
    {
        AudioClip[] currentSurfaceSteps = GetSurfaceSteps();
        if (currentSurfaceSteps == null || currentSurfaceSteps.Length == 0) return;

        // Use two footstep sounds layered together for more impact
        AudioClip landSound1 = currentSurfaceSteps[Random.Range(0, currentSurfaceSteps.Length)];
        AudioClip landSound2 = currentSurfaceSteps[Random.Range(0, currentSurfaceSteps.Length)];

        // Calculate volume based on fall speed
        float fallImpact = Mathf.Abs(characterController.velocity.y);
        float dynamicVolume = Mathf.Lerp(landingVolume * 0.7f, landingVolume,
            Mathf.InverseLerp(1f, 10f, fallImpact));

        // Add variation
        float finalVolume = dynamicVolume + Random.Range(-volumeVariation, volumeVariation);
        finalVolume = Mathf.Clamp(finalVolume, 0.1f, 1f);

        // Play two slightly different pitched versions of the step sound
        footstepSource.pitch = Random.Range(0.85f, 0.9f);  // Lower pitch for impact
        footstepSource.PlayOneShot(landSound1, finalVolume);

        footstepSource.pitch = Random.Range(0.95f, 1f);    // Slightly higher pitch for detail
        footstepSource.PlayOneShot(landSound2, finalVolume * 0.6f);  // Second sound slightly quieter

        footstepSource.pitch = 1f; // Reset pitch for normal steps
    }

    private AudioClip[] GetSurfaceSteps()
    {
        // Cast ray down to check surface type
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f, surfaceCheckMask))
        {
            // Check surface tag
            switch (hit.collider.tag)
            {
                case "Wood":
                    return woodSteps;
                case "Metal":
                    return metalSteps;
                default:
                    return concreteSteps; // Default surface type
            }
        }

        return concreteSteps; // Default if no surface detected
    }
}