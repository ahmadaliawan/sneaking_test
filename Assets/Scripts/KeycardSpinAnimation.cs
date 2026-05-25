using UnityEngine;

/// <summary>
/// Attaches to a keycard pickup and spins it continuously around its Y axis,
/// making it visually obvious and attractive in the level.
/// </summary>
public class KeycardSpinAnimation : MonoBehaviour
{
    [Header("Spin Settings")]
    [Tooltip("Degrees per second the keycard rotates around its Y axis.")]
    public float spinSpeed = 90f;

    [Header("Float Settings")]
    [Tooltip("How many units above its placed position the keycard floats.")]
    public float floatHeight = 0.2f;

    private void Start()
    {
        // Lift the card up once so it floats above the surface
        transform.position += Vector3.up * floatHeight;
    }

    private void Update()
    {
        // Spin continuously around the world Y axis
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }
}
