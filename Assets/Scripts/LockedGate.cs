using UnityEngine;

/// <summary>
/// Script cho cổng bị khóa, yêu cầu Key để mở
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LockedGate : MonoBehaviour
{
    [Header("Gate Settings")]
    [SerializeField] private int requiredKeyID = 1; // ID của Key cần để mở cổng
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Collider2D gateCollider; // Collider của cổng (sẽ tắt khi mở)

    [Header("UI Panel")]
    [SerializeField] private GameObject lockPanel; // Panel UI hiển thị khi đến gần
    [SerializeField] private LockedGateUI gateUI; // Component UI để hiển thị trạng thái

    [Header("Visual")]
    [SerializeField] private GameObject lockedVisual; // Visual khi cổng bị khóa
    [SerializeField] private GameObject unlockedVisual; // Visual khi cổng đã mở

    private bool isUnlocked = false;
    private bool playerInRange = false;
    private InventoryController inventoryController;
    private float lastRefreshTime = 0f;
    private const float refreshInterval = 0.5f; // Refresh mỗi 0.5 giây

    private void Awake()
    {
        // Tự động tìm collider nếu chưa gán
        if (gateCollider == null)
        {
            gateCollider = GetComponent<Collider2D>();
        }

        // Tự động tìm InventoryController
        inventoryController = FindFirstObjectByType<InventoryController>();

        // Ẩn panel ban đầu
        if (lockPanel != null)
        {
            lockPanel.SetActive(false);
        }

        // Setup visual ban đầu
        UpdateVisuals();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag) || isUnlocked) return;

        playerInRange = true;
        ShowPanel();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Refresh UI khi player vẫn trong range (để cập nhật nếu vừa nhặt Key)
        // Chỉ refresh mỗi refreshInterval để tối ưu performance
        if (playerInRange && !isUnlocked && other.CompareTag(playerTag))
        {
            if (Time.time - lastRefreshTime >= refreshInterval)
            {
                RefreshUI();
                lastRefreshTime = Time.time;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;
        HidePanel();
    }

    private void ShowPanel()
    {
        if (lockPanel != null)
        {
            lockPanel.SetActive(true);
        }

        // Cập nhật UI với trạng thái Key
        if (gateUI != null)
        {
            bool hasKey = CheckHasKey();
            int keyCount = GetKeyCount();
            gateUI.UpdateUI(hasKey, requiredKeyID, 1); // 1 Key cần thiết
        }
    }

    private void HidePanel()
    {
        if (lockPanel != null)
        {
            lockPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Kiểm tra xem player có Key không
    /// </summary>
    private bool CheckHasKey()
    {
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        if (inventoryController == null)
        {
            Debug.LogWarning("[LockedGate] InventoryController not found!");
            return false;
        }

        return inventoryController.HasItem(requiredKeyID);
    }

    /// <summary>
    /// Mở cổng (được gọi từ UI khi nhấn xác nhận)
    /// </summary>
    public void UnlockGate()
    {
        if (isUnlocked) return;

        // Kiểm tra lại Key trước khi mở
        if (!CheckHasKey())
        {
            Debug.LogWarning("[LockedGate] Player does not have required key!");
            if (gateUI != null)
            {
                gateUI.ShowMessage("Bạn chưa có chìa khóa!");
            }
            return;
        }

        // Mở cổng
        isUnlocked = true;

        // Tắt collider
        if (gateCollider != null)
        {
            gateCollider.enabled = false;
        }

        // Cập nhật visual
        UpdateVisuals();

        // Ẩn panel
        HidePanel();

        Debug.Log("[LockedGate] Gate unlocked!");
    }

    private void UpdateVisuals()
    {
        if (lockedVisual != null)
        {
            lockedVisual.SetActive(!isUnlocked);
        }

        if (unlockedVisual != null)
        {
            unlockedVisual.SetActive(isUnlocked);
        }
    }

    /// <summary>
    /// Lấy số lượng Key hiện có
    /// </summary>
    private int GetKeyCount()
    {
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        if (inventoryController == null)
        {
            return 0;
        }

        return inventoryController.GetItemCount(requiredKeyID);
    }

    /// <summary>
    /// Refresh UI khi player vẫn trong range (để cập nhật nếu player vừa nhặt Key)
    /// </summary>
    public void RefreshUI()
    {
        if (playerInRange && !isUnlocked && gateUI != null)
        {
            bool hasKey = CheckHasKey();
            gateUI.UpdateUI(hasKey, requiredKeyID, 1); // 1 Key cần thiết
        }
    }
}

