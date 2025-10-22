// DoorProximityInteractor.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorProximityInteractor : MonoBehaviour
{
    [Header("References")]
    public UIMiniGamePanelController panelController;
    public DoorController door;

    [Header("Settings")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Hint (Optional)")]
    public GameObject hint;

    private bool playerInRange;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (hint != null) hint.SetActive(false);
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
                Debug.LogWarning("[DoorProximityInteractor] panelController not assigned.");
            }
        }
    }
}
