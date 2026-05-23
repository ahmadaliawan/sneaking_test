using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A simple, beginner-friendly inventory script for the player to store keys and keycards.
/// Attach this to your Player prefab alongside the controller.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Collected Keycards")]
    [Tooltip("List of keycards currently held by the player.")]
    public List<string> collectedKeycards = new List<string>();

    /// <summary>
    /// Checks if the player possesses a specific keycard in their inventory.
    /// </summary>
    public bool HasKeycard(string keycardName)
    {
        return collectedKeycards.Contains(keycardName);
    }

    /// <summary>
    /// Adds a keycard to the inventory and shows a log.
    /// </summary>
    public void AddKeycard(string keycardName)
    {
        if (!collectedKeycards.Contains(keycardName))
        {
            collectedKeycards.Add(keycardName);
            Debug.Log("Inventory: Collected " + keycardName + "! You can now open matching locked doors.");
        }
    }
}