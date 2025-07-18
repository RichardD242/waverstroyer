
using UnityEngine;
using System.Collections;

public class TeleporterButton : MonoBehaviour
{
    [Header("Rising Button")]
    [Header("Sign Control")]
    public SignDoor signToControl; // Assignable in inspector
    public SignDoor.SignState signStateAfterTeleport = SignDoor.SignState.Open;
    [Header("Button Settings")]
    public KeyCode interactionKey = KeyCode.E;
    public float interactionDistance = 3f;
    
    [Header("Button Animation")]
    public float pressDistance = 0.2f;
    public float pressSpeed = 5f;
    public AnimationCurve pressEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Effects")]
    public GameObject particleSystemPrefab; // Prefab containing the particle system
    public AudioClip buttonSound; // Sound to play when button is pressed
    public float soundVolume = 1f; // Volume for the button sound
    public Transform teleportDestination;
    public Transform playerToTeleport;

    [Header("Door Reference")]
    public DoorOpenerButton doorOpenerButton; // Reference to the door opener button

    [Header("Doors")]
    public Transform leftDoor;
    public Transform rightDoor;
    public float doorMoveDistance = 2f;
    public float doorSpeed = 2f;
    public float doorCloseAmount = 0.8f; // How much doors move towards center (0 = don't move, 1 = fully closed)
    public AnimationCurve doorCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Rising Button")]
    public GameObject risingButton; // Assign in inspector
    public Vector3 risingButtonHiddenPosition;
    public Vector3 risingButtonVisiblePosition;
    public float risingButtonRiseDuration = 1f;
    IEnumerator RaiseRisingButton()
    {
        if (risingButton == null) yield break;
        float elapsed = 0f;
        risingButton.transform.position = risingButtonHiddenPosition;
        risingButton.SetActive(true);
        while (elapsed < risingButtonRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / risingButtonRiseDuration);
            risingButton.transform.position = Vector3.Lerp(risingButtonHiddenPosition, risingButtonVisiblePosition, t);
            yield return null;
        }
        risingButton.transform.position = risingButtonVisiblePosition;
    }

    private ParticleSystem buttonParticles;
    private GameObject particleInstance;
    private Vector3 originalButtonPosition;
    private Vector3 leftDoorStartPos;
    private Vector3 rightDoorStartPos;
    private bool playerInRange = false;
    private Camera playerCamera;

    void Start()
    {
        originalButtonPosition = transform.position;
        playerCamera = Camera.main;

        // Store initial door positions (these are the OPEN positions, updated to new user-provided values)
        leftDoorStartPos = new Vector3(-18.6023312f, 2.43730998f, 7.14710045f);
        rightDoorStartPos = new Vector3(-18.6023312f, 2.43730998f, 12.9330997f);
        if (leftDoor != null) leftDoor.position = leftDoorStartPos;
        if (rightDoor != null) rightDoor.position = rightDoorStartPos;

        // Setup particle system
        if (particleSystemPrefab != null)
        {
            particleInstance = Instantiate(particleSystemPrefab, transform.position, Quaternion.identity);
            particleInstance.transform.SetParent(transform);
            buttonParticles = particleInstance.GetComponent<ParticleSystem>();
        }
    }
    void Update()
    {
        CheckForPlayer();
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            Debug.Log("Teleporter button pressed - starting teleport");
            StartCoroutine(ActivateTeleporter());
        }
    }

    IEnumerator ActivateTeleporter()
    {
        Debug.Log("ActivateTeleporter started");
        
        // Play sound using AudioSource.PlayClipAtPoint (no AudioSource component needed)
        if (buttonSound != null)
        {
            AudioSource.PlayClipAtPoint(buttonSound, transform.position, soundVolume);
        }

        // Play particles
        if (buttonParticles != null)
        {
            buttonParticles.Clear();
            buttonParticles.Play();
        }

        // Animate button press
        yield return StartCoroutine(AnimateButtonPress());

        // Wait for teleport
        yield return new WaitForSeconds(0.5f);

        // Teleport player
        if (teleportDestination != null && playerToTeleport != null)
        {
            Debug.Log($"Teleporting player from {playerToTeleport.position} to {teleportDestination.position}");
            playerToTeleport.position = teleportDestination.position;
            playerToTeleport.rotation = teleportDestination.rotation;
            Debug.Log("Teleportation completed");
        }
        else
        {
            Debug.Log($"Teleport failed - teleportDestination: {teleportDestination != null}, playerToTeleport: {playerToTeleport != null}");
        }

        // Wait a moment after teleport
        yield return new WaitForSeconds(1f);

        // Close the doors directly
        yield return StartCoroutine(CloseDoors());

        // Update sign: hide Open, show Closed
        if (signToControl != null)
        {
            signToControl.ShowClosedSign();
        }

        // Raise the rising button after teleport/door close
        if (risingButton != null)
        {
            StartCoroutine(RaiseRisingButton());
        }

        Debug.Log("ActivateTeleporter completed");
    IEnumerator RaiseRisingButton()
    {
        if (risingButton == null) yield break;
        float elapsed = 0f;
        risingButton.transform.position = risingButtonHiddenPosition;
        risingButton.SetActive(true);
        while (elapsed < risingButtonRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / risingButtonRiseDuration);
            risingButton.transform.position = Vector3.Lerp(risingButtonHiddenPosition, risingButtonVisiblePosition, t);
            yield return null;
        }
        risingButton.transform.position = risingButtonVisiblePosition;
    }
    }

    IEnumerator AnimateButtonPress()
    {
        Vector3 moveDirection = -transform.up;
        Vector3 pressedPosition = originalButtonPosition + (moveDirection * pressDistance);
        
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
    }
    
    IEnumerator CloseDoors()
    {
        if (leftDoor == null || rightDoor == null) yield break;
        // Use the same closed positions as DoorOpenerButton (updated to new user-provided values)
        Vector3 leftTargetPos = new Vector3(-18.6023312f, 2.43730998f, 9.05500031f);
        Vector3 rightTargetPos = new Vector3(-18.6023312f, 2.43730998f, 11.1000004f);
        Vector3 leftStart = leftDoor.position;
        Vector3 rightStart = rightDoor.position;
        float elapsed = 0f;
        float animationDuration = doorSpeed;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            leftDoor.position = Vector3.Lerp(leftStart, leftTargetPos, smoothT);
            rightDoor.position = Vector3.Lerp(rightStart, rightTargetPos, smoothT);
            yield return null;
        }
        leftDoor.position = leftTargetPos;
        rightDoor.position = rightTargetPos;
        // Set the sign state directly if assigned
        if (signToControl != null)
        {
            signToControl.ForceSignState(signStateAfterTeleport);
            Debug.Log($"TeleporterButton: Set sign {signToControl.gameObject.name} to {signStateAfterTeleport}");
        }
    }

    void CheckForPlayer()
    {
        if (playerCamera == null) return;
        
        float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
        
        if (distance <= interactionDistance)
        {
            Vector3 directionToButton = (transform.position - playerCamera.transform.position).normalized;
            float dotProduct = Vector3.Dot(playerCamera.transform.forward, directionToButton);
            
            playerInRange = dotProduct > 0.3f;
        }
        else
        {
            playerInRange = false;
        }
    }
}