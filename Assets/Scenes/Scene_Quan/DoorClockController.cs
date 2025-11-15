using UnityEngine;

public class DoorClockController : MonoBehaviour
{
    [Header("Visuals")]
    public Animator animator; // optional - expects 'Open' trigger
    public GameObject doorObject; // optional - can disable on open

    private bool _opened;

    public void OpenDoor()
    {
        if (_opened) return;
        _opened = true;

        if (animator != null)
            animator.SetTrigger("Open");
        if (doorObject != null)
            doorObject.SetActive(false);
        else
            Debug.Log("[DoorController] Opened (no visuals wired).");
    }
}
