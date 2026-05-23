# Sneaking Game Prototype - Detailed Documentation

Welcome to the Sneaking Game Prototype! This document is designed specifically for someone who is new to Unity and game development. We'll break down the core concepts of Unity used in this project and explain how all the different code scripts interact with one another to create a playable game.

---

## 1. Core Unity Concepts Used in This Game

Before diving into the code, it's important to understand a few fundamental building blocks of Unity that make this game tick:

### GameObjects & Components
Everything you see in a Unity scene (the player, the doors, the floor, the keycards) is a **GameObject**. By itself, a GameObject is just an empty container. We give it behavior by attaching **Components** to it. A Component can be a visual mesh, a physical collider, or a custom C# script.

### Colliders & Triggers
A **Collider** is an invisible shape (like a box or a sphere) wrapped around a GameObject that defines its physical boundaries. It stops objects from passing through each other. 
- **Trigger**: If we check the "Is Trigger" box on a Collider, it stops acting like a solid wall and instead acts like an invisible tripwire. When something enters it, it "triggers" an event in our code (used in `ExitTrigger.cs`).

### Raycasting
A **Raycast** is an invisible laser beam shot from a specific point in a specific direction. In our game, the player shoots a raycast forward from the center of their camera. If that laser beam hits an object's Collider, the game knows the player is "looking" at that object.

### Coroutines
Normally, a function runs entirely in a single frame. A **Coroutine** is a special kind of function in Unity that can pause its execution, wait for a little bit, and continue on the next frame. We use this to smoothly swing doors open over time, rather than having them teleport open instantly.

---

## 2. How the Systems Interact

The game's interaction system is built entirely around answering one question: *"What is the player looking at, and what should happen when they press E?"*

Here is the flow of how the scripts talk to each other:
1. The **Player** has a script (`PlayerInteraction.cs`) that constantly shoots a Raycast forward.
2. If the raycast hits an object, it checks: *"Does this object have the `IInteractable` interface?"*
3. If yes, it asks the object for its custom text prompt (e.g., "Press E to open") and displays it on the screen.
4. When the player presses 'E', the Player script tells the object to execute its specific `Interact()` function. 
5. The object then does whatever it's programmed to do (a door opens, a keycard gets picked up). If a door needs a keycard, it asks the `PlayerInventory.cs` script if the player has the required item.

---

## 3. Breakdown of the Scripts

Let's look at what each script actually does.

### `IInteractable.cs`
This is an **Interface**. Think of it as a contract. Any script that attaches this interface *must* include two specific functions:
1. `Interact(GameObject source)`: What happens when the player presses E.
2. `GetInteractPrompt()`: What text should show up on the player's screen.
By using an interface, the `PlayerInteraction.cs` script doesn't need to know if it's looking at a door, a computer, or a keycard. It just knows it's looking at "something interactable".

### `PlayerInteraction.cs`
Attached to the Player. 
- In its `Update()` function (which runs every single frame), it shoots out a Raycast (`PerformRaycast()`).
- It automatically draws a crosshair on the screen. When the raycast hits an `IInteractable` object, the crosshair turns green and the prompt text appears.
- It constantly checks if the 'E' key is pressed. If it is, it calls `Interact()` on the object it's looking at.

### `PlayerInventory.cs`
Attached to the Player alongside the Interaction script.
- It holds a simple list of strings (`collectedKeycards`).
- It has a function `AddKeycard()` to put new keys into the list, and a function `HasKeycard()` so that doors can check if the player possesses the right key.

### `KeycardInteractable.cs`
Attached to the Keycard models in the game.
- It "signs the contract" by implementing `IInteractable`.
- In its `Start()` function, it creates a new material dynamically to make the card glow with a specific color.
- When `Interact()` is called by the player, it looks for the `PlayerInventory` on the player, adds its name to the inventory list, and then uses `Destroy(gameObject)` to delete itself from the world, simulating being picked up.

### `DoorInteractable.cs`
Attached to Door objects.
- It implements `IInteractable`.
- It has settings in the Unity Editor allowing you to make it locked, and specifically require a certain keycard (e.g., "Red Keycard").
- When `Interact()` is called, it checks if it's locked. If it is, it asks the player's `PlayerInventory` if they have the required card.
- If access is granted (or if it was never locked), it uses a **Coroutine** (`SmoothRotate`) to mathematically smoothly rotate the door on its hinge over a few seconds.
- It even has logic to link a second door, so interacting with one side of double doors opens both automatically.

### `ExitTrigger.cs`
Attached to an invisible box at the end of the level.
- This doesn't use the interaction system. Instead, it relies on Unity's physics engine.
- It uses the built-in `OnTriggerEnter(Collider other)` function. 
- When the Player walks into the invisible trigger zone, this script fires, prints a message to the console, and gracefully ends the game/play mode.

---

## 4. Tips for Experimenting

If you want to learn by doing, try these mini-projects with the existing code:
1. **Change the Interaction Button**: Open `PlayerInteraction.cs` and find where it looks for `KeyCode.E`. Change it to `KeyCode.F`.
2. **Create a "Blue Keycard"**: Duplicate a Keycard in the scene. In the Unity Inspector, change its `Keycard Name` to "Blue Keycard" and its `Card Color` to Blue. Then, make a Door that requires a "Blue Keycard" to open.
3. **Make a new Interactable**: Create a new script called `LightSwitchInteractable`. Make it inherit from `MonoBehaviour, IInteractable`. Make the `Interact()` function turn a Light component on and off!
