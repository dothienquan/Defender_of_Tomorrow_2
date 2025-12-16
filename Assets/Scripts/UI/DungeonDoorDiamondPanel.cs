using System.Collections;
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

    [Header("Sorting Order Change")]
    [SerializeField] private GameObject objectToChangeSortingOrder; // Object cần thay đổi sorting order
    [SerializeField] private int newSortingOrder = 1; // Sorting order mới (mặc định 1)

    private List<Slot> diamondSlots = new List<Slot>();
    private InventoryController inventoryController;
    private ItemDictionary itemDictionary;
    private bool isDoorOpened = false;
    private int lastDiamondCount = 0; // Để theo dõi thay đổi

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
        lastDiamondCount = 0;
    }

    private void Update()
    {
        // Kiểm tra liên tục khi panel đang mở để đảm bảo tự động tắt khi đủ item
        if (gameObject.activeSelf && !isDoorOpened)
        {
            int currentCount = GetDiamondCount();
            
            // Nếu số lượng thay đổi, cập nhật status
            if (currentCount != lastDiamondCount)
            {
                lastDiamondCount = currentCount;
                UpdateStatus();
            }
            // Nếu đã đủ nhưng chưa tắt, kiểm tra lại
            else if (currentCount >= requiredDiamondCount)
            {
                UpdateStatus();
            }
        }
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
        lastDiamondCount = 0;
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
            
            // QUAN TRỌNG: Đảm bảo Slot component được enable (có thể bị disabled trong prefab)
            if (!slot.enabled)
            {
                slot.enabled = true;
            }
            
            // Đánh dấu slot là DiamondSlot để phân biệt với slot inventory/hotbar
            DiamondSlot diamondSlot = slotObj.GetComponent<DiamondSlot>();
            if (diamondSlot == null)
            {
                diamondSlot = slotObj.AddComponent<DiamondSlot>();
            }
            
            // Đảm bảo DiamondSlot component được enable
            if (!diamondSlot.enabled)
            {
                diamondSlot.enabled = true;
            }
            
            diamondSlots.Add(slot);
            
            Debug.Log($"[DungeonDoorDiamondPanel] Created slot {i}: Slot enabled={slot.enabled}, DiamondSlot enabled={diamondSlot.enabled}");
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
    /// Kiểm tra xem tất cả slots đã có item đúng ID chưa
    /// </summary>
    public bool AreAllSlotsFilledWithValidDiamonds()
    {
        if (diamondSlots.Count < requiredDiamondCount)
        {
            return false;
        }

        int validCount = 0;
        foreach (Slot slot in diamondSlots)
        {
            if (slot != null && slot.currentItem != null && IsValidDiamond(slot.currentItem))
            {
                validCount++;
            }
        }

        return validCount >= requiredDiamondCount;
    }

    /// <summary>
    /// Cập nhật trạng thái UI
    /// </summary>
    public void UpdateStatus()
    {
        int currentCount = GetDiamondCount();
        bool hasEnough = currentCount >= requiredDiamondCount;
        bool allSlotsFilled = AreAllSlotsFilledWithValidDiamonds();

        Debug.Log($"[DungeonDoorDiamondPanel] UpdateStatus - Count: {currentCount}/{requiredDiamondCount}, HasEnough: {hasEnough}, AllFilled: {allSlotsFilled}, AutoClose: {autoCloseOnComplete}, DoorOpened: {isDoorOpened}");
        
        // Debug chi tiết từng slot
        for (int i = 0; i < diamondSlots.Count; i++)
        {
            Slot slot = diamondSlots[i];
            if (slot != null)
            {
                bool hasItem = slot.currentItem != null;
                bool isValid = hasItem && IsValidDiamond(slot.currentItem);
                string itemName = hasItem ? slot.currentItem.GetComponent<Item>()?.Name ?? "Unknown" : "None";
                Debug.Log($"[DungeonDoorDiamondPanel] Slot {i}: HasItem={hasItem}, IsValid={isValid}, ItemName={itemName}");
            }
        }

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

        // Tự động đóng panel và mở cổng nếu đủ item đúng ID
        if (hasEnough && allSlotsFilled && autoCloseOnComplete && !isDoorOpened)
        {
            Debug.Log($"[DungeonDoorDiamondPanel] ✓ All {requiredDiamondCount} diamonds placed correctly. Auto-closing panel...");
            OnComplete();
        }
        else if (hasEnough && !allSlotsFilled)
        {
            Debug.LogWarning($"[DungeonDoorDiamondPanel] Has enough count ({currentCount}) but not all slots filled with valid diamonds!");
        }
        else if (hasEnough && !autoCloseOnComplete)
        {
            Debug.Log($"[DungeonDoorDiamondPanel] Has enough diamonds but autoCloseOnComplete is false. Waiting for confirm button.");
        }
        else if (hasEnough && isDoorOpened)
        {
            Debug.LogWarning($"[DungeonDoorDiamondPanel] Has enough diamonds but door already opened!");
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
        // Kiểm tra slot có phải là diamond slot không
        if (slot == null)
        {
            Debug.LogWarning("[DungeonDoorDiamondPanel] OnItemDroppedInSlot called with null slot!");
            return;
        }

        // Kiểm tra slot có trong danh sách diamondSlots không
        if (!diamondSlots.Contains(slot))
        {
            Debug.LogWarning($"[DungeonDoorDiamondPanel] Slot {slot.name} is not in diamondSlots list!");
            return;
        }

        Debug.Log($"[DungeonDoorDiamondPanel] Item dropped into slot. Current diamond count: {GetDiamondCount()}/{requiredDiamondCount}");

        // Delay một frame để đảm bảo item đã được set vào slot
        StartCoroutine(UpdateStatusDelayed());
    }

    /// <summary>
    /// Cập nhật status sau một frame để đảm bảo item đã được set vào slot
    /// </summary>
    private System.Collections.IEnumerator UpdateStatusDelayed()
    {
        yield return null; // Đợi một frame
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
    /// Đóng panel - chỉ ẩn panel minigame, không ẩn toàn bộ UI canvas
    /// </summary>
    private void ClosePanel()
    {
        // Nếu có doorInteractor, dùng nó để đóng panel (đảm bảo đúng panel được đóng)
        if (doorInteractor != null)
        {
            doorInteractor.ClosePanel();
        }
        else
        {
            // Fallback: chỉ ẩn chính GameObject này, không ẩn parent (có thể là Canvas)
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

        // Thay đổi sorting order của object
        ChangeObjectSortingOrder();

        Debug.Log("[DungeonDoorDiamondPanel] Door opened!");
    }

    /// <summary>
    /// Thay đổi sorting order của object từ 0 sang 1 (hoặc giá trị được set)
    /// </summary>
    private void ChangeObjectSortingOrder()
    {
        if (objectToChangeSortingOrder == null)
        {
            Debug.LogWarning("[DungeonDoorDiamondPanel] ObjectToChangeSortingOrder is null! Cannot change sorting order.");
            return;
        }

        // Tìm SpriteRenderer component
        SpriteRenderer spriteRenderer = objectToChangeSortingOrder.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = newSortingOrder;
            Debug.Log($"[DungeonDoorDiamondPanel] Changed sorting order of {objectToChangeSortingOrder.name} to {newSortingOrder}");
        }
        else
        {
            // Nếu không có SpriteRenderer, thử tìm trong children
            spriteRenderer = objectToChangeSortingOrder.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = newSortingOrder;
                Debug.Log($"[DungeonDoorDiamondPanel] Changed sorting order of {spriteRenderer.gameObject.name} (child of {objectToChangeSortingOrder.name}) to {newSortingOrder}");
            }
            else
            {
                Debug.LogWarning($"[DungeonDoorDiamondPanel] No SpriteRenderer found on {objectToChangeSortingOrder.name} or its children! Cannot change sorting order.");
            }
        }
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

