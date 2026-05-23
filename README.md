# Sneaking Test

A first-person prototype built in Unity focusing on stealth, exploration, and interaction.

## Overview

This project is a prototype for an escape/sneaking game where the player navigates through various rooms (hallways, computer labs, storage rooms) using a first-person controller. The core gameplay loop involves finding items, interacting with the environment, and reaching the exit.

## Features

- **First-Person Controller**: Smooth character movement and camera controls.
- **Interaction System**: 
  - Interact with various objects in the environment using a unified `IInteractable` interface.
  - Includes interactable doors, computers, and vending machines.
- **Inventory System**: Pick up and manage items, such as keycards required to unlock specific areas.
- **Exit Trigger**: A designated exit point to complete the scene or level.
- **Prototype Environment**: Blockout materials and level design focused on testing mechanics in areas like classrooms, storage rooms, and hallways.

## Project Structure

- **`Assets/Scripts/`**: Contains the core logic.
  - `FirstPersonController.cs`: Handles player movement and camera.
  - `PlayerInteraction.cs` & `PlayerInventory.cs`: Manages what the player is looking at and holding.
  - `*Interactable.cs`: Scripts for specific objects in the world.
  - `ExitTrigger.cs`: Handles the win/exit condition.
- **`Assets/Scenes/`**:
  - `EchoHall_Prototype`: The main testing ground for the mechanics.

## Getting Started

1. Clone the repository.
2. Open the project in Unity (uses Universal Render Pipeline).
3. Open the `EchoHall_Prototype` scene located in `Assets/Scenes/`.
4. Press Play to test out the player movement and interaction systems.

## License

MIT License
