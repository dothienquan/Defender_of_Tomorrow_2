using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel quản lý việc kéo thả kim cương vào để mở cổng dungeon
/// </summary>
public class DungeonDoorDiamondPanel : MonoBehaviour
{
    [Header("Diamond Settings")]
    [SerializeField] private int[] requiredDiamondIDs; // Array các ID của kim cương (nếu để trống sẽ kiểm tra theo tên)
    [SerializeField] private int requiredDiamondCount = 3; // Số lượng kim cương cần
    [SerializeField] private bool checkByName = true; // Nếu true, sẽ kiểm tra tên chứa "diamond" thay vì chỉ kiểm tra ID

    [Header("UI References")]
    [SerializeField] private Transform diamondSlotsParent; // Parent chứa các slot để kéo kim cương vào
    [SerializeField] private GameObject diamondSlotPrefab; // Prefab của slot
    [SerializeField] private int slotCount = 3; // Số lượng slot
    [SerializeField] private Button confirmButton; // Nút xác nhận (optional, có thể không dùng)
    [SerializeField] private TextMeshProUGUI statusText; // Text hiển thị trạng thái (ví dụ: "2/3")
    [SerializeField] private string statusFormat = "{0}/{1}"; // Format status text
    [SerializeField] private bool autoCloseOnComplete = true; // Tự động đóng panel khi đủ item

    [Header("Door Reference")]
    [SerializeField] private DoorClockController doorController; // Controller để mở cổng
    [SerializeField] private Collider2D doorCollider; // Collider của cổng (sẽ tắt khi mở)
    [SerializeField] private DungeonDoorInteractor doorInteractor; // Interactor để thông báo khi cổng mở

    private List<Slot> diamondSlots = new List<Slot>();
    private InventoryController inventoryController;
    private ItemDictionary itemDictionary;
    private bool isDoorOpened = false;

    private void Awake()
    {
        // Tự động tìm các components
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        // Setup confirm button (optional)
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false; // Ban đầu disable
        }

        // KHÔNG tạo slots trong Awake - sẽ tạo khi panel mở (trong InitializeSlots)
    }

    private void OnEnable()
    {
        // Khi panel được enable, khởi tạo slots nếu chưa có
        if (diamondSlots.Count == 0 && diamondSlotsParent != null)
        {
            InitializeSlots();
        }
    }

    private void OnDisable()
    {
        // Clear slots khi panel tắt để spawn lại lần sau
        ClearSlots();
    }

    /// <summary>
    /// Khởi tạo slots khi panel mở (được gọi từ DungeonDoorInteractor hoặc OnEnable)
    /// </summary>
    public void InitializeSlots()
    {
        // Xóa slots cũ nếu có
        ClearSlots();

        // Tạo slots mới
        CreateSlots();

        // Cập nhật status ban đầu
        UpdateStatus();
    }

    /// <summary>
    /// Xóa tất cả slots
    /// </summary>
    private void ClearSlots()
    {
        if (diamondSlotsParent != null)
        {
            // Destroy tất cả child slots
            for (int i = diamondSlotsParent.childCount - 1; i >= 0; i--)
            {
                Destroy(diamondSlotsParent.GetChild(i).gameObject);
            }
        }

        diamondSlots.Clear();
    }

    private void CreateSlots()
    {
        if (diamondSlotsParent == null || diamondSlotPrefab == null) return;

        diamondSlots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(diamondSlotPrefab, diamondSlotsParent);
            Slot slot = slotObj.GetComponent<Slot>();
            if (slot == null)
            {
                slot = slotObj.AddComponent<Slot>();
            }
            diamondSlots.Add(slot);
        }
    }

    /// <summary>
    /// Kiểm tra xem item được kéo vào có phải là kim cương không
    /// </summary>
    public bool IsValidDiamond(GameObject item)
    {
        if (item == null) return false;

        Item itemComponent = item.GetComponent<Item>();
        if (itemComponent == null) return false;

        // Kiểm tra theo tên nếu checkByName = true
        if (checkByName && itemComponent.Name.ToLower().Contains("diamond"))
        {
            return true;
        }

        // Kiểm tra theo ID nếu có requiredDiamondIDs
        if (requiredDiamondIDs != null && requiredDiamondIDs.Length > 0)
        {
            foreach (int diamondID in requiredDiamondIDs)
            {
                if (itemComponent.ID == diamondID)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Đếm số lượng kim cương đã được kéo vào panel
    /// </summary>
    public int GetDiamondCount()
    {
        int count = 0;
        foreach (Slot slot in diamondSlots)
        {
            if (slot != null && slot.currentItem != null && IsValidDiamond(slot.currentItem))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Cập nhật trạng thái UI
    /// </summary>
    public void UpdateStatus()
    {
        int currentCount = GetDiamondCount();
        bool hasEnough = currentCount >= requiredDiamondCount;

        // Cập nhật status text
        if (statusText != null)
        {
            statusText.text = string.Format(statusFormat, currentCount, requiredDiamondCount);
        }

        // Enable/disable confirm button (nếu có)
        if (confirmButton != null)
        {
            confirmButton.interactable = hasEnough && !isDoorOpened;
        }

        // Tự động đóng panel và mở cổng nếu đủ item
        if (hasEnough && autoCloseOnComplete && !isDoorOpened)
        {
            OnComplete();
        }
    }

    /// <summary>
    /// Xử lý khi đủ item (tự động gọi hoặc từ confirm button)
    /// </summary>
    private void OnComplete()
    {
        if (isDoorOpened)
        {
            Debug.LogWarning("[DungeonDoorDiamondPanel] Door already opened!");
            return;
        }

        int diamondCount = GetDiamondCount();
        if (diamondCount < requiredDiamondCount)
        {
            Debug.LogWarning($"[DungeonDoorDiamondPanel] Not enough diamonds! Have: {diamondCount}, Need: {requiredDiamondCount}");
            return;
        }

        // Remove kim cương từ inventory
        RemoveDiamondsFromInventory();

        // Mở cổng
        OpenDoor();

        // Đóng panel
        ClosePanel();
    }

    /// <summary>
    /// Được gọi khi item được kéo vào slot (từ ItemDragHandler hoặc tự gọi)
    /// </summary>
    public void OnItemDroppedInSlot(Slot slot)
    {
        UpdateStatus();
    }

    /// <summary>
    /// Xử lý khi nhấn nút xác nhận (nếu có confirm button)
    /// </summary>
    private void OnConfirmClicked()
    {
        OnComplete();
    }

    /// <summary>
    /// Đóng panel
    /// </summary>
    private void ClosePanel()
    {
        if (transform.parent != null)
        {
            transform.parent.gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Remove kim cương từ inventory sau khi confirm
    /// </summary>
    private void RemoveDiamondsFromInventory()
    {
        if (inventoryController == null || inventoryController.inventoryPanel == null) return;

        int removedCount = 0;
        int needToRemove = requiredDiamondCount;

        // Tìm và remove kim cương từ inventory
        foreach (Transform slotTransform in inventoryController.inventoryPanel.transform)
        {
            if (needToRemove <= 0) break;

            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null && IsValidDiamond(slot.currentItem))
                {
                    Destroy(slot.currentItem);
                    slot.currentItem = null;
                    removedCount++;
                    needToRemove--;
                }
            }
        }

        Debug.Log($"[DungeonDoorDiamondPanel] Removed {removedCount} diamonds from inventory");
    }

    /// <summary>
    /// Mở cổng
    /// </summary>
    private void OpenDoor()
    {
        isDoorOpened = true;

        // Mở cổng qua DoorClockController
        if (doorController != null)
        {
            doorController.OpenDoor();
        }

        // Tắt collider của cổng
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        // Thông báo cho doorInteractor
        if (doorInteractor != null)
        {
            doorInteractor.OnDoorOpened();
        }

        Debug.Log("[DungeonDoorDiamondPanel] Door opened!");
    }

    /// <summary>
    /// Reset panel (xóa tất cả kim cương đã kéo vào)
    /// </summary>
    public void ResetPanel()
    {
        foreach (Slot slot in diamondSlots)
        {
            if (slot != null && slot.currentItem != null)
            {
                Destroy(slot.currentItem);
                slot.currentItem = null;
            }
        }

        UpdateStatus();
    }
}

