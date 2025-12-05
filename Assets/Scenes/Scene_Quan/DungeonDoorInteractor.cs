using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DungeonDoorInteractor : MonoBehaviour
{
    [Header("References")]
    public GameObject uiPanel;       // Normal UI panel
    public DoorClockController door;      // Optional
    public DungeonDoorDiamondPanel diamondPanel; // Panel quản lý kim cương (optional)

    [Header("Settings")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("UI Hint (Optional)")]
    public GameObject hint;

    private bool playerInRange = false;
    private bool panelOpen = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
        if (hint != null) hint.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            if (!panelOpen)
            {
                // Tự động mở panel khi player chạm vào collider
                OpenPanel();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            if (hint != null) hint.SetActive(false);

            // Auto-close panel when leaving area
            if (panelOpen)
                ClosePanel();
        }
    }

    // Removed Update() - panel now opens automatically on trigger enter
    // Player can still close panel manually if needed via UI button

    public void OpenPanel()
    {
        if (uiPanel == null) return;

        uiPanel.SetActive(true);
        panelOpen = true;

        if (hint != null) hint.SetActive(false);

        // Khởi tạo slots và cập nhật trạng thái diamond panel nếu có
        if (diamondPanel != null)
        {
            diamondPanel.InitializeSlots(); // Spawn slots khi panel mở
            diamondPanel.UpdateStatus();
        }
    }

    public void ClosePanel()
    {
        if (uiPanel == null) return;

        uiPanel.SetActive(false);
        panelOpen = false;

        if (playerInRange && hint != null)
            hint.SetActive(true);
    }

    // Call this from a UI Button (optional)
    public void OnMinigameSuccess()
    {
        ClosePanel();
        if (door != null)
            door.OpenDoor();
    }

    // Được gọi từ DungeonDoorDiamondPanel khi cổng được mở
    public void OnDoorOpened()
    {
        ClosePanel();
    }
}
