using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DungeonDoorInteractor : MonoBehaviour
{
    [Header("References")]
    public UIClockMinigameController panelController; // UI panel opener/closer
    public DoorController door;                        // Optional door to open on success

    [Header("Settings")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Hint (Optional)")]
    public GameObject hint;

    private bool playerInRange;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (hint != null) hint.SetActive(false);
        SubscribeToSuccess(true);
    }

    private void OnDisable()
    {
        SubscribeToSuccess(false);
    }

    private void SubscribeToSuccess(bool add)
    {
        if (panelController == null) return;
        if (add)
            panelController.onMiniGameSuccess.AddListener(HandleMinigameSuccess);
        else
            panelController.onMiniGameSuccess.RemoveListener(HandleMinigameSuccess);
    }

    private void HandleMinigameSuccess()
    {
       // if (door != null) door.OpenDoor();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            if (hint != null) hint.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (hint != null) hint.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (panelController != null)
            {
                panelController.OpenPanel();
            }
            else
            {
                Debug.LogWarning("[DungeonDoorInteractor] panelController not assigned.");
            }
        }
    }
}
