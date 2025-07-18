using UnityEngine;
using System.Collections;

public class PlatformRotator : MonoBehaviour
{
    [Header("Button Settings")]
    public KeyCode interactionKey = KeyCode.E;
    public float interactionDistance = 3f;
    public LayerMask playerLayer = 1;
    
    [Header("Button Animation")]
    public float pressDistance = 0.2f;
    public float pressSpeed = 5f;
    public AnimationCurve pressEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Effects")]
    public GameObject particleSystemPrefab;  // Change to GameObject prefab
    public AudioClip buttonSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    private ParticleSystem buttonParticles;
    private GameObject particleInstance;  // Add this to track the instantiated prefab
    
    [Header("Platform Animation")]
    public Transform platformPivot;      // Empty GameObject as rotation center
    public Transform platformToRotate;   // The actual platform you want to rotate
    public float rotationAmount = 90f;
    public float rotationDuration = 3f;
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Debug")]
    public bool enableDebug = true;

    private bool isRotating = false;
    private Vector3 originalButtonPosition;
    private bool playerInRange = false;
    private Camera playerCamera;
    private Quaternion originalPlatformRotation; // Store original rotation
    private int rotationCount = 0; // Track how many times platform has rotated

    void Start()
    {
        originalButtonPosition = transform.position;
        playerCamera = Camera.main;
        
        // Store original platform rotation
        if (platformPivot != null)
        {
            originalPlatformRotation = platformPivot.rotation;
        }

        // Create particle system from prefab
        if (particleSystemPrefab != null)
        {
            particleInstance = Instantiate(particleSystemPrefab, transform.position, Quaternion.identity);
            particleInstance.transform.SetParent(transform);  // Make it child of button
            buttonParticles = particleInstance.GetComponent<ParticleSystem>();
            
            if (buttonParticles == null)
            {
                Debug.LogError("PlatformRotator: Prefab does not contain a ParticleSystem component!");
                Destroy(particleInstance);
            }
            else if (enableDebug)
            {
                Debug.Log("PlatformRotator: Successfully created particle system from prefab");
            }
        }
        else
        {
            Debug.LogWarning("PlatformRotator: No particle system prefab assigned!");
        }

        if (platformToRotate == null)
        {
            Debug.LogError("PlatformRotator: No platform assigned to rotate!");
        }
        if (platformPivot == null)
        {
            Debug.LogError("PlatformRotator: No pivot point assigned!");
        }
    }

    void Update()
    {
        CheckForPlayer();
        
        // Only allow rotation if platform is in default position and not currently rotating
        if (playerInRange && !isRotating && IsPlatformInDefaultPosition())
        {
            if (Input.GetKeyDown(interactionKey))
            {
                if (enableDebug) Debug.Log("PlatformRotator: Button pressed!");
                StartCoroutine(PressButtonAndRotate());
            }
        }
    }

    IEnumerator PressButtonAndRotate()
    {
        Debug.Log("Button pressed, starting sequence...");

        // Play cutscene while platform rotates
        PlatformCutscene cutscene = FindObjectOfType<PlatformCutscene>();
        if (cutscene != null)
        {
            cutscene.PlayPlatformCutscene();
        }
        else
        {
            Debug.LogWarning("PlatformRotator: No PlatformCutscene found in scene!");
        }

        // Play sound
        if (buttonSound != null)
        {
            Debug.Log("Playing sound...");
            // Create temporary AudioSource
            GameObject audioObject = new GameObject("TempAudio");
            audioObject.transform.position = transform.position;
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = buttonSound;
            audioSource.volume = soundVolume;
            audioSource.spatialBlend = 1f;  // Make it 3D sound
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;
            audioSource.Play();

            // Destroy the temporary audio object after playing
            Destroy(audioObject, buttonSound.length + 0.1f);
        }
        else
        {
            Debug.LogError("No button sound clip assigned!");
        }

        // Play particles
        if (buttonParticles != null)
        {
            buttonParticles.Clear();  // Clear any existing particles
            buttonParticles.Play();
            Debug.Log("Playing particle effect...");
        }

        // Button press animation
        yield return StartCoroutine(AnimateButtonPress());

        // Rotate platform
        if (platformPivot != null && platformToRotate != null)
        {
            Debug.Log("Starting platform rotation...");
            yield return StartCoroutine(RotatePlatform());
        }
        else
        {
            Debug.LogError("Missing platform or pivot reference!");
        }
    }

    IEnumerator AnimateButtonPress()
    {
        // Use transform.up for local space movement (along green Y axis)
        Vector3 moveDirection = -transform.up;  // Negative up = pressing downward
        Vector3 pressedPosition = originalButtonPosition + (moveDirection * pressDistance);
        
        // Press downward
        float elapsed = 0f;
        float pressDuration = 1f / pressSpeed;
        
        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressDuration;
            float curveT = pressEasing.Evaluate(t);
            
            transform.position = Vector3.Lerp(originalButtonPosition, pressedPosition, curveT);
            yield return null;
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // Return to original position
        elapsed = 0f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressDuration;
            float curveT = pressEasing.Evaluate(t);
            
            transform.position = Vector3.Lerp(pressedPosition, originalButtonPosition, curveT);
            yield return null;
        }
        
        transform.position = originalButtonPosition;
        
        if (enableDebug) Debug.Log("PlatformRotator: Button press animation complete");
    }

    IEnumerator RotatePlatform()
    {
        if (platformPivot == null) yield break;
        
        isRotating = true;
        Quaternion startRotation = platformPivot.rotation;
        // Change (0, rotationAmount, 0) to (rotationAmount, 0, 0) for X-axis rotation
        // or (0, 0, rotationAmount) for Z-axis rotation
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, rotationAmount);
        
        float elapsed = 0f;
        
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotationDuration;
            float curveT = rotationCurve.Evaluate(t);
            
            platformPivot.rotation = Quaternion.Lerp(startRotation, targetRotation, curveT);
            yield return null;
        }
        
        platformPivot.rotation = targetRotation;
        rotationCount++; // Increment rotation count
        isRotating = false;
        
        if (enableDebug)
        {
            Debug.Log($"Platform rotation complete. Total rotations: {rotationCount}");
            Debug.Log($"Platform in default position: {IsPlatformInDefaultPosition()}");
        }
    }
    
    // Check if platform is in default position (based on rotation count)
    private bool IsPlatformInDefaultPosition()
    {
        if (platformPivot == null) return false;
        
        // Calculate expected rotations for a full 360° cycle
        int rotationsFor360 = Mathf.RoundToInt(360f / Mathf.Abs(rotationAmount));
        
        // Platform is in default position if rotation count is divisible by full cycle
        bool inDefaultPosition = (rotationCount % rotationsFor360) == 0;
        
        if (enableDebug)
        {
            Debug.Log($"Rotation count: {rotationCount}, Rotations for 360°: {rotationsFor360}, In default: {inDefaultPosition}");
        }
        
        return inDefaultPosition;
    }
    
    // Public method to check platform state (for other scripts)
    public bool IsPlatformInDefault()
    {
        return IsPlatformInDefaultPosition();
    }
    
    // Public method to reset platform state (for other scripts)
    public void ResetPlatformState()
    {
        rotationCount = 0;
        if (platformPivot != null)
        {
            platformPivot.rotation = originalPlatformRotation;
        }
    }

    void CheckForPlayer()
    {
        if (playerCamera == null)
        {
            Debug.LogError("No camera found!");
            return;
        }
        
        float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
        
        if (distance <= interactionDistance)
        {
            Vector3 directionToButton = (transform.position - playerCamera.transform.position).normalized;
            float dotProduct = Vector3.Dot(playerCamera.transform.forward, directionToButton);
            
            if (dotProduct > 0.3f)
            {
                if (!playerInRange)
                {
                    playerInRange = true;
                    Debug.Log("Player in range!");
                }
            }
            else
            {
                playerInRange = false;
            }
        }
        else
        {
            playerInRange = false;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        Gizmos.color = Color.blue;
        if (platformPivot != null)
        {
            Gizmos.DrawWireSphere(platformPivot.position, 0.5f);
            Gizmos.DrawLine(platformPivot.position, platformPivot.position + platformPivot.forward * 2);
        }
    }
}