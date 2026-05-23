using UnityEngine;

/// <summary>
/// A reusable, clean interface for any object in the game that can be interacted with.
/// Simply implement this interface on any MonoBehaviour script to make it interactive.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Triggered when the player looks at this object and presses the interaction key (E).
    /// </summary>
    /// <param name="source">The GameObject that initiated the interaction (usually the Player).</param>
    void Interact(GameObject source);

    /// <summary>
    /// Returns the descriptive text to display on the player's screen (e.g. "Open Door" or "Use Vending Machine").
    /// </summary>
    string GetInteractPrompt();
}