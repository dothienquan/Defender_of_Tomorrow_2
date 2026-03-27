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
    
    [Header("Actions On Unlock")]
    [Tooltip("Object sẽ được bật lên (Active) sau khi mở khóa")]
    [SerializeField] private GameObject objectToActivate; 
    
    [Tooltip("Object sẽ bị tắt đi (Deactive) sau khi mở khóa")]
    [SerializeField] private GameObject objectToDeactivate;

    [Header("VFX Settings")]
    [Tooltip("Prefab hiệu ứng sẽ chạy khi mở khóa")]
    [SerializeField] private GameObject unlockVFX;
    
    [Tooltip("Vị trí spawn VFX (Nếu để trống sẽ dùng vị trí của cổng)")]
    [SerializeField] private Transform vfxSpawnPoint;

    private bool isUnlocked = false;
    private bool playerInRange = false;
    private InventoryController inventoryController;
    private float lastRefreshTime = 0f;
    private const float refreshInterval = 0.5f;

    private void Awake()
    {
        // Tự động tìm collider nếu chưa gán
        if (gateCollider == null)
            gateCollider = GetComponent<Collider2D>();

        // Tự động tìm InventoryController
        inventoryController = FindFirstObjectByType<InventoryController>();

        // Ẩn panel ban đầu
        if (lockPanel != null)
            lockPanel.SetActive(false);

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
            Canvas.ForceUpdateCanvases();
        }

        if (gateUI != null)
        {
            bool hasKey = CheckHasKey();
            gateUI.UpdateUI(hasKey, requiredKeyID, 1);
        }
    }

    private void HidePanel()
    {
        if (lockPanel != null)
        {
            lockPanel.SetActive(false);
        }
    }

    private bool CheckHasKey()
    {
        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryController>();

        if (inventoryController == null) return false;

        return inventoryController.HasItem(requiredKeyID);
    }
    
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

        // --- SỬA LỖI TẠI ĐÂY ---
        // Bỏ qua đoạn CheckHasKey() vì UI đã trừ Key trước khi gọi hàm này.
        // Nếu giữ lại check này, logic sẽ sai vì key đã bị mất.
        /* if (!CheckHasKey())
        {
            if (gateUI != null) gateUI.ShowMessage("Bạn chưa có chìa khóa!");
            return;
        }
        */

        // --- XỬ LÝ MỞ KHÓA ---
        isUnlocked = true;

        if (gateCollider != null) gateCollider.enabled = false;

        UpdateVisuals();
        HidePanel();

        // 1. Activate Object
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }

        // 2. Deactivate Object (Phần bạn đang cần)
        if (objectToDeactivate != null)
        {
            objectToDeactivate.SetActive(false);
            Debug.Log($"[LockedGate] Deactivated object: {objectToDeactivate.name}");
        }
        else
        {
            // Log cảnh báo nếu bạn quên kéo object vào Inspector
            Debug.LogWarning("[LockedGate] Object To Deactivate is NULL! Check Inspector.");
        }

        // 3. Play VFX
        if (unlockVFX != null)
        {
            Vector3 spawnPos = (vfxSpawnPoint != null) ? vfxSpawnPoint.position : transform.position;
            GameObject vfxInstance = Instantiate(unlockVFX, spawnPos, Quaternion.identity);
            Destroy(vfxInstance, 2f);
        }

        Debug.Log("[LockedGate] Gate unlocked successfully!");
    }

    private void UpdateVisuals()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(isUnlocked);
    }

    public void RefreshUI()
    {
        if (playerInRange && !isUnlocked && gateUI != null)
        {
            bool hasKey = CheckHasKey();
            gateUI.UpdateUI(hasKey, requiredKeyID, 1);
        }
    }
}