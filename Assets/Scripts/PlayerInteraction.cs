using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Attached to the Player. Spawns a raycast from the camera center to detect IInteractable elements,
/// displays a clean visual HUD with cursor and prompt text, and processes key E presses.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("Maximum reach in meters to interact with objects.")]
    public float interactionRange = 3.0f;
    [Tooltip("Layers that contain interactable objects (default is everything).")]
    public LayerMask interactableLayers = ~0;

    [Header("HUD Customization (Optional)")]
    [Tooltip("Text element to show standard prompts. Left unassigned, the system will dynamically instantiate a clean one.")]
    public Text promptText;
    [Tooltip("Centermost targeting dot. Left unassigned, the system will dynamically instantiate a sleek pixel crosshair.")]
    public Image crosshairImage;

    private Transform _cameraTransform;
    private IInteractable _currentInteractable;

    private void Start()
    {
        // Cache camera viewport source - looks first at the child Camera
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            _cameraTransform = cam.transform;
        }
        else
        {
            _cameraTransform = transform;
            Debug.LogWarning("PlayerInteraction: No Camera found in children. Raycasting directly from Player root instead.");
        }

        // Failsafe: Automatically generates a clean targeting HUD if not manually dragged into Inspector slots
        EnsureHUDExists();
    }

    private void Update()
    {
        PerformRaycast();
        CheckForInput();
    }

    /// <summary>
    /// Performs a raycast from the camera center. Updates the HUD and caches the current interactable target.
    /// </summary>
    private void PerformRaycast()
    {
        if (_cameraTransform == null) return;

        Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
        RaycastHit hit;

        // Visual helper in Scene view: green line showing the reach of the player
        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.green);

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayers))
        {
            // Search hit object and recursively its parent hierarchy for IInteractable
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                _currentInteractable = interactable;

                // Update text to prompt "Press E to [Action]"
                if (promptText != null)
                {
                    promptText.text = interactable.GetInteractPrompt();
                    promptText.gameObject.SetActive(true);
                }

                // Change crosshair to green and enlarge to give visual feedback
                if (crosshairImage != null)
                {
                    crosshairImage.color = new Color(0.18f, 0.8f, 0.44f, 0.9f); // Emerald Green
                    crosshairImage.rectTransform.sizeDelta = new Vector2(8f, 8f);
                }
                return;
            }
        }

        // Clear target state when pointing away
        _currentInteractable = null;
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
        if (crosshairImage != null)
        {
            crosshairImage.color = new Color(1f, 1f, 1f, 0.6f); // Subtle transparent white
            crosshairImage.rectTransform.sizeDelta = new Vector2(4f, 4f);
        }
    }

    /// <summary>
    /// Polls keypress input in hybrid mode. Triggers Interact callback on current target.
    /// </summary>
    private void CheckForInput()
    {
        if (_currentInteractable == null) return;

        bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            interactPressed = keyboard.eKey.wasPressedThisFrame;
        }
#else
        interactPressed = Input.GetKeyDown(KeyCode.E);
#endif

        if (interactPressed)
        {
            // Call the interface method and pass this Player GameObject as the source
            _currentInteractable.Interact(gameObject);
        }
    }

    /// <summary>
    /// Procedurally creates a clean target crosshair and text HUD if no references were assigned.
    /// </summary>
    private void EnsureHUDExists()
    {
        if (promptText != null && crosshairImage != null) return;

        GameObject existingCanvas = GameObject.Find("InteractionHUD");
        if (existingCanvas != null)
        {
            if (promptText == null) promptText = existingCanvas.GetComponentInChildren<Text>();
            return;
        }

        // Instantiate standard ScreenSpace canvas
        GameObject hud = new GameObject("InteractionHUD");
        Canvas canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hud.AddComponent<CanvasScaler>();

        // Center visual dot crosshair
        GameObject dotObj = new GameObject("CrosshairDot");
        dotObj.transform.parent = hud.transform;
        Image dot = dotObj.AddComponent<Image>();
        dot.color = new Color(1f, 1f, 1f, 0.6f);
        RectTransform dotRect = dot.GetComponent<RectTransform>();
        dotRect.anchoredPosition = Vector2.zero;
        dotRect.sizeDelta = new Vector2(4f, 4f);

        // Center prompt text underneath the targeting dot
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.parent = hud.transform;
        Text txtComp = textObj.AddComponent<Text>();
        
        // Try loading modern Unity's built-in LegacyRuntime font, falling back to Arial if older version
        txtComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txtComp.font == null)
        {
            txtComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        
        txtComp.fontSize = 22;
        txtComp.alignment = TextAnchor.MiddleCenter;
        txtComp.color = Color.white;

        // Add soft drop shadow for readability
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform txtRect = txtComp.GetComponent<RectTransform>();
        txtRect.anchoredPosition = new Vector2(0f, -45f);
        txtRect.sizeDelta = new Vector2(600f, 50f);

        promptText = txtComp;
        crosshairImage = dot;
        promptText.gameObject.SetActive(false);
    }
}