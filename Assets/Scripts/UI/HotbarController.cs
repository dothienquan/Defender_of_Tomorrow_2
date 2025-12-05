using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    public GameObject hotbarPanel;
    public GameObject slotPrefab;
    public int slotCount = 10;

    private ItemDictionary itemDictionary;

    private Key[] hotbarKeys;

    private void Awake()
    {
        itemDictionary = FindFirstObjectByType<ItemDictionary>();

        hotbarKeys = new Key[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            hotbarKeys[i] = i < 9 ? (Key)((int)Key.Digit1 + i) : Key.Digit0;
        }
    }

    private void Start()
    {
        // Tự động tạo slots nếu hotbarPanel chưa có slots
        if (hotbarPanel != null && slotPrefab != null)
        {
            int currentSlotCount = hotbarPanel.transform.childCount;
            if (currentSlotCount < slotCount)
            {
                Debug.Log($"[HotbarController] Tự động tạo {slotCount - currentSlotCount} slots cho hotbar.");
                for (int i = currentSlotCount; i < slotCount; i++)
                {
                    Instantiate(slotPrefab, hotbarPanel.transform);
                }
            }
        }
        else
        {
            if (hotbarPanel == null)
                Debug.LogWarning("[HotbarController] HotbarPanel chưa được gán!");
            if (slotPrefab == null)
                Debug.LogWarning("[HotbarController] SlotPrefab chưa được gán!");
        }
    }


    // NOTE: Keyboard input được xử lý bởi ActiveInventory thông qua PlayerControls
    // HotbarController chỉ quản lý slots và items, không xử lý input trực tiếp
    // void Update() đã được xóa để tránh xung đột với ActiveInventory

    /// <summary>
    /// Sử dụng item trong slot (được gọi từ ActiveInventory khi nhấn số)
    /// </summary>
    public void UseItemInSlot(int index)
    {
        if (hotbarPanel == null || index < 0 || index >= hotbarPanel.transform.childCount) return;

        Slot slot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
        if (slot == null || slot.currentItem == null) return;

        Item item = slot.currentItem.GetComponent<Item>();
        if (item != null)
        {
            // Nếu là vũ khí, equip vào ActiveInventory
            if (item.IsWeapon)
            {
                EquipWeaponFromSlot(slot, item);
            }
            else
            {
                // Sử dụng item thông thường
                item.UseItem();
            }
        }
    }

    /// <summary>
    /// Equip vũ khí từ slot vào ActiveInventory
    /// </summary>
    private void EquipWeaponFromSlot(Slot slot, Item item)
    {
        if (item.weaponInfo == null) return;

        // Đảm bảo InventorySlot có WeaponInfo
        InventorySlot inventorySlot = slot.GetComponent<InventorySlot>();
        if (inventorySlot == null)
        {
            inventorySlot = slot.gameObject.AddComponent<InventorySlot>();
        }
        
        // Set weapon info
        inventorySlot.SetWeapon(item.weaponInfo);

        // ActiveInventory sẽ tự động highlight và equip weapon khi SetActiveSlot được gọi
    }

    public List<InventorySaveData> GetHotbarItems()
    {
        List<InventorySaveData> hotbarData = new List<InventorySaveData>();
        
        if (hotbarPanel == null)
        {
            Debug.LogError("[HotbarController] Cannot get hotbar items: hotbarPanel is null!");
            return hotbarData;
        }

        int slotCount = hotbarPanel.transform.childCount;
        Debug.Log($"[HotbarController] Getting hotbar items. Total slots: {slotCount}");

        foreach (Transform slotTransform in hotbarPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null)
            {
                Debug.LogWarning($"[HotbarController] Slot at index {slotTransform.GetSiblingIndex()} does not have Slot component!");
                continue;
            }

            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null)
                {
                    if (item.ID > 0)
                    {
                        hotbarData.Add(new InventorySaveData 
                        { 
                            itemID = item.ID, 
                            slotIndex = slotTransform.GetSiblingIndex() 
                        });
                        Debug.Log($"[HotbarController] Saving item ID {item.ID} (Name: {item.Name}) from slot {slotTransform.GetSiblingIndex()}.");
                    }
                    else
                    {
                        Debug.LogWarning($"[HotbarController] Item '{item.Name}' in slot {slotTransform.GetSiblingIndex()} has invalid ID: {item.ID}!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[HotbarController] Item in slot {slotTransform.GetSiblingIndex()} does not have Item component! Item name: {slot.currentItem.name}");
                }
            }
        }
        
        Debug.Log($"[HotbarController] Saved {hotbarData.Count} items from hotbar (checked {slotCount} slots).");
        return hotbarData;
    }

    public void SetHotbarItems(List<InventorySaveData> inventorySaveData)
    {
        if (hotbarPanel == null || slotPrefab == null)
        {
            Debug.LogError("[HotbarController] Cannot set hotbar items: hotbarPanel or slotPrefab is null!");
            return;
        }

        // Đảm bảo itemDictionary đã được khởi tạo
        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (itemDictionary == null)
        {
            Debug.LogError("[HotbarController] Cannot set hotbar items: ItemDictionary not found!");
            return;
        }

        // Xóa tất cả slots cũ
        foreach (Transform child in hotbarPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Tạo lại slots
        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(slotPrefab, hotbarPanel.transform);
        }

        // Load items vào slots
        foreach (InventorySaveData data in inventorySaveData)
        {
            if (data.slotIndex < slotCount && data.slotIndex >= 0)
            {
                Transform slotTransform = hotbarPanel.transform.GetChild(data.slotIndex);
                Slot slot = slotTransform.GetComponent<Slot>();
                
                if (slot == null)
                {
                    Debug.LogWarning($"[HotbarController] Slot at index {data.slotIndex} does not have Slot component!");
                    continue;
                }

                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
                    // QUAN TRỌNG: Instantiate với worldPositionStays = false để item được đặt đúng trong UI space
                    GameObject newItem = Instantiate(itemPrefab, slotTransform, false);
                    
                    // Nếu item có WorldItemUIHandler, enable UI components
                    WorldItemUIHandler worldItemHandler = newItem.GetComponent<WorldItemUIHandler>();
                    if (worldItemHandler != null)
                    {
                        worldItemHandler.OnAddedToInventory();
                    }
                    
                    // Setup item giống như trong InventoryController
                    SetupItemForHotbar(newItem, slotTransform);
                    
                    // Debug: Kiểm tra item sau khi setup
                    RectTransform itemRect = newItem.GetComponent<RectTransform>();
                    Image itemImage = newItem.GetComponent<Image>();
                    CanvasGroup itemCanvasGroup = newItem.GetComponent<CanvasGroup>();
                    
                    Debug.Log($"[HotbarController] Item '{newItem.name}' setup complete:");
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
                    Debug.Log($"[HotbarController] Loaded item ID {data.itemID} to hotbar slot {data.slotIndex}.");
                }
                else
                {
                    Debug.LogWarning($"[HotbarController] Item prefab with ID {data.itemID} not found in ItemDictionary!");
                }
            }
        }

        Debug.Log($"[HotbarController] Loaded {inventorySaveData.Count} items into hotbar.");
    }

    /// <summary>
    /// Setup item cho hotbar (tương tự như SetupItemForInventory trong InventoryController)
    /// </summary>
    private void SetupItemForHotbar(GameObject newItem, Transform slotTransform)
    {
        // QUAN TRỌNG: Đảm bảo item có RectTransform TRƯỚC khi setup
        RectTransform rectTransform = newItem.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning($"[HotbarController] Item prefab '{newItem.name}' does not have RectTransform. Adding RectTransform component...");
            rectTransform = newItem.AddComponent<RectTransform>();
        }
        
        // Convert SpriteRenderer thành Image nếu cần (world prefab -> UI prefab)
        SpriteRenderer spriteRenderer = newItem.GetComponent<SpriteRenderer>();
        Image image = newItem.GetComponent<Image>();
        
        // QUAN TRỌNG: Trong UI Canvas, luôn dùng Image, không dùng SpriteRenderer
        // Nếu có cả SpriteRenderer và Image, disable SpriteRenderer và đảm bảo Image có sprite
        if (spriteRenderer != null && image != null)
        {
            Debug.Log($"[HotbarController] Item '{newItem.name}' has both SpriteRenderer and Image. Disabling SpriteRenderer and using Image for UI...");
            
            // Đảm bảo Image có sprite (ưu tiên từ Image, nếu không có thì lấy từ SpriteRenderer)
            if (image.sprite == null && spriteRenderer.sprite != null)
            {
                image.sprite = spriteRenderer.sprite;
                Debug.Log($"[HotbarController] Copied sprite from SpriteRenderer to Image for '{newItem.name}'.");
            }
            
            // Enable Image và disable SpriteRenderer (vì đang trong UI Canvas)
            image.enabled = image.sprite != null;
            spriteRenderer.enabled = false;
        }
        else if (spriteRenderer != null && image == null)
        {
            Debug.Log($"[HotbarController] Converting world item '{newItem.name}' to UI item (SpriteRenderer -> Image)...");
            
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
                    Debug.LogWarning($"[HotbarController] Item '{newItem.name}' has no sprite! Please assign a sprite to WeaponInfo.icon or add Image/SpriteRenderer component.");
                }
                
                image.preserveAspect = true;
                image.raycastTarget = true;
                image.enabled = image.sprite != null;
            }
        }
        
        // QUAN TRỌNG: Đảm bảo item có parent đúng (slotTransform)
        if (newItem.transform.parent != slotTransform)
        {
            Debug.LogWarning($"[HotbarController] Item parent mismatch! Setting parent to slot.");
            newItem.transform.SetParent(slotTransform, false);
        }
        
        // QUAN TRỌNG: Setup RectTransform SAU KHI đã có parent và Image
        // Force update Canvas để đảm bảo layout được tính toán
        Canvas.ForceUpdateCanvases();
        
        // Setup RectTransform để fit vào slot
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localPosition = Vector3.zero;
        
        // Set offsetMin và offsetMax (sẽ được AutoFitToSlot override với padding)
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Force update lại sau khi setup
        Canvas.ForceUpdateCanvases();
        
        // Thêm AutoFitToSlot component nếu chưa có
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
                            Debug.Log($"[HotbarController] Scaled down item '{newItem.name}' to fit in slot. Scale: {scale}, SlotSize: {slotSize}, ItemSize: {itemSize}, MaxItemSize: {maxItemSize}");
                        }
                        else
                        {
                            Debug.LogWarning($"[HotbarController] Invalid scale calculated: {scale}. Using default scale 1.");
                            rectTransform.localScale = Vector3.one;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[HotbarController] Invalid sizes - SlotSize: {slotSize}, ItemSize: {itemSize}. Using default scale 1.");
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
                // Đảm bảo color alpha = 1 (không transparent)
                Color imgColor = image.color;
                imgColor.a = 1f;
                image.color = imgColor;
                Debug.Log($"[HotbarController] Image enabled for '{newItem.name}' with sprite: {image.sprite.name}");
            }
            else
            {
                Debug.LogWarning($"[HotbarController] Image component exists but has no sprite for '{newItem.name}'!");
            }
        }
        else
        {
            Debug.LogWarning($"[HotbarController] No Image component found for '{newItem.name}' after setup!");
        }
        
        // Đảm bảo GameObject được active
        if (!newItem.activeSelf)
        {
            Debug.LogWarning($"[HotbarController] Item '{newItem.name}' is not active! Activating...");
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
            Debug.Log($"[HotbarController] CanvasGroup alpha set to 1 for '{newItem.name}'");
        }
        
        // Đảm bảo RectTransform có size > 0
        if (rectTransform != null)
        {
            // Force set size nếu size = 0
            if (rectTransform.rect.size.x <= 0 || rectTransform.rect.size.y <= 0)
            {
                Debug.LogWarning($"[HotbarController] Item '{newItem.name}' has zero size! Setting default size...");
                rectTransform.sizeDelta = new Vector2(100, 100); // Default size
                Canvas.ForceUpdateCanvases();
            }
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

        // Nếu là weapon, đảm bảo InventorySlot có WeaponInfo
        Item item = newItem.GetComponent<Item>();
        if (item != null && item.IsWeapon)
        {
            InventorySlot inventorySlot = slotTransform.GetComponent<InventorySlot>();
            if (inventorySlot == null)
            {
                inventorySlot = slotTransform.gameObject.AddComponent<InventorySlot>();
            }
            inventorySlot.SetWeapon(item.weaponInfo);
        }
    }
}
