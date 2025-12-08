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
    // Ưu tiên dùng singleton
    if (ItemDictionary.Instance != null)
    {
        itemDictionary = ItemDictionary.Instance;
    }
    else
    {
        itemDictionary = FindFirstObjectByType<ItemDictionary>();
    }

    if (itemDictionary == null)
    {
        Debug.LogError("[InventoryController] ItemDictionary not found in scene! Inventory items will not be able to load.");
    }

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
                
                // QUAN TRỌNG: Stop và disable ItemDropEffect nếu có (để tránh tweens can thiệp vào inventory UI)
                ItemDropEffect dropEffect = newItem.GetComponent<ItemDropEffect>();
                if (dropEffect != null)
                {
                    dropEffect.StopEffect();
                    // Disable component để tránh tự động start lại trong OnEnable
                    dropEffect.enabled = false;
                    Debug.Log($"[InventoryController] Stopped and disabled ItemDropEffect on {newItem.name}.");
                }
                
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
        
        // QUAN TRỌNG: Trong UI Canvas, luôn dùng Image, không dùng SpriteRenderer
        // Nếu có cả SpriteRenderer và Image, disable SpriteRenderer và đảm bảo Image có sprite
        if (spriteRenderer != null && image != null)
        {
            Debug.Log($"[InventoryController] Item '{newItem.name}' has both SpriteRenderer and Image. Disabling SpriteRenderer and using Image for UI...");
            
            // Đảm bảo Image có sprite (ưu tiên từ Image, nếu không có thì lấy từ SpriteRenderer)
            if (image.sprite == null && spriteRenderer.sprite != null)
            {
                image.sprite = spriteRenderer.sprite;
                Debug.Log($"[InventoryController] Copied sprite from SpriteRenderer to Image for '{newItem.name}'.");
            }
            
            // Enable Image và disable SpriteRenderer (vì đang trong UI Canvas)
            image.enabled = image.sprite != null;
            spriteRenderer.enabled = false;
        }
        else if (spriteRenderer != null && image == null)
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
        
        // QUAN TRỌNG: Tính toán size từ sprite TRƯỚC KHI setup RectTransform
        // Lấy sprite để tính size (ưu tiên Image, sau đó SpriteRenderer)
        Sprite itemSprite = null;
        if (image != null && image.sprite != null)
        {
            itemSprite = image.sprite;
        }
        else if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            itemSprite = spriteRenderer.sprite;
        }
        
        // Tính size từ sprite nếu có
        Vector2 spriteSize = Vector2.zero;
        if (itemSprite != null)
        {
            spriteSize = itemSprite.rect.size;
            Debug.Log($"[InventoryController] Item '{newItem.name}' sprite size: {spriteSize}");
        }
        
        // QUAN TRỌNG: Đảm bảo item có parent đúng (slotTransform)
        // Set parent TRƯỚC KHI setup RectTransform để đảm bảo coordinates đúng
        if (newItem.transform.parent != slotTransform)
        {
            Debug.LogWarning($"[InventoryController] Item parent mismatch! Setting parent to slot. Current parent: {newItem.transform.parent?.name}, Expected: {slotTransform.name}");
            newItem.transform.SetParent(slotTransform, false);
        }
        
        // QUAN TRỌNG: Setup RectTransform SAU KHI đã có parent và Image
        // Force update Canvas để đảm bảo layout được tính toán
        Canvas.ForceUpdateCanvases();
        
        // QUAN TRỌNG: Reset tất cả transform properties trước khi setup
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
        
        // Setup RectTransform để fit vào slot
        // Đảm bảo anchors và pivot đúng để item không bị snap
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // QUAN TRỌNG: Với stretch anchors (0-1), sizeDelta phải = 0
        // AutoFitToSlot sẽ dùng offsetMin/offsetMax để tạo padding và size sẽ được tính từ parent
        rectTransform.sizeDelta = Vector2.zero;
        
        // Force update lại để đảm bảo tất cả changes được apply
        Canvas.ForceUpdateCanvases();
        
        // Set offsetMin và offsetMax (sẽ được AutoFitToSlot override với padding)
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Force update lại sau khi setup
        Canvas.ForceUpdateCanvases();
        
        // Debug log để kiểm tra và đảm bảo parent đúng
        if (rectTransform.parent != slotTransform)
        {
            Debug.LogError($"[InventoryController] CRITICAL: Item '{newItem.name}' parent is not slot! Parent: {rectTransform.parent?.name}, Expected: {slotTransform.name}");
            // Force set parent lại và reset position
            rectTransform.SetParent(slotTransform, false);
            rectTransform.localPosition = Vector3.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        }
        
        // QUAN TRỌNG: Đảm bảo item không bị positioned ở giữa inventory panel
        // Kiểm tra nếu parent là inventory panel thay vì slot
        InventoryController invController = slotTransform.GetComponentInParent<InventoryController>();
        if (invController != null && invController.inventoryPanel != null)
        {
            if (rectTransform.parent == invController.inventoryPanel.transform)
            {
                Debug.LogError($"[InventoryController] CRITICAL: Item '{newItem.name}' is parented to inventory panel instead of slot! Fixing...");
                rectTransform.SetParent(slotTransform, false);
                rectTransform.localPosition = Vector3.zero;
                rectTransform.anchoredPosition = Vector2.zero;
                Canvas.ForceUpdateCanvases();
            }
        }
        
        // Thêm AutoFitToSlot component nếu chưa có (để item tự động fit vào slot)
        AutoFitToSlot autoFit = newItem.GetComponent<AutoFitToSlot>();
        if (autoFit == null)
        {
            autoFit = newItem.AddComponent<AutoFitToSlot>();
            autoFit.padding = 4f;
            autoFit.preserveAspectForImage = true;
        }
        else
        {
            // Nếu đã có, đảm bảo padding đúng
            autoFit.padding = 4f;
            autoFit.preserveAspectForImage = true;
        }
        
        // QUAN TRỌNG: Force apply AutoFitToSlot ngay lập tức (không đợi Awake)
        autoFit.Apply();
        
        // Force update Canvas sau khi AutoFitToSlot được apply
        Canvas.ForceUpdateCanvases();
        
        // Kiểm tra lại position và size sau khi setup
        if (rectTransform.anchoredPosition != Vector2.zero)
        {
            Debug.LogWarning($"[InventoryController] Item '{newItem.name}' anchoredPosition is not zero after setup: {rectTransform.anchoredPosition}. Resetting...");
            rectTransform.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        }
        
        // Đảm bảo item không bị lệch ra ngoài slot
        RectTransform slotRect = slotTransform as RectTransform;
        if (slotRect != null && rectTransform != null)
        {
            // Force update để đảm bảo rect size được tính toán đúng
            Canvas.ForceUpdateCanvases();
            
            Vector2 slotSize = slotRect.rect.size;
            Vector2 itemSize = rectTransform.rect.size;
            
            // Kiểm tra slot size và item size hợp lệ
            if (slotSize.x > 0 && slotSize.y > 0 && itemSize.x > 0 && itemSize.y > 0)
            {
                // Nếu item size lớn hơn slot size (sau khi trừ padding), scale down
                float padding = autoFit != null ? autoFit.padding : 4f;
                float maxItemSize = Mathf.Min(slotSize.x, slotSize.y) - (padding * 2);
                
                // Đảm bảo maxItemSize > 0
                if (maxItemSize > 0)
                {
                    float maxItemDimension = Mathf.Max(itemSize.x, itemSize.y);
                    
                    // Đảm bảo không chia cho 0
                    if (maxItemDimension > 0 && maxItemDimension > maxItemSize)
                    {
                        float scale = maxItemSize / maxItemDimension;
                        
                        // Đảm bảo scale hợp lệ (0 < scale <= 1)
                        scale = Mathf.Clamp(scale, 0.1f, 1f);
                        
                        if (float.IsFinite(scale) && scale > 0)
                        {
                            rectTransform.localScale = Vector3.one * scale;
                            Canvas.ForceUpdateCanvases();
                            Debug.Log($"[InventoryController] Scaled down item '{newItem.name}' to fit in slot. Scale: {scale}, SlotSize: {slotSize}, ItemSize: {itemSize}, MaxItemSize: {maxItemSize}");
                        }
                        else
                        {
                            Debug.LogWarning($"[InventoryController] Invalid scale calculated: {scale}. Using default scale 1.");
                            rectTransform.localScale = Vector3.one;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[InventoryController] Invalid sizes - SlotSize: {slotSize}, ItemSize: {itemSize}. Using default scale 1.");
                rectTransform.localScale = Vector3.one;
            }
        }
        else
        {
            // Đảm bảo scale = 1 nếu không thể tính toán
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
        }
        
        // Đảm bảo Image có raycast target và được cấu hình đúng
        // QUAN TRỌNG: Tìm lại Image component vì có thể đã được thêm sau
        image = newItem.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            image.preserveAspect = true;
            // Đảm bảo Image được enable nếu có sprite
            if (image.sprite != null)
            {
                image.enabled = true;
                // QUAN TRỌNG: Reset color về white (alpha = 1) để đảm bảo không bị ảnh hưởng bởi ItemDropEffect
                image.color = Color.white;
                Debug.Log($"[InventoryController] Image enabled for '{newItem.name}' with sprite: {image.sprite.name}, color reset to white.");
            }
            else
            {
                Debug.LogWarning($"[InventoryController] Image component exists but has no sprite for '{newItem.name}'!");
            }
        }
        else
        {
            Debug.LogWarning($"[InventoryController] No Image component found for '{newItem.name}' after setup!");
        }
        
        // QUAN TRỌNG: Reset SpriteRenderer color nếu có (để đảm bảo không bị ảnh hưởng bởi ItemDropEffect)
        // Sử dụng lại biến spriteRenderer đã khai báo ở đầu method
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            Debug.Log($"[InventoryController] SpriteRenderer color reset to white for '{newItem.name}'.");
        }
        
        // Đảm bảo GameObject được active
        if (!newItem.activeSelf)
        {
            Debug.LogWarning($"[InventoryController] Item '{newItem.name}' is not active! Activating...");
            newItem.SetActive(true);
        }
        
        // Đảm bảo item có CanvasGroup để có thể drag
        CanvasGroup canvasGroup = newItem.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = newItem.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        
        // Đảm bảo CanvasGroup không block visibility
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f; // Đảm bảo alpha = 1 (không transparent)
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            Debug.Log($"[InventoryController] CanvasGroup alpha set to 1 for '{newItem.name}'");
        }
        
        // Đảm bảo RectTransform có size > 0 SAU KHI AutoFitToSlot đã apply
        // Với stretch anchors, size được tính từ parent size - offsets
        if (rectTransform != null)
        {
            // Force update để đảm bảo rect size được tính toán sau khi AutoFitToSlot apply
            Canvas.ForceUpdateCanvases();
            
            // Kiểm tra rect size (sau khi AutoFitToSlot đã setup offsets)
            Vector2 rectSize = rectTransform.rect.size;
            
            // Kiểm tra parent (slot) có size hợp lệ không
            RectTransform slotRectTransform = slotTransform as RectTransform;
            Vector2 slotSize = slotRectTransform != null ? slotRectTransform.rect.size : Vector2.zero;
            
            // Nếu slot có size nhưng item size vẫn = 0, có thể do offsets hoặc parent chưa được layout
            if (slotSize.x > 0 && slotSize.y > 0 && (rectSize.x <= 0.1f || rectSize.y <= 0.1f))
            {
                Debug.LogWarning($"[InventoryController] Item '{newItem.name}' has zero rect size but slot has size {slotSize}. " +
                    $"Rect size: {rectSize}, Offsets: min={rectTransform.offsetMin}, max={rectTransform.offsetMax}. " +
                    $"This may be a layout timing issue. Item should resize automatically.");
                
                // Không force set sizeDelta vì với stretch anchors, sizeDelta = 0 là đúng
                // AutoFitToSlot đã set offsets, size sẽ được tính tự động khi layout updates
                // Chỉ log warning, không force fix vì có thể là timing issue
            }
            else if (slotSize.x <= 0.1f || slotSize.y <= 0.1f)
            {
                Debug.LogWarning($"[InventoryController] Slot '{slotTransform.name}' has zero size! This will cause item '{newItem.name}' to have zero size. " +
                    $"Slot size: {slotSize}. Please check slot RectTransform setup.");
            }
            
            // Log thông tin để debug
            Debug.Log($"[InventoryController] Item '{newItem.name}' setup complete - Slot size: {slotSize}, Item rect size: {rectSize}, " +
                $"Sprite size: {spriteSize}, SizeDelta: {rectTransform.sizeDelta}, Offsets: min={rectTransform.offsetMin}, max={rectTransform.offsetMax}");
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
    if (ItemDictionary.Instance != null)
    {
        itemDictionary = ItemDictionary.Instance;
    }
    else
    {
        itemDictionary = FindFirstObjectByType<ItemDictionary>();
    }
}

if (itemDictionary == null)
{
    Debug.LogError("[InventoryController] Cannot set inventory items: ItemDictionary not found (even after trying singleton + FindFirstObjectByType)!");
    return;
}

Debug.Log($"[InventoryController] ItemDictionary found. Attempting to load {inventorySaveData.Count} items...");
        
        Debug.Log($"[InventoryController] ItemDictionary found. Attempting to load {inventorySaveData.Count} items...");

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
                Debug.LogError($"[InventoryController] Item prefab with ID {data.itemID} not found in ItemDictionary! Slot: {data.slotIndex}. " +
                    $"Make sure ItemDictionary is initialized and contains item with ID {data.itemID}.");
                continue;
            }
            
            Debug.Log($"[InventoryController] Found item prefab for ID {data.itemID}: {itemPrefab.name}");

            // Instantiate item
            GameObject newItem = Instantiate(itemPrefab, slotTransform, false);
            
            // QUAN TRỌNG: Stop và disable ItemDropEffect nếu có (để tránh tweens can thiệp vào inventory UI)
            ItemDropEffect dropEffect = newItem.GetComponent<ItemDropEffect>();
            if (dropEffect != null)
            {
                dropEffect.StopEffect();
                // Disable component để tránh tự động start lại trong OnEnable
                dropEffect.enabled = false;
                Debug.Log($"[InventoryController] Stopped and disabled ItemDropEffect on {newItem.name} during load.");
            }
            
            // Nếu item có WorldItemUIHandler, enable UI components
            WorldItemUIHandler worldItemHandler = newItem.GetComponent<WorldItemUIHandler>();
            if (worldItemHandler != null)
            {
                worldItemHandler.OnAddedToInventory();
            }
            
            // Setup item giống như trong AddItem
            SetupItemForInventory(newItem, slotTransform);
            
            // Debug: Kiểm tra item sau khi setup
            RectTransform itemRect = newItem.GetComponent<RectTransform>();
            Image itemImage = newItem.GetComponent<Image>();
            CanvasGroup itemCanvasGroup = newItem.GetComponent<CanvasGroup>();
            
            Debug.Log($"[InventoryController] Item '{newItem.name}' setup complete:");
            Debug.Log($"  - Active: {newItem.activeSelf}");
            Debug.Log($"  - Parent: {newItem.transform.parent?.name}");
            Debug.Log($"  - RectTransform size: {itemRect?.rect.size}");
            Debug.Log($"  - RectTransform anchoredPosition: {itemRect?.anchoredPosition}");
            Debug.Log($"  - RectTransform localScale: {newItem.transform.localScale}");
            Debug.Log($"  - Image enabled: {itemImage?.enabled}");
            Debug.Log($"  - Image sprite: {itemImage?.sprite?.name ?? "NULL"}");
            Debug.Log($"  - Image color: {itemImage?.color}");
            Debug.Log($"  - CanvasGroup alpha: {itemCanvasGroup?.alpha}");
            Debug.Log($"  - CanvasGroup blocksRaycasts: {itemCanvasGroup?.blocksRaycasts}");
            
            slot.currentItem = newItem;
            loadedCount++;
            Debug.Log($"[InventoryController] Loaded item ID {data.itemID} to inventory slot {data.slotIndex}.");
        }

        Debug.Log($"[InventoryController] Successfully loaded {loadedCount} out of {inventorySaveData.Count} items into inventory.");
    }
}
