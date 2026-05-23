using UnityEngine;
using System.Collections;

/// <summary>
/// A reusable, robust script that implements IInteractable.
/// Handles smooth door swinging around a hinge, door locks, keycard verification, and HUD feedback.
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Configuration")]
    [Tooltip("The parent transform representing the door hinge. If null, rotates this object directly.")]
    public Transform doorHinge;
    [Tooltip("Is the door starting in an open state?")]
    public bool isOpen = false;
    [Tooltip("Is the door locked?")]
    public bool isLocked = false;
    [Tooltip("Does this door require a keycard to unlock?")]
    public bool requiresKeycard = false;
    [Tooltip("The keycard name checked against the player's inventory.")]
    public string keycardName = "Red Keycard";

    [Header("Rotation Settings")]
    [Tooltip("Target rotation angle when open (relative to closing rotation).")]
    public float openAngle = 90.0f;
    [Tooltip("Lerp/Slerp transition speed for the door swing.")]
    public float rotationSpeed = 3.0f;

    [Header("Linked Door (Optional)")]
    [Tooltip("Another door to toggle simultaneously (e.g. for double doors).")]
    public DoorInteractable linkedDoor;

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Coroutine _rotationCoroutine;
    private string _feedbackMessage = "";
    private float _feedbackTimer = 0.0f;
    private bool _isProcessingLink = false; // Prevents recursive loop on double doors

    private void Start()
    {
        if (doorHinge == null)
        {
            doorHinge = transform;
        }

        // Cache closed and open local rotations
        _closedRotation = doorHinge.localRotation;
        _openRotation = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        // Position door instantly to starting state on boot
        doorHinge.localRotation = isOpen ? _openRotation : _closedRotation;
    }

    private void Update()
    {
        // Countdown timer for locked feedback warning text
        if (_feedbackTimer > 0.0f)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0.0f)
            {
                _feedbackMessage = "";
            }
        }
    }

    /// <summary>
    /// Implements IInteractable.Interact. Toggles open state and checks lock credentials.
    /// </summary>
    public void Interact(GameObject source)
    {
        if (isLocked)
        {
            if (requiresKeycard)
            {
                PlayerInventory inventory = source.GetComponent<PlayerInventory>();
                if (inventory != null && inventory.HasKeycard(keycardName))
                {
                    // Card approved!
                    isLocked = false;
                    requiresKeycard = false;
                    _feedbackMessage = "Access Granted!";
                    _feedbackTimer = 2.0f;
                    Debug.Log("Door: Access granted using " + keycardName + ".");
                    
                    // Automatically unlock linked double door if present
                    if (linkedDoor != null && linkedDoor.isLocked)
                    {
                        linkedDoor.isLocked = false;
                        linkedDoor.requiresKeycard = false;
                    }
                }
                else
                {
                    // Access Denied
                    _feedbackMessage = "Access Denied: Requires " + keycardName + "!";
                    _feedbackTimer = 2.0f;
                    Debug.LogWarning("Door: Access denied. Missing " + keycardName + ".");
                    return;
                }
            }
            else
            {
                // Simple standard locked door
                _feedbackMessage = "Door is Locked!";
                _feedbackTimer = 2.0f;
                Debug.LogWarning("Door: Standard lock is active.");
                return;
            }
        }

        // Toggle open/closed state
        isOpen = !isOpen;

        // Perform swing rotation
        TriggerRotation(isOpen);

        // Double door synchronization
        if (linkedDoor != null && !_isProcessingLink)
        {
            _isProcessingLink = true;
            linkedDoor.isOpen = this.isOpen;
            linkedDoor.TriggerRotation(this.isOpen);
            _isProcessingLink = false;
        }
    }

    /// <summary>
    /// Implements IInteractable.GetInteractPrompt. Swaps states dynamically.
    /// </summary>
    public string GetInteractPrompt()
    {
        // If displaying locked/access-denied warning, override standard prompts
        if (!string.IsNullOrEmpty(_feedbackMessage))
        {
            return _feedbackMessage;
        }

        if (isLocked)
        {
            if (requiresKeycard)
            {
                return "Press E to Unlock [Requires " + keycardName + "]";
            }
            return "Press E to Open [Locked]";
        }

        return isOpen ? "Press E to Close Door" : "Press E to Open Door";
    }

    /// <summary>
    /// Triggers smooth coroutine rotation to target rotation state.
    /// </summary>
    public void TriggerRotation(bool open)
    {
        if (_rotationCoroutine != null)
        {
            StopCoroutine(_rotationCoroutine);
        }

        Quaternion targetRot = open ? _openRotation : _closedRotation;
        _rotationCoroutine = StartCoroutine(SmoothRotate(targetRot));
    }

    private IEnumerator SmoothRotate(Quaternion targetRot)
    {
        while (Quaternion.Angle(doorHinge.localRotation, targetRot) > 0.05f)
        {
            doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, targetRot, Time.deltaTime * rotationSpeed);
            yield return null;
        }
        doorHinge.localRotation = targetRot;
        _rotationCoroutine = null;
    }
}