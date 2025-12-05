using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    private ItemDictionary itemDictionary;

    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    private void Start()
    {
        itemDictionary = FindFirstObjectByType<ItemDictionary>();
        
        // Tự động tạo slots nếu chưa có
        CreateSlotsIfNeeded();
    }

    public bool AddItem(GameObject itemPrefab)
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryController] InventoryPanel is null! Cannot add item.");
            return false;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("[InventoryController] ItemPrefab is null! Cannot add item.");
            return false;
        }

        // Kiểm tra xem có slots không
        if (inventoryPanel.transform.childCount == 0)
        {
            Debug.LogWarning("[InventoryController] InventoryPanel has no slots! Creating slots automatically...");
            CreateSlotsIfNeeded();
        }

        // Tìm slot trống
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem == null)
            {
                // QUAN TRỌNG: Instantiate với worldPositionStays = false để item được đặt đúng trong UI space
                GameObject newItem = Instantiate(itemPrefab, slotTransform, false);
                
                // Nếu item có WorldItemUIHandler, enable UI components
                WorldItemUIHandler worldItemHandler = newItem.GetComponent<WorldItemUIHandler>();
                if (worldItemHandler != null)
                {
                    worldItemHandler.OnAddedToInventory();
                }
                
                // Setup item sử dụng helper method
                SetupItemForInventory(newItem, slotTransform);
                
                slot.currentItem = newItem;
                
                Debug.Log($"[InventoryController] Item '{itemPrefab.name}' added to inventory at slot {slotTransform.GetSiblingIndex()}.");
                return true;
            }
        }

        Debug.LogWarning("[InventoryController] Inventory is full. Cannot add item.");
        return false;
    }

    /// <summary>
    /// Setup item cho inventory (tách ra để dùng chung cho AddItem và SetInventoryItems)
    /// </summary>
    private void SetupItemForInventory(GameObject newItem, Transform slotTransform)
    {
        // QUAN TRỌNG: Đảm bảo item có RectTransform TRƯỚC khi setup
        RectTransform rectTransform = newItem.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning($"[InventoryController] Item prefab '{newItem.name}' does not have RectTransform. Adding RectTransform component...");
            rectTransform = newItem.AddComponent<RectTransform>();
        }
        
        // Convert SpriteRenderer thành Image nếu cần (world prefab -> UI prefab)
        SpriteRenderer spriteRenderer = newItem.GetComponent<SpriteRenderer>();
        Image image = newItem.GetComponent<Image>();
        
        if (spriteRenderer != null && image == null)
        {
            Debug.Log($"[InventoryController] Converting world item '{newItem.name}' to UI item (SpriteRenderer -> Image)...");
            
            // Tạo Image component và copy sprite
            image = newItem.AddComponent<Image>();
            if (spriteRenderer.sprite != null)
            {
                image.sprite = spriteRenderer.sprite;
            }
            else
            {
                // Thử lấy từ WeaponInfo nếu SpriteRenderer không có sprite
                Item itemComponent = newItem.GetComponent<Item>();
                if (itemComponent != null && itemComponent.weaponInfo != null && itemComponent.weaponInfo.icon != null)
                {
                    image.sprite = itemComponent.weaponInfo.icon;
                }
            }
            
            image.preserveAspect = true;
            image.raycastTarget = true;
            image.enabled = image.sprite != null;
            
            // Disable SpriteRenderer thay vì Destroy (an toàn hơn)
            spriteRenderer.enabled = false;
        }
        else if (image == null)
        {
            // Nếu không có cả SpriteRenderer và Image, thử lấy sprite từ Item component hoặc WeaponInfo
            Item itemComponent = newItem.GetComponent<Item>();
            if (itemComponent != null)
            {
                image = newItem.AddComponent<Image>();
                
                // Thử lấy sprite từ WeaponInfo
                if (itemComponent.weaponInfo != null && itemComponent.weaponInfo.icon != null)
                {
                    image.sprite = itemComponent.weaponInfo.icon;
                }
                else
                {
                    Debug.LogWarning($"[InventoryController] Item '{newItem.name}' has no sprite! Please assign a sprite to WeaponInfo.icon or add Image/SpriteRenderer component.");
                }
                
                image.preserveAspect = true;
                image.raycastTarget = true;
                image.enabled = image.sprite != null;
            }
        }
        
        // QUAN TRỌNG: Đảm bảo item có parent đúng (slotTransform)
        if (newItem.transform.parent != slotTransform)
        {
            Debug.LogWarning($"[InventoryController] Item parent mismatch! Setting parent to slot.");
            newItem.transform.SetParent(slotTransform, false);
        }
        
        // QUAN TRỌNG: Setup RectTransform SAU KHI đã có parent và Image
        // Force update Canvas để đảm bảo layout được tính toán
        Canvas.ForceUpdateCanvases();
        
        // Setup RectTransform để fit vào slot
        // Đảm bảo anchors và pivot đúng để item không bị snap
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localPosition = Vector3.zero; // Đảm bảo local position = 0
        
        // Set offsetMin và offsetMax (sẽ được AutoFitToSlot override với padding)
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Force update lại sau khi setup
        Canvas.ForceUpdateCanvases();
        
        // Debug log để kiểm tra
        if (rectTransform.parent != slotTransform)
        {
            Debug.LogError($"[InventoryController] CRITICAL: Item '{newItem.name}' parent is not slot! Parent: {rectTransform.parent?.name}, Expected: {slotTransform.name}");
            // Force set parent lại
            rectTransform.SetParent(slotTransform, false);
            Canvas.ForceUpdateCanvases();
        }
        
        // Thêm AutoFitToSlot component nếu chưa có (để item tự động fit vào slot)
        // AutoFitToSlot sẽ tự động setup RectTransform trong Awake() với padding
        AutoFitToSlot autoFit = newItem.GetComponent<AutoFitToSlot>();
        if (autoFit == null)
        {
            autoFit = newItem.AddComponent<AutoFitToSlot>();
            autoFit.padding = 4f;
            autoFit.preserveAspectForImage = true;
        }
        
        // Force update Canvas sau khi AutoFitToSlot được thêm
        Canvas.ForceUpdateCanvases();
        
        // Kiểm tra lại position sau khi setup
        if (rectTransform.anchoredPosition != Vector2.zero)
        {
            Debug.LogWarning($"[InventoryController] Item '{newItem.name}' anchoredPosition is not zero after setup: {rectTransform.anchoredPosition}. Resetting...");
            rectTransform.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        }
        
        // Đảm bảo Image có raycast target để có thể drag
        if (image != null)
        {
            image.raycastTarget = true;
        }
        
        // Đảm bảo item có CanvasGroup để có thể drag
        CanvasGroup canvasGroup = newItem.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = newItem.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        
        // Đảm bảo item có ItemDragHandler để có thể drag
        if (newItem.GetComponent<ItemDragHandler>() == null)
        {
            newItem.AddComponent<ItemDragHandler>();
        }
        
        // Disable các components không cần thiết cho UI
        Rigidbody2D rb = newItem.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false; // Disable physics
        }
        
        Collider2D collider = newItem.GetComponent<Collider2D>();
        if (collider != null && collider.isTrigger == false)
        {
            collider.enabled = false; // Disable collider (không cần trong UI)
        }
    }

    /// <summary>
    /// Tự động tạo slots nếu chưa có
    /// </summary>
    private void CreateSlotsIfNeeded()
    {
        if (inventoryPanel == null || slotPrefab == null)
        {
            Debug.LogError("[InventoryController] Cannot create slots: inventoryPanel or slotPrefab is null!");
            return;
        }

        int currentSlotCount = inventoryPanel.transform.childCount;
        if (currentSlotCount < slotCount)
        {
            Debug.Log($"[InventoryController] Creating {slotCount - currentSlotCount} slots for inventory.");
            for (int i = currentSlotCount; i < slotCount; i++)
            {
                Instantiate(slotPrefab, inventoryPanel.transform);
            }
        }
    }

    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> invData = new List<InventorySaveData>();
        
        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryController] Cannot get inventory items: inventoryPanel is null!");
            return invData;
        }

        int slotCount = inventoryPanel.transform.childCount;
        Debug.Log($"[InventoryController] Getting inventory items. Total slots: {slotCount}");

        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null)
            {
                Debug.LogWarning($"[InventoryController] Slot at index {slotTransform.GetSiblingIndex()} does not have Slot component!");
                continue;
            }

            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null)
                {
                    if (item.ID > 0)
                    {
                        invData.Add(new InventorySaveData 
                        { 
                            itemID = item.ID, 
                            slotIndex = slotTransform.GetSiblingIndex() 
                        });
                        Debug.Log($"[InventoryController] Saving item ID {item.ID} (Name: {item.Name}) from slot {slotTransform.GetSiblingIndex()}.");
                    }
                    else
                    {
                        Debug.LogWarning($"[InventoryController] Item '{item.Name}' in slot {slotTransform.GetSiblingIndex()} has invalid ID: {item.ID}!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[InventoryController] Item in slot {slotTransform.GetSiblingIndex()} does not have Item component! Item name: {slot.currentItem.name}");
                }
            }
        }
        
        Debug.Log($"[InventoryController] Saved {invData.Count} items from inventory (checked {slotCount} slots).");
        return invData;
    }

    /// <summary>
    /// Kiểm tra xem player có item với ID cụ thể trong inventory không
    /// </summary>
    public bool HasItem(int itemID)
    {
        if (inventoryPanel == null)
        {
            Debug.LogWarning("[InventoryController] HasItem: inventoryPanel is null!");
            return false;
        }
        
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null) continue;
            
            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null)
                {
                    if (item.ID == itemID)
                    {
                        Debug.Log($"[InventoryController] Found item ID {itemID} (Name: {item.Name}) in inventory.");
                        return true;
                    }
                }
                else
                {
                    Debug.LogWarning($"[InventoryController] Item in slot {slotTransform.GetSiblingIndex()} does not have Item component!");
                }
            }
        }
        
        Debug.Log($"[InventoryController] Item ID {itemID} not found in inventory.");
        return false;
    }

    /// <summary>
    /// Lấy số lượng item với ID cụ thể trong inventory
    /// </summary>
    public int GetItemCount(int itemID)
    {
        int count = 0;
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null && item.ID == itemID)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public void SetInventoryItems(List<InventorySaveData> inventorySaveData)
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryController] Cannot set inventory items: inventoryPanel is null!");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("[InventoryController] Cannot set inventory items: slotPrefab is null!");
            return;
        }

        if (slotCount <= 0)
        {
            Debug.LogError("[InventoryController] Cannot set inventory items: slotCount is 0 or negative!");
            return;
        }

        if (inventorySaveData == null)
        {
            Debug.LogWarning("[InventoryController] inventorySaveData is null! Clearing inventory.");
            inventorySaveData = new List<InventorySaveData>();
        }

        Debug.Log($"[InventoryController] Setting inventory items. Slot count: {slotCount}, Items to load: {inventorySaveData.Count}");

        // Xóa tất cả slots cũ
        foreach (Transform child in inventoryPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Force update Canvas để đảm bảo layout được tính toán
        Canvas.ForceUpdateCanvases();

        // Tạo lại slots
        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(slotPrefab, inventoryPanel.transform);
        }

        // Force update Canvas sau khi tạo slots
        Canvas.ForceUpdateCanvases();

        // Đảm bảo itemDictionary đã được khởi tạo
        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (itemDictionary == null)
        {
            Debug.LogError("[InventoryController] Cannot set inventory items: ItemDictionary not found!");
            return;
        }

        // Load items vào slots
        int loadedCount = 0;
        foreach (InventorySaveData data in inventorySaveData)
        {
            if (data == null)
            {
                Debug.LogWarning("[InventoryController] Found null InventorySaveData entry, skipping...");
                continue;
            }

            if (data.slotIndex < 0 || data.slotIndex >= slotCount)
            {
                Debug.LogWarning($"[InventoryController] Invalid slot index {data.slotIndex} (must be 0-{slotCount - 1}), skipping item ID {data.itemID}.");
                continue;
            }

            if (data.itemID <= 0)
            {
                Debug.LogWarning($"[InventoryController] Invalid item ID {data.itemID} at slot {data.slotIndex}, skipping...");
                continue;
            }

            Transform slotTransform = inventoryPanel.transform.GetChild(data.slotIndex);
            if (slotTransform == null)
            {
                Debug.LogError($"[InventoryController] Slot transform at index {data.slotIndex} is null!");
                continue;
            }

            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null)
            {
                Debug.LogWarning($"[InventoryController] Slot at index {data.slotIndex} does not have Slot component!");
                continue;
            }

            if (slot.currentItem != null)
            {
                Debug.LogWarning($"[InventoryController] Slot {data.slotIndex} already has an item! Destroying old item...");
                Destroy(slot.currentItem);
            }

            GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
            if (itemPrefab == null)
            {
                Debug.LogWarning($"[InventoryController] Item prefab with ID {data.itemID} not found in ItemDictionary!");
                continue;
            }

            // Instantiate item
            GameObject newItem = Instantiate(itemPrefab, slotTransform, false);
            
            // Nếu item có WorldItemUIHandler, enable UI components
            WorldItemUIHandler worldItemHandler = newItem.GetComponent<WorldItemUIHandler>();
            if (worldItemHandler != null)
            {
                worldItemHandler.OnAddedToInventory();
            }
            
            // Setup item giống như trong AddItem
            SetupItemForInventory(newItem, slotTransform);
            
            slot.currentItem = newItem;
            loadedCount++;
            Debug.Log($"[InventoryController] Loaded item ID {data.itemID} to inventory slot {data.slotIndex}.");
        }

        Debug.Log($"[InventoryController] Successfully loaded {loadedCount} out of {inventorySaveData.Count} items into inventory.");
    }
}
