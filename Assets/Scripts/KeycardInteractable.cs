using UnityEngine;

/// <summary>
/// An interactable item representing a physical keycard lying in the level.
/// When E is pressed, it adds itself to the player's inventory and vanishes.
/// </summary>
public class KeycardInteractable : MonoBehaviour, IInteractable
{
    [Header("Keycard Settings")]
    [Tooltip("The unique name identifier of this keycard (e.g. 'Red Keycard').")]
    public string keycardName = "Red Keycard";
    [Tooltip("Visual color of the card.")]
    public Color cardColor = Color.red;

    private void Start()
    {
        // Color the card mesh dynamically and add a glowing URP emission for visual searchability!
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", cardColor);
            
            // Turn on emissive channel so it glows slightly in dark environments
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", cardColor * 0.5f);
            mat.SetFloat("_Smoothness", 0.6f);
            
            rend.sharedMaterial = mat;
        }
    }

    public void Interact(GameObject source)
    {
        // Find PlayerInventory component on the player source
        PlayerInventory inventory = source.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddKeycard(keycardName);
            
            // Provide console feedback and destroy the physical pickup card
            Debug.Log("Keycard: Picked up " + keycardName + " from level!");
            GameObject.Destroy(gameObject);
        }
        else
        {
            Debug.LogError("Keycard: Player interacts with keycard but does not have a PlayerInventory script attached!");
        }
    }

    public string GetInteractPrompt()
    {
        return "Press E to pick up " + keycardName;
    }
}