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
    
    [Header("On Unlock")]
    [SerializeField] private GameObject objectToActivate; // Object sẽ được set active sau khi unlock thành công

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
            
            // Đảm bảo UI được update sau khi active
            // Force update Canvas để đảm bảo layout được tính toán
            Canvas.ForceUpdateCanvases();
            
            // Đảm bảo EventSystem hoạt động
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogWarning("[LockedGate] EventSystem not found! UI interactions may not work.");
            }
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

        bool hasKey = inventoryController.HasItem(requiredKeyID);
        Debug.Log($"[LockedGate] Checking for key ID {requiredKeyID}: {(hasKey ? "FOUND" : "NOT FOUND")}");
        
        // Debug: In ra tất cả items trong inventory
        if (!hasKey)
        {
            Debug.LogWarning($"[LockedGate] Key ID {requiredKeyID} not found in inventory. Checking all items...");
            int itemCount = inventoryController.GetItemCount(requiredKeyID);
            Debug.Log($"[LockedGate] Item count for ID {requiredKeyID}: {itemCount}");
        }
        
        return hasKey;
    }
    
    /// <summary>
    /// Public method để lấy requiredKeyID (dùng cho LockedGateUI)
    /// </summary>
    public int GetRequiredKeyID()
    {
        return requiredKeyID;
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

        // Set active object sau khi panel đã tắt
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            Debug.Log($"[LockedGate] Activated object: {objectToActivate.name}");
        }

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

