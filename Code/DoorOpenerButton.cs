using UnityEngine;
using System.Collections;

public class DoorOpenerButton : MonoBehaviour
{
    [Header("Rising Button")]
    public GameObject risingButton; // Assign in inspector if used
    [Header("Sign Control")]
    public SignDoor signToControl; // Assign in inspector
    
    [Header("Button Settings")]
    [Header("Platform Reset")]
    public Transform platformToReset; // Assign in inspector (the platform itself)
    public KeyCode interactionKey = KeyCode.E;
    public float interactionDistance = 3f;
    
    [Header("Button Animation")]
    public float pressDistance = 0.2f;
    public float pressSpeed = 5f;
    public AnimationCurve pressEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Effects")]
    public GameObject particleSystemPrefab;
    public AudioClip buttonSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("Doors")]
    public Transform leftDoor;
    public Transform rightDoor;
    public float doorSpeed = 2f;
    public AnimationCurve doorCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Door open/closed positions (updated to new user-provided values)
    private static readonly Vector3 leftDoorOpenPos = new Vector3(-18.6023312f, 2.43730998f, 7.14710045f);
    private static readonly Vector3 rightDoorOpenPos = new Vector3(-18.6023312f, 2.43730998f, 12.9330997f);
    private static readonly Vector3 leftDoorClosedPos = new Vector3(-18.6023312f, 2.43730998f, 9.05500031f);
    private static readonly Vector3 rightDoorClosedPos = new Vector3(-18.6023312f, 2.43730998f, 11.1000004f);
    private const float positionTolerance = 0.01f;

    private ParticleSystem buttonParticles;
    private GameObject particleInstance;
    private Vector3 originalButtonPosition;
    private bool playerInRange = false;
    private Camera playerCamera;
    private bool isAnimating = false;

    void Start()
    {
        originalButtonPosition = transform.position;
        playerCamera = Camera.main;
        if (leftDoor != null) leftDoor.position = leftDoorOpenPos;
        if (rightDoor != null) rightDoor.position = rightDoorOpenPos;

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
        if (playerInRange && AreDoorsClosed() && !isAnimating && Input.GetKeyDown(interactionKey))
        {
            StartCoroutine(OpenDoorsAndUpdateSign());
        }
    }

    IEnumerator OpenDoorsAndUpdateSign()
    {
        // Play sound
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

        // Only open doors if closed and not animating
        if (AreDoorsClosed() && !isAnimating)
        {
            // Hide the rising button if present
            if (risingButton != null) risingButton.SetActive(false);
            yield return StartCoroutine(OpenDoorAnimation());
        }
        // Update sign: hide Closed, show Open
        if (signToControl != null)
        {
            signToControl.ShowOpenSign();
        }

        // Reset platform Z rotation if assigned
        if (platformToReset != null)
        {
            Vector3 euler = platformToReset.localEulerAngles;
            platformToReset.localEulerAngles = new Vector3(euler.x, euler.y, 0f);
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

    IEnumerator OpenDoorAnimation()
    {
        if (leftDoor == null || rightDoor == null || AreDoorsOpen()) yield break;
        isAnimating = true;
        Vector3 leftStart = leftDoor.position;
        Vector3 rightStart = rightDoor.position;
        Vector3 leftTarget = leftDoorOpenPos;
        Vector3 rightTarget = rightDoorOpenPos;
        float elapsed = 0f;
        while (elapsed < doorSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / doorSpeed;
            float curveT = doorCurve.Evaluate(t);
            leftDoor.position = Vector3.Lerp(leftStart, leftTarget, curveT);
            rightDoor.position = Vector3.Lerp(rightStart, rightTarget, curveT);
            yield return null;
        }
        leftDoor.position = leftTarget;
        rightDoor.position = rightTarget;
        isAnimating = false;
    }

    // Helper: are both doors at open positions?
    public bool AreDoorsOpen()
    {
        if (leftDoor == null || rightDoor == null) return false;
        return (Vector3.Distance(leftDoor.position, leftDoorOpenPos) < positionTolerance &&
                Vector3.Distance(rightDoor.position, rightDoorOpenPos) < positionTolerance);
    }

    // Helper: are both doors at closed positions?
    public bool AreDoorsClosed()
    {
        if (leftDoor == null || rightDoor == null) return false;
        return (Vector3.Distance(leftDoor.position, leftDoorClosedPos) < positionTolerance &&
                Vector3.Distance(rightDoor.position, rightDoorClosedPos) < positionTolerance);
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