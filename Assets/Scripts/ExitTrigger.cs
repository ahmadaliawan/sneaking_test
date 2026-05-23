using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the Player
        if (other.name == "Player" || other.GetComponent<CharacterController>() != null)
        {
            Debug.Log("Player has reached the exit! Ending the scene...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
