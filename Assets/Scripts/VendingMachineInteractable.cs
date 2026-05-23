using UnityEngine;

/// <summary>
/// A fun, concrete example of an interactable object.
/// When the player presses E, it instantiates a physical soda can that rolls on the floor.
/// </summary>
public class VendingMachineInteractable : MonoBehaviour, IInteractable
{
    [Header("Vending Settings")]
    [Tooltip("The name of the item being sold (e.g. Soda, Snacks).")]
    public string itemName = "Soda";
    [Tooltip("Color of the dispensed item's model.")]
    public Color itemColor = Color.red;
    [Tooltip("Offset position where the item will be spawned (relative to vending machine).")]
    public Vector3 spawnOffset = new Vector3(0.5f, 0.2f, 0.0f); // Placed at dispenser slot level

    /// <summary>
    /// Implements IInteractable.Interact. Triggered when clicked.
    /// </summary>
    public void Interact(GameObject source)
    {
        // Spawns a physical soda cylinder that pops out
        GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        can.name = itemName + "_Can";
        can.transform.position = transform.position + transform.TransformDirection(spawnOffset);
        can.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f); // Tiny soda can shape
        can.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Laying flat on its side

        // Assign a color-matching material
        Renderer rend = can.GetComponent<Renderer>();
        if (rend != null)
        {
            Material canMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            canMat.SetColor("_BaseColor", itemColor);
            canMat.SetFloat("_Smoothness", 0.8f); // Glossy metal feel
            canMat.SetFloat("_Metallic", 0.7f);
            rend.sharedMaterial = canMat;
        }

        // Add a Rigidbody to make it physically roll on the floor!
        Rigidbody rb = can.AddComponent<Rigidbody>();
        rb.mass = 0.5f;

        // Apply a small outward popping force!
        Vector3 ejectDirection = transform.right; // Facing outward from the wall
        rb.AddForce(ejectDirection * 1.5f + Vector3.up * 1.0f, ForceMode.Impulse);

        // Debug feedback
        Debug.Log("Vending Machine: Dispensed 1 ice-cold " + itemName + "!");
    }

    /// <summary>
    /// Implements IInteractable.GetInteractPrompt.
    /// </summary>
    public string GetInteractPrompt()
    {
        return "Press E to buy " + itemName;
    }
}