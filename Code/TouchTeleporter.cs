using UnityEngine;
using System.Collections;
public class TouchTeleporter : MonoBehaviour
{
    [Header("Teleporter Settings")]
    public Transform teleportDestination;
    public Transform playerToTeleport;
    
    [Header("Player Detection")]
    public string playerTag = "Player";
    public bool requireTriggerEnter = true; // If false, uses continuous collision detection
    
    [Header("Effects")]
    public AudioClip teleportSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    public GameObject teleportParticles;
    
    [Header("Teleport Delay")]
    public float teleportDelay = 0.0f; // Reduced delay for faster response
    private bool canTeleport = true;
    private float teleportCooldown = 0.5f; // Reduced cooldown for faster re-triggering
    private bool isPlayerInside = false; // Track if player is inside trigger
    
    void Start()
    {
        Debug.Log("TouchTeleporter: Start initializing...");

        // Ensure we have a collider set as trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add a box collider if none exists
            col = gameObject.AddComponent<BoxCollider>();
            Debug.Log("TouchTeleporter: Added BoxCollider component");
        }
        col.isTrigger = true;
        
        // Auto-assign player if not set
        if (playerToTeleport == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerToTeleport = player.transform;
                Debug.Log($"TouchTeleporter: Auto-assigned player: {player.name}");
            }
            else
            {
                Debug.LogWarning($"TouchTeleporter: Could not find player with tag '{playerTag}'");
            }
        }
        else
        {
            Debug.Log($"TouchTeleporter: Player already assigned: {playerToTeleport.name}");
        }

        // Log summary of teleporter configuration
        Debug.Log($"TouchTeleporter Configuration:\n" +
                 $"- Destination: {(teleportDestination ? teleportDestination.name : "MISSING!")}\n" +
                 $"- Player: {(playerToTeleport ? playerToTeleport.name : "MISSING!")}");
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = true;
            if (canTeleport)
            {
                TeleportPlayer();
            }
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = true;
            if (!requireTriggerEnter && canTeleport)
            {
                TeleportPlayer();
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = false;
        }
    }
    
    void FixedUpdate()
    {
        // Additional check for fast-moving objects that might miss trigger events
        if (canTeleport && !isPlayerInside && playerToTeleport != null)
        {
            Collider col = GetComponent<Collider>();
            if (col != null && col.bounds.Contains(playerToTeleport.position))
            {
                Debug.Log("TouchTeleporter: Detected fast-moving player, triggering teleport");
                TeleportPlayer();
            }
        }
    }
    
    void TeleportPlayer()
    {
        if (teleportDestination == null || playerToTeleport == null || !canTeleport)
        {
            Debug.LogWarning("TouchTeleporter: Missing destination or player reference!");
            return;
        }
        
        // Prevent rapid teleporting
        canTeleport = false;
        
        // Start teleport process
        StartCoroutine(TeleportProcess());
    }
    
    System.Collections.IEnumerator TeleportProcess()
    {
        // Play sound
        if (teleportSound != null)
        {
            AudioSource.PlayClipAtPoint(teleportSound, transform.position, soundVolume);
        }
        
        // Play particles at current position
        if (teleportParticles != null)
        {
            Instantiate(teleportParticles, playerToTeleport.position, Quaternion.identity);
        }
        
        // Wait for delay
        yield return new WaitForSeconds(teleportDelay);
        
        // Teleport player
        Vector3 oldPosition = playerToTeleport.position;
        playerToTeleport.position = teleportDestination.position;
        playerToTeleport.rotation = teleportDestination.rotation;
        
        Debug.Log($"TouchTeleporter: Teleported player from {oldPosition} to {teleportDestination.position}");
        
        // Play particles at destination
        if (teleportParticles != null)
        {
            Instantiate(teleportParticles, teleportDestination.position, Quaternion.identity);
        }
        
        // Wait for cooldown before allowing another teleport
        yield return new WaitForSeconds(teleportCooldown);
        
        // Reset state
        isPlayerInside = false;
        canTeleport = true;
    }
    
    void OnDrawGizmos()
    {
        // Draw the teleporter area in the scene view
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // Draw line to destination
        if (teleportDestination != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, teleportDestination.position);
            Gizmos.DrawWireSphere(teleportDestination.position, 0.5f);
        }
    }
}
