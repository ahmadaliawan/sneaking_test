using UnityEngine;

/// <summary>
/// A concrete example of an interactable object.
/// Toggles a computer screen between off (black) and on (glowing green) with dynamic prompt states.
/// </summary>
public class ComputerInteractable : MonoBehaviour, IInteractable
{
    [Header("Terminal Settings")]
    [Tooltip("The Renderer of the monitor screen panel.")]
    public Renderer screenRenderer;

    private bool _isOnline = false;
    private Material _screenMaterial;

    private void Start()
    {
        // If screen renderer is not manually set, try to find one in children named "*Screen*" or "*Monitor*"
        if (screenRenderer == null)
        {
            screenRenderer = GetComponent<Renderer>();
        }

        if (screenRenderer != null)
        {
            // Create a unique material instance so we don't change other screens in the lab
            _screenMaterial = new Material(screenRenderer.sharedMaterial);
            screenRenderer.sharedMaterial = _screenMaterial;
            SetScreenState(false);
        }
    }

    /// <summary>
    /// Implements IInteractable.Interact. Toggles power state.
    /// </summary>
    public void Interact(GameObject source)
    {
        _isOnline = !_isOnline;
        SetScreenState(_isOnline);

        if (_isOnline)
        {
            Debug.Log("Computer Terminal: System boot initialized. CPU at 100% capacity.");
        }
        else
        {
            Debug.Log("Computer Terminal: System shutting down safely.");
        }
    }

    /// <summary>
    /// Implements IInteractable.GetInteractPrompt. Swaps based on current state.
    /// </summary>
    public string GetInteractPrompt()
    {
        return _isOnline ? "Press E to Turn Computer Off" : "Press E to Turn Computer On";
    }

    /// <summary>
    /// Updates the material colors and emission to reflect the power state.
    /// </summary>
    private void SetScreenState(bool online)
    {
        if (_screenMaterial == null) return;

        Color targetColor = online ? new Color(0.12f, 0.73f, 0.43f) : new Color(0.08f, 0.09f, 0.11f); // Glowing Green vs Dark Slate
        
        if (_screenMaterial.HasProperty("_BaseColor"))
        {
            _screenMaterial.SetColor("_BaseColor", targetColor);
        }
        else if (_screenMaterial.HasProperty("_Color"))
        {
            _screenMaterial.SetColor("_Color", targetColor);
        }

        // Enable or disable emission for Universal Render Pipeline (URP) shader
        if (online)
        {
            _screenMaterial.EnableKeyword("_EMISSION");
            _screenMaterial.SetColor("_EmissionColor", targetColor * 1.5f); // Multiplied for a nice glow
        }
        else
        {
            _screenMaterial.DisableKeyword("_EMISSION");
            _screenMaterial.SetColor("_EmissionColor", Color.clear);
        }
    }
}