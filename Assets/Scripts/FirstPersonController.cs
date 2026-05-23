using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [Tooltip("Base speed when walking.")]
    public float walkSpeed = 5.0f;
    [Tooltip("Speed multiplier when sprinting.")]
    public float sprintMultiplier = 1.6f;
    [Tooltip("Speed multiplier when crouching.")]
    public float crouchMultiplier = 0.5f;

    [Header("Camera & Look Settings")]
    [Tooltip("Sensitivity of mouse looking.")]
    public float mouseSensitivity = 2.0f;
    [Tooltip("Minimum angle the player can look down.")]
    public float minLookAngle = -80.0f;
    [Tooltip("Maximum angle the player can look up.")]
    public float maxLookAngle = 80.0f;
    [Tooltip("Reference to the player's camera.")]
    public Transform playerCamera;

    [Header("Physics & Gravity")]
    [Tooltip("Acceleration due to gravity.")]
    public float gravity = -9.81f;
    [Tooltip("Force of jumping.")]
    public float jumpHeight = 1.5f;

    [Header("Crouch Settings")]
    [Tooltip("Height of the character controller when standing.")]
    public float standingHeight = 2.0f;
    [Tooltip("Height of the character controller when crouching.")]
    public float crouchHeight = 1.0f;
    [Tooltip("Speed at which the character controller height transitions.")]
    public float crouchTransitionSpeed = 8.0f;

    private CharacterController _controller;
    private Vector3 _velocity;
    private float _verticalRotation = 0.0f;
    private bool _isCrouching = false;
    private float _originalCameraY;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();

        // If no camera reference is manually set, try to find one in the children
        if (playerCamera == null)
        {
            Camera childCam = GetComponentInChildren<Camera>();
            if (childCam != null)
            {
                playerCamera = childCam.transform;
            }
            else
            {
                Debug.LogError("FirstPersonController requires a camera reference or a Camera component as a child.");
            }
        }

        if (playerCamera != null)
        {
            _originalCameraY = playerCamera.localPosition.y;
        }

        // Lock cursor to the center of the screen and hide it for clean playtesting
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCrouch();
    }

    /// <summary>
    /// Handles horizontal body rotation (yaw) and vertical camera look rotation (pitch).
    /// Works with both legacy Input Manager and the modern Input System package.
    /// </summary>
    private void HandleMouseLook()
    {
        if (playerCamera == null) return;

        float mouseX = 0f;
        float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
        {
            var mouseDelta = mouse.delta.ReadValue();
            // Mouse System delta is in pixels, so we apply a standard 0.05f scaling factor
            mouseX = mouseDelta.x * mouseSensitivity * 0.05f;
            mouseY = mouseDelta.y * mouseSensitivity * 0.05f;
        }
#else
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
#endif

        // Pitch rotation (up/down) - clamped to prevent flipping upside down
        _verticalRotation -= mouseY;
        _verticalRotation = Mathf.Clamp(_verticalRotation, minLookAngle, maxLookAngle);
        playerCamera.localRotation = Quaternion.Euler(_verticalRotation, 0.0f, 0.0f);

        // Yaw rotation (left/right) - rotates the entire player body
        transform.Rotate(Vector3.up * mouseX);
    }

    /// <summary>
    /// Handles standard WASD movement, modifiers (sprint/crouch), jumping, and gravity.
    /// Works with both legacy Input Manager and the modern Input System package.
    /// </summary>
    private void HandleMovement()
    {
        bool isGrounded = _controller.isGrounded;
        if (isGrounded && _velocity.y < 0)
        {
            // Keep player firmly grounded by applying a small continuous downward force
            _velocity.y = -2.0f;
        }

        float moveX = 0f;
        float moveZ = 0f;
        bool isSprinting = false;
        bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) moveZ += 1f;
            if (keyboard.sKey.isPressed) moveZ -= 1f;
            if (keyboard.dKey.isPressed) moveX += 1f;
            if (keyboard.aKey.isPressed) moveX -= 1f;

            isSprinting = keyboard.leftShiftKey.isPressed;
            _isCrouching = keyboard.leftCtrlKey.isPressed;
            jumpPressed = keyboard.spaceKey.wasPressedThisFrame;
        }
#else
        moveX = Input.GetAxis("Horizontal");
        moveZ = Input.GetAxis("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            _isCrouching = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            _isCrouching = false;
        }

        jumpPressed = Input.GetButtonDown("Jump");
#endif

        // Calculate direction relative to the player's current orientation
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        
        // Normalize vector to ensure uniform movement speed in diagonal directions
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        // Calculate speed with active modifiers
        float speed = walkSpeed;
        if (_isCrouching)
        {
            speed *= crouchMultiplier;
        }
        else if (isSprinting)
        {
            speed *= sprintMultiplier;
        }

        // Move the CharacterController along horizontal plane
        _controller.Move(move * speed * Time.deltaTime);

        // Handle Jump input
        if (jumpPressed && isGrounded)
        {
            // Physics formula for jumping: v = sqrt(2 * g * h)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // Accumulate downward acceleration over time
        _velocity.y += gravity * Time.deltaTime;

        // Move CharacterController vertically (gravity/jumping)
        _controller.Move(_velocity * Time.deltaTime);
    }

    /// <summary>
    /// Smoothly transitions character controller height and camera position when crouching.
    /// </summary>
    private void HandleCrouch()
    {
        // Interpolate character controller height
        float targetHeight = _isCrouching ? crouchHeight : standingHeight;
        _controller.height = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

        // Interpolate camera height dynamically to match controller bounds
        if (playerCamera != null)
        {
            float cameraOffsetRatio = targetHeight / standingHeight;
            float targetCamY = _originalCameraY * cameraOffsetRatio;
            Vector3 camLocPos = playerCamera.localPosition;
            camLocPos.y = Mathf.Lerp(camLocPos.y, targetCamY, Time.deltaTime * crouchTransitionSpeed);
            playerCamera.localPosition = camLocPos;
        }
    }
}