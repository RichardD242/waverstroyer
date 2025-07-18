using UnityEngine;
public class SignDoor : MonoBehaviour
{
    // Static tracking of the currently active sign
    private static SignDoor currentlyActiveSign;
    // Public methods for external sign control
    public void ShowOpenSign()
    {
        Debug.Log("SignDoor: ShowOpenSign called");
        DeactivateAllSigns();
        if (openSign != null) openSign.SetActive(true);
        currentSignState = SignState.Open;
        CurrentSignState = SignState.Open;
    }

    public void ShowClosedSign()
    {
        DeactivateAllSigns();
        if (closedSign != null) closedSign.SetActive(true);
        currentSignState = SignState.Closed;
        CurrentSignState = SignState.Closed;
    }

    public void ShowInUseSign()
    {
        Debug.Log("SignDoor: ShowInUseSign called");
        DeactivateAllSigns();
        if (inUseSign != null) inUseSign.SetActive(true);
        currentSignState = SignState.InUse;
        CurrentSignState = SignState.InUse;
    }

    public void DeactivateAllSignsPublic()
    {
        DeactivateAllSigns();
        currentSignState = SignState.None;
        CurrentSignState = SignState.None;
    }
    [Header("Door References")]
    [SerializeField] private Transform door1;
    [SerializeField] private Transform door2;
    [SerializeField] private DoorOpenerButton doorOpenerButton; // Reference to door opener script
    
    [Header("Door Open Detection")]
    [SerializeField] private float door1OpenRotationY = 90f;
    [SerializeField] private float door2OpenRotationY = 90f;
    [SerializeField] private float rotationTolerance = 5f;
    [SerializeField] private bool useDoorOpenerButtonState = true; // Use button state instead of rotation
    
    [Header("Sign GameObjects")]
    [SerializeField] private GameObject openSign;
    [SerializeField] private GameObject closedSign;
    [SerializeField] private GameObject inUseSign;
    [SerializeField] private GameObject errorSign;
    
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private BoxCollider playerDetectionBox; // Make it assignable again
    
    [Header("Sign Transition Settings")]
    [SerializeField] private bool useSmoothTransitions = true;
    
    // Current state tracking
    private bool isPlayerInBox = false;
    private SignState currentSignState = SignState.None;
    private SignState targetSignState = SignState.None;
    
    // Static property for other scripts to access current sign state
    public static SignState CurrentSignState { get; private set; } = SignState.None;
    
    public enum SignState
    {
        None,
        Open,
        Closed,
        InUse,
        Error
    }
    
    void Start()
    {
        // Validate all required components
        if (!ValidateComponents())
        {
            SetSignState(SignState.Error);
            return;
        }
        
        // Set up box collider for player detection
        if (playerDetectionBox != null)
        {
            playerDetectionBox.isTrigger = true;
        }
        else
        {
            // Try to find BoxCollider on this GameObject as fallback
            playerDetectionBox = GetComponent<BoxCollider>();
            if (playerDetectionBox != null)
            {
                playerDetectionBox.isTrigger = true;
            }
            else
            {
                SetSignState(SignState.Error);
                return;
            }
        }
        
        // Initialize all signs as inactive
        DeactivateAllSigns();
        
        // Set initial state to Open (doors start closed, but sign shows what action is available)
        SetSignState(SignState.Open);
        ShowOpenSign(); // Force open sign on start
    }
    
    void Update()
    {
        // No automatic sign state update; sign state is now controlled by button scripts only.
    }
    
    public void DetermineTargetSignState()
    {
        // Priority 1: If player is in box, show "In Use"
        if (isPlayerInBox)
        {
            targetSignState = SignState.InUse;
            return;
        }

        // Priority 2: Check door states
        bool doorsOpen = false;

        if (useDoorOpenerButtonState && doorOpenerButton != null)
        {
            // Use the door opener button state for more accurate tracking
            doorsOpen = doorOpenerButton.AreDoorsOpen();
        }
        else
        {
            // Fall back to rotation-based detection
            bool door1Open = IsDoorOpen(door1, door1OpenRotationY);
            bool door2Open = IsDoorOpen(door2, door2OpenRotationY);
            doorsOpen = door1Open && door2Open;
        }

        if (doorsOpen)
        {
            targetSignState = SignState.Closed;  // When doors are open, show "Closed" (not available)
        }
        else
        {
            targetSignState = SignState.Open;    // When doors are closed, show "Open" (available to open)
        }
    }
    
    private bool IsDoorOpen(Transform door, float openRotationY)
    {
        if (door == null) return false;
        
        float currentRotationY = door.rotation.eulerAngles.y;
        float difference = Mathf.Abs(Mathf.DeltaAngle(currentRotationY, openRotationY));
        
        return difference <= rotationTolerance;
    }
    
    private void SetSignState(SignState newState)
    {
        currentSignState = newState;
        CurrentSignState = newState; // Update static property
        
        if (!useSmoothTransitions)
        {
            // Instant switching
            DeactivateAllSigns();
            ActivateSignForState(newState);
        }
    }
    
    private void HandleSmoothTransitions()
    {
        // Simple fade implementation - you can enhance this with actual fade effects
        DeactivateAllSigns();
        ActivateSignForState(currentSignState);
    }
    
    private void DeactivateAllSigns()
    {
        if (openSign != null) openSign.SetActive(false);
        if (closedSign != null) closedSign.SetActive(false);
        if (inUseSign != null) inUseSign.SetActive(false);
        if (errorSign != null) errorSign.SetActive(false);
    }
    
    private void ActivateSignForState(SignState state)
    {
        GameObject signToActivate = null;
        switch (state)
        {
            case SignState.Open:
                signToActivate = openSign;
                break;
            case SignState.Closed:
                signToActivate = closedSign;
                break;
            case SignState.InUse:
                signToActivate = inUseSign;
                break;
            case SignState.Error:
                signToActivate = errorSign;
                break;
        }
        // Deactivate the previously active sign if it's not this one
        if (currentlyActiveSign != null && currentlyActiveSign != this)
        {
            currentlyActiveSign.DeactivateAllSigns();
        }
        currentlyActiveSign = this;
        if (signToActivate != null)
        {
            signToActivate.SetActive(true);
        }
    }
    
    private bool ValidateComponents()
    {
        bool isValid = true;
        
        if (door1 == null)
        {
            Debug.LogError("SignDoor: Door1 reference is missing!");
            isValid = false;
        }
        
        if (door2 == null)
        {
            Debug.LogError("SignDoor: Door2 reference is missing!");
            isValid = false;
        }
        
        if (playerDetectionBox == null)
        {
            Debug.LogError("SignDoor: Player detection box collider is missing!");
            isValid = false;
        }
        
        if (openSign == null || closedSign == null || inUseSign == null || errorSign == null)
        {
            Debug.LogError("SignDoor: One or more sign GameObjects are missing!");
            isValid = false;
        }
        
        if (useDoorOpenerButtonState && doorOpenerButton == null)
        {
            Debug.LogWarning("SignDoor: Door opener button reference is missing! Falling back to rotation detection.");
        }
        
        return isValid;
    }
    
    // Collision detection for player
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log("SignDoor: Player entered trigger, showing InUse sign");
            isPlayerInBox = true;
            ShowInUseSign();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log("SignDoor: Player exited trigger, showing Open sign");
            isPlayerInBox = false;
            ShowOpenSign();
        }
    }
    
    // Public method to manually check current state (for debugging)
    public SignState GetCurrentSignState()
    {
        return currentSignState;
    }
    
    // Public method to force a state (for debugging or emergencies only)
    public void ForceSignState(SignState state)
    {
        Debug.Log($"SignDoor: Force changing state from {currentSignState} to {state}");
        SetSignState(state);
        // Make sure UI updates immediately
        DeactivateAllSigns();
        ActivateSignForState(state);
    }
}
