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

    [Header("Slide Settings")]
    [Tooltip("Initial speed burst when starting a slide.")]
    public float slideInitialSpeed = 12.0f;
    [Tooltip("How fast the slide speed decays.")]
    public float slideFriction = 7.0f;
    [Tooltip("Height of the character controller when sliding.")]
    public float slideHeight = 0.5f;
    [Tooltip("Camera roll angle (tilt) during a slide.")]
    public float slideCameraTilt = -5.0f;
    [Tooltip("How fast the camera tilts during a slide.")]
    public float slideTiltSpeed = 10.0f;

    private CharacterController _controller;
    private Vector3 _velocity;
    private float _verticalRotation = 0.0f;
    private float _cameraRoll = 0.0f;
    private bool _isCrouching = false;
    private bool _isSliding = false;
    private Vector3 _slideDirection;
    private float _currentSlideSpeed;
    private float _originalCameraY;
    private bool _isSprinting = false;

    [Header("Sprint Camera Shake")]
    [Tooltip("How far the camera bobs vertically while sprinting.")]
    public float sprintBobAmplitudeY = 0.06f;
    [Tooltip("How far the camera bobs sideways while sprinting.")]
    public float sprintBobAmplitudeX = 0.03f;
    [Tooltip("Speed of the bob cycle while sprinting.")]
    public float sprintBobFrequency = 12f;
    [Tooltip("How quickly the shake blends in/out.")]
    public float sprintBobSmoothing = 8f;

    private float _sprintBobTimer = 0f;
    private float _sprintBobBlend = 0f;  // 0 = no shake, 1 = full shake
    private Vector3 _baseCameraLocalPos;

    [Header("Landing Camera Shake")]
    [Tooltip("How far the camera dips down on landing.")]
    public float landingShakeAmplitude = 0.12f;
    [Tooltip("How quickly the camera recovers from a landing dip.")]
    public float landingShakeRecovery = 10f;
    [Tooltip("Minimum downward speed to trigger a landing shake.")]
    public float landingShakeThreshold = 3f;

    private bool _wasGrounded = true;
    private float _fallVelocity = 0f;       // Tracks downward speed while airborne
    private float _landingShakeOffset = 0f; // Current Y offset applied by landing shake

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
            _baseCameraLocalPos = playerCamera.localPosition;
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
        HandleSprintShake();
        HandleLandingShake();
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
        
        // Handle Camera Roll for sliding
        float targetRoll = _isSliding ? slideCameraTilt : 0f;
        _cameraRoll = Mathf.Lerp(_cameraRoll, targetRoll, Time.deltaTime * slideTiltSpeed);

        playerCamera.localRotation = Quaternion.Euler(_verticalRotation, 0.0f, _cameraRoll);

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
        bool slidePressed = false;

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
            slidePressed = keyboard.cKey.wasPressedThisFrame;
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
        slidePressed = Input.GetKeyDown(KeyCode.C);
#endif

        Vector3 move;
        float speed;

        if (_isSliding)
        {
            // During slide, movement is locked to slide direction, and speed decays
            _currentSlideSpeed -= slideFriction * Time.deltaTime;
            move = _slideDirection;
            speed = _currentSlideSpeed;

            // Stop sliding if speed drops enough, or if we jump
            if (_currentSlideSpeed <= crouchMultiplier * walkSpeed || jumpPressed)
            {
                _isSliding = false;
                _isCrouching = true; // Transition smoothly into a crouch after sliding
            }
            _isSprinting = false;
        }
        else
        {
            // Calculate direction relative to the player's current orientation
            move = transform.right * moveX + transform.forward * moveZ;
            
            // Normalize vector to ensure uniform movement speed in diagonal directions
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            // Calculate speed with active modifiers
            speed = walkSpeed;
            if (_isCrouching)
            {
                speed *= crouchMultiplier;
            }
            else if (isSprinting)
            {
                speed *= sprintMultiplier;
            }

            // Track sprint state for camera shake
            _isSprinting = isSprinting && isGrounded && move.sqrMagnitude > 0.01f;

            // Trigger slide if sprinting, moving, and pressing C
            if (slidePressed && _isSprinting && isGrounded)
            {
                _isSliding = true;
                _slideDirection = move;
                _currentSlideSpeed = slideInitialSpeed;
                speed = _currentSlideSpeed;
            }
        }

        // Track fall velocity and detect landing
        if (!isGrounded)
        {
            // Record how fast we're falling (use negative velocity for magnitude)
            _fallVelocity = Mathf.Abs(_velocity.y);
        }
        else if (!_wasGrounded)
        {
            // We just landed this frame — trigger shake if we fell fast enough
            if (_fallVelocity >= landingShakeThreshold)
            {
                // Scale shake strength with fall speed, clamped to max amplitude
                float strength = Mathf.Clamp01(_fallVelocity / 15f);
                _landingShakeOffset = -landingShakeAmplitude * strength;
            }
            _fallVelocity = 0f;
        }
        _wasGrounded = isGrounded;

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
    /// Applies a subtle head-bob shake to the camera while the player is sprinting.
    /// Uses a sine wave on X and Y axes, smoothly blended in and out.
    /// </summary>
    private void HandleSprintShake()
    {
        if (playerCamera == null) return;

        // Blend the shake weight smoothly in (sprinting) or out (not sprinting)
        float targetBlend = _isSprinting ? 1f : 0f;
        _sprintBobBlend = Mathf.Lerp(_sprintBobBlend, targetBlend, Time.deltaTime * sprintBobSmoothing);

        if (_sprintBobBlend > 0.001f)
        {
            _sprintBobTimer += Time.deltaTime * sprintBobFrequency;

            // Vertical bob (up-down) — full sine wave
            float bobY = Mathf.Sin(_sprintBobTimer) * sprintBobAmplitudeY * _sprintBobBlend;
            // Horizontal bob (side-to-side) — half-frequency for a natural feel
            float bobX = Mathf.Sin(_sprintBobTimer * 0.5f) * sprintBobAmplitudeX * _sprintBobBlend;

            // Apply offset on top of the base camera position (HandleCrouch manages Y, so we ADD to it)
            Vector3 camPos = playerCamera.localPosition;
            camPos.x = _baseCameraLocalPos.x + bobX;
            // Only override Y if crouch is not actively transitioning (blend check)
            camPos.y += bobY;
            playerCamera.localPosition = camPos;
        }
        else
        {
            // Reset timer when not shaking to avoid phase pop on next sprint
            _sprintBobTimer = 0f;
        }
    }

    /// <summary>
    /// Applies a one-shot downward camera dip when the player lands after a jump.
    /// Shake strength scales with how fast the player was falling.
    /// </summary>
    private void HandleLandingShake()
    {
        if (playerCamera == null || Mathf.Abs(_landingShakeOffset) < 0.0005f)
        {
            _landingShakeOffset = 0f;
            return;
        }

        // Spring the offset back toward zero each frame
        _landingShakeOffset = Mathf.Lerp(_landingShakeOffset, 0f, Time.deltaTime * landingShakeRecovery);

        // Apply directly as a Y nudge on the camera local position
        Vector3 camPos = playerCamera.localPosition;
        camPos.y += _landingShakeOffset;
        playerCamera.localPosition = camPos;
    }

    /// <summary>
    /// Smoothly transitions character controller height and camera position when crouching.
    /// </summary>
    private void HandleCrouch()
    {
        // Interpolate character controller height
        float targetHeight = standingHeight;
        if (_isSliding) targetHeight = slideHeight;
        else if (_isCrouching) targetHeight = crouchHeight;
        
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