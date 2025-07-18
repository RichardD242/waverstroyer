using UnityEngine;
using System.Collections;

public class TeleporterCutscene : MonoBehaviour
{
    [Header("Teleporter Settings")]
    public Transform teleportDestination;
    public LayerMask playerLayer = 1; // Which layer the player is on
    
    [Header("Cutscene Settings")]
    public bool enableCutscene = true;
    public float cutsceneDuration = 3f;
    public Transform cutsceneCamera; // Optional separate camera for cutscene
    public AnimationCurve cameraMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Camera Movement")]
    public Vector3 cutsceneStartPos = new Vector3(0, 10, -10);
    public Vector3 cutsceneEndPos = new Vector3(0, 5, 0);
    public bool lookAtPlayer = true;
    
    [Header("Effects")]
    public GameObject teleportEffect; // Optional particle effect
    public AudioClip teleportSound;
    
    private PlayerMovement playerController;
    private Camera playerCamera;
    private Camera cutsceneCam;
    private AudioSource audioSource;
    private bool isPlaying = false;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Get player camera
        if (Camera.main != null)
        {
            playerCamera = Camera.main;
        }
        
        // Setup cutscene camera if provided
        if (cutsceneCamera != null)
        {
            cutsceneCam = cutsceneCamera.GetComponent<Camera>();
            if (cutsceneCam != null)
            {
                cutsceneCam.enabled = false;
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (((1 << other.gameObject.layer) & playerLayer) != 0 && !isPlaying)
        {
            // Get player controller
            playerController = other.GetComponent<PlayerMovement>();
            
            if (enableCutscene)
            {
                StartCoroutine(PlayTeleportCutscene(other.transform));
            }
            else
            {
                // Just teleport without cutscene
                TeleportPlayer(other.transform);
            }
        }
    }
    
    IEnumerator PlayTeleportCutscene(Transform player)
    {
        isPlaying = true;
        
        // Disable player movement
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Play teleport sound
        if (audioSource != null && teleportSound != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }
        
        // Show teleport effect
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, transform.position, Quaternion.identity);
        }
        
        // Switch to cutscene camera
        if (cutsceneCam != null)
        {
            playerCamera.enabled = false;
            cutsceneCam.enabled = true;
            
            // Animate cutscene camera
            StartCoroutine(AnimateCutsceneCamera(player));
        }
        
        // Wait for cutscene to finish
        yield return new WaitForSeconds(cutsceneDuration);
        
        // Teleport player
        TeleportPlayer(player);
        
        // Switch back to player camera
        if (cutsceneCam != null)
        {
            cutsceneCam.enabled = false;
            playerCamera.enabled = true;
        }
        
        // Re-enable player movement
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        isPlaying = false;
    }
    
    IEnumerator AnimateCutsceneCamera(Transform player)
    {
        if (cutsceneCamera == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < cutsceneDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cutsceneDuration;
            float curveValue = cameraMoveCurve.Evaluate(t);
            
            // Move camera
            cutsceneCamera.position = Vector3.Lerp(cutsceneStartPos, cutsceneEndPos, curveValue);
            
            // Look at player
            if (lookAtPlayer && player != null)
            {
                cutsceneCamera.LookAt(player);
            }
            
            yield return null;
        }
    }
    
    void TeleportPlayer(Transform player)
    {
        if (teleportDestination != null)
        {
            // Teleport player
            player.position = teleportDestination.position;
            player.rotation = teleportDestination.rotation;
            
            // Show effect at destination
            if (teleportEffect != null)
            {
                Instantiate(teleportEffect, teleportDestination.position, Quaternion.identity);
            }
            
            Debug.Log("Player teleported to: " + teleportDestination.position);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw teleporter trigger area
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // Draw line to destination
        if (teleportDestination != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, teleportDestination.position);
            Gizmos.DrawWireSphere(teleportDestination.position, 0.5f);
        }
        
        // Draw cutscene camera positions
        if (cutsceneCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cutsceneStartPos, 0.3f);
            Gizmos.DrawWireSphere(cutsceneEndPos, 0.3f);
            Gizmos.DrawLine(cutsceneStartPos, cutsceneEndPos);
        }
    }
}
