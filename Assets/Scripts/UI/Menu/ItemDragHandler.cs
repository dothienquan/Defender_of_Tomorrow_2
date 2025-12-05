using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;
    RectTransform rectTransform;
    Canvas canvas;
    private int originalSiblingIndex;
    private Vector3 originalScale;
    private Vector2 originalSizeDelta;
    private Vector2 originalAnchoredPosition;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;
    private Vector3 originalParentScale;
    private Vector2 originalWorldSize; // Kích thước world space của item
    private Animator animator; // Animator component (nếu có)
    private bool originalAnimatorEnabled; // Trạng thái ban đầu của Animator
    private AutoFitToSlot autoFitToSlot; // AutoFitToSlot component (nếu có)
    private bool originalAutoFitEnabled; // Trạng thái ban đầu của AutoFitToSlot

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Handle left-click to automatically move item to first empty hotbar slot
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only handle left mouse button clicks
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Check if item is already in hotbar - if so, don't do anything
        Slot currentSlot = transform.parent?.GetComponent<Slot>();
        if (currentSlot == null)
            return;

        // Check if current slot is in hotbar
        HotbarController hotbarController = FindFirstObjectByType<HotbarController>();
        if (hotbarController == null || hotbarController.hotbarPanel == null)
            return;

        // Check if item is already in hotbar
        bool isInHotbar = false;
        foreach (Transform slotTransform in hotbarController.hotbarPanel.transform)
        {
            if (slotTransform == currentSlot.transform)
            {
                isInHotbar = true;
                break;
            }
        }

        // If already in hotbar, don't do anything
        if (isInHotbar)
            return;

        // Find first empty hotbar slot
        Slot emptyHotbarSlot = FindFirstEmptyHotbarSlot(hotbarController);
        if (emptyHotbarSlot == null)
        {
            Debug.Log("[ItemDragHandler] Hotbar is full. Cannot add item.");
            return;
        }

        // Move item to empty hotbar slot
        MoveItemToSlot(currentSlot, emptyHotbarSlot);
    }

    /// <summary>
    /// Find the first empty slot in hotbar
    /// </summary>
    private Slot FindFirstEmptyHotbarSlot(HotbarController hotbarController)
    {
        if (hotbarController.hotbarPanel == null)
            return null;

        foreach (Transform slotTransform in hotbarController.hotbarPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem == null)
            {
                return slot;
            }
        }

        return null;
    }

    /// <summary>
    /// Move item from source slot to target slot
    /// </summary>
    private void MoveItemToSlot(Slot sourceSlot, Slot targetSlot)
    {
        if (sourceSlot == null || targetSlot == null)
            return;

        // Clear source slot
        sourceSlot.currentItem = null;

        // Move item to target slot
        transform.SetParent(targetSlot.transform);
        targetSlot.currentItem = gameObject;
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // Handle weapon-specific logic
        Item item = GetComponent<Item>();
        if (item != null && item.IsWeapon)
        {
            // Update InventorySlot at new slot
            InventorySlot inventorySlot = targetSlot.GetComponent<InventorySlot>();
            if (inventorySlot == null)
            {
                inventorySlot = targetSlot.gameObject.AddComponent<InventorySlot>();
            }
            inventorySlot.SetWeapon(item.weaponInfo);

            // Clear InventorySlot at old slot (if any)
            InventorySlot oldInventorySlot = sourceSlot.GetComponent<InventorySlot>();
            if (oldInventorySlot != null)
            {
                oldInventorySlot.SetWeapon(null);
            }

            // Refresh ActiveInventory to update equipped weapon
            if (ActiveInventory.Instance != null)
            {
                ActiveInventory.Instance.RefreshActiveWeapon();
            }
        }

        Debug.Log($"[ItemDragHandler] Moved item '{item?.Name}' to hotbar slot {targetSlot.transform.GetSiblingIndex()}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent; //Save OG parent
        originalSiblingIndex = transform.GetSiblingIndex();
        
        // Lưu tất cả RectTransform properties
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        if (rectTransform != null)
        {
            originalScale = transform.localScale;
            originalSizeDelta = rectTransform.sizeDelta;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            originalAnchorMin = rectTransform.anchorMin;
            originalAnchorMax = rectTransform.anchorMax;
            originalPivot = rectTransform.pivot;
            originalParentScale = originalParent != null ? originalParent.localScale : Vector3.one;
            
            // Lưu kích thước visual thực tế (kích thước hiển thị trên màn hình)
            // Sử dụng rect.size để lấy kích thước local, sau đó nhân với lossyScale để có world size
            Vector2 localRectSize = rectTransform.rect.size;
            Vector3 currentLossyScale = rectTransform.lossyScale;
            originalWorldSize = new Vector2(
                localRectSize.x * currentLossyScale.x,
                localRectSize.y * currentLossyScale.y
            );
        }
        
        // Tìm hoặc tạo drag container với scale = 1 để tránh ảnh hưởng của CanvasScaler
        Transform targetParent = null;
        if (canvas != null)
        {
            // Tìm drag container
            Transform dragContainer = canvas.transform.Find("DragContainer");
            if (dragContainer == null)
            {
                // Tạo drag container mới với scale = 1
                GameObject containerObj = new GameObject("DragContainer");
                containerObj.transform.SetParent(canvas.transform, false);
                RectTransform containerRect = containerObj.AddComponent<RectTransform>();
                containerRect.anchorMin = Vector2.zero;
                containerRect.anchorMax = Vector2.one;
                containerRect.sizeDelta = Vector2.zero;
                containerRect.anchoredPosition = Vector2.zero;
                containerObj.transform.localScale = Vector3.one; // QUAN TRỌNG: scale = 1
                dragContainer = containerObj.transform;
            }
            targetParent = dragContainer;
        }
        else
        {
            targetParent = transform.root;
        }
        
        // Set parent
        transform.SetParent(targetParent, false);
        
        // Tính toán sizeDelta mới để giữ nguyên kích thước visual (không bị ảnh hưởng bởi CanvasScaler)
        if (rectTransform != null)
        {
            // Lấy CanvasScaler để tính scale factor
            UnityEngine.UI.CanvasScaler canvasScaler = canvas != null ? canvas.GetComponent<UnityEngine.UI.CanvasScaler>() : null;
            float canvasScaleFactor = 1f;
            
            if (canvasScaler != null)
            {
                if (canvasScaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    // Tính scale factor từ CanvasScaler
                    float scaleX = Screen.width / canvasScaler.referenceResolution.x;
                    float scaleY = Screen.height / canvasScaler.referenceResolution.y;
                    float match = canvasScaler.matchWidthOrHeight;
                    canvasScaleFactor = Mathf.Lerp(scaleX, scaleY, match);
                }
                else if (canvasScaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    canvasScaleFactor = canvasScaler.scaleFactor;
                }
            }
            
            // Tính sizeDelta mới
            // originalWorldSize là kích thước trên màn hình (đã bao gồm CanvasScaler)
            // DragContainer có scale = 1, nhưng vẫn bị ảnh hưởng bởi CanvasScaler của Canvas
            // Nên cần chia cho canvasScaleFactor để có sizeDelta đúng
            Vector2 newSizeDelta = new Vector2(
                canvasScaleFactor != 0 ? originalWorldSize.x / canvasScaleFactor : originalSizeDelta.x,
                canvasScaleFactor != 0 ? originalWorldSize.y / canvasScaleFactor : originalSizeDelta.y
            );
            
            // Set sizeDelta để giữ nguyên kích thước visual
            rectTransform.sizeDelta = newSizeDelta;
            
            // Set scale = 1 để không bị scale thêm
            transform.localScale = Vector3.one;
            
            // Set anchor và pivot để dễ di chuyển
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            
            // Đảm bảo RectTransform không bị lock bởi constraints
            // Unity không có API trực tiếp để set constraints, nhưng đảm bảo anchoredPosition có thể thay đổi
            // Force update để đảm bảo RectTransform được apply đúng
            Canvas.ForceUpdateCanvases();
        }
        
        // Đảm bảo item ở trên cùng
        transform.SetAsLastSibling();
        
        // Disable Animator nếu có (để tránh can thiệp vào position)
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            originalAnimatorEnabled = animator.enabled;
            animator.enabled = false; // Disable Animator khi drag
        }
        
        // Disable AutoFitToSlot nếu có (để tránh can thiệp vào position khi drag)
        autoFitToSlot = GetComponent<AutoFitToSlot>();
        if (autoFitToSlot != null)
        {
            originalAutoFitEnabled = autoFitToSlot.enabled;
            autoFitToSlot.enabled = false; // Disable AutoFitToSlot khi drag
        }
        
        // Setup canvas group
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 1f; // Không transparent - hiển thị rõ ràng
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        if (canvas != null && rectTransform != null)
        {
            // Lấy parent hiện tại (có thể là DragContainer)
            Transform currentParent = rectTransform.parent;
            RectTransform parentRect = currentParent != null ? currentParent as RectTransform : null;
            
            // Nếu không có parentRect, dùng canvasRect
            if (parentRect == null)
            {
                parentRect = canvas.transform as RectTransform;
            }
            
            // Convert screen position to local position trong parent
            Vector2 localPoint;
            
            // Screen Space - Overlay không có camera, Screen Space - Camera có camera
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint))
            {
                // Set position của item theo chuột (cả X và Y)
                rectTransform.anchoredPosition = localPoint;
                
                // Debug: Kiểm tra nếu chỉ có X được update
                if (Mathf.Abs(rectTransform.anchoredPosition.y - localPoint.y) > 0.01f)
                {
                    Debug.LogWarning($"[ItemDragHandler] Y position mismatch! Expected: {localPoint.y}, Actual: {rectTransform.anchoredPosition.y}");
                    // Force set lại Y
                    rectTransform.anchoredPosition = new Vector2(localPoint.x, localPoint.y);
                }
            }
            else
            {
                // Fallback: dùng world position nếu ScreenPointToLocalPointInRectangle fail
                Vector3 worldPos;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parentRect,
                    eventData.position,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out worldPos))
                {
                    rectTransform.position = worldPos;
                }
                else
                {
                    // Final fallback: sử dụng world position trực tiếp
                    transform.position = eventData.position;
                }
            }
        }
        else
        {
            // Fallback: sử dụng world position
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore Animator nếu có
        if (animator != null)
        {
            animator.enabled = originalAnimatorEnabled;
        }
        
        // Restore AutoFitToSlot nếu có
        if (autoFitToSlot != null)
        {
            autoFitToSlot.enabled = originalAutoFitEnabled;
        }
        
        // Restore canvas group
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true; //Enables raycasts
            canvasGroup.alpha = 1f; //No longer transparent
        }

        // Tìm slot drop target
        Slot dropSlot = null;
        
        // Thử lấy từ pointerEnter trước
        if (eventData.pointerEnter != null)
        {
            dropSlot = eventData.pointerEnter.GetComponent<Slot>();
            if (dropSlot == null)
            {
                dropSlot = eventData.pointerEnter.GetComponentInParent<Slot>();
            }
        }
        
        // Nếu không tìm thấy, thử lấy từ pointerCurrentRaycast
        if (dropSlot == null && eventData.pointerCurrentRaycast.gameObject != null)
        {
            dropSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<Slot>();
            if (dropSlot == null)
            {
                dropSlot = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Slot>();
            }
        }
        
        Slot originalSlot = originalParent != null ? originalParent.GetComponent<Slot>() : null;

        bool itemMoved = false;

        if (dropSlot != null && originalSlot != null)
        {
            // Kiểm tra nếu slot thuộc về DungeonDoorDiamondPanel
            DungeonDoorDiamondPanel diamondPanel = dropSlot.GetComponentInParent<DungeonDoorDiamondPanel>();
            if (diamondPanel != null)
            {
                // Kiểm tra xem item có phải là kim cương không
                Item item = GetComponent<Item>();
                if (item == null || !diamondPanel.IsValidDiamond(gameObject))
                {
                    // Không phải kim cương, không cho phép kéo vào
                    ReturnToOriginalSlot();
                    return;
                }
            }

            //Is a slot under drop point
            if (dropSlot.currentItem != null)
            {
                //Slot has an item - swap items
                GameObject otherItem = dropSlot.currentItem;
                otherItem.transform.SetParent(originalSlot.transform, false);
                
                // Setup lại RectTransform cho item được swap
                RectTransform otherRect = otherItem.GetComponent<RectTransform>();
                if (otherRect != null)
                {
                    otherRect.anchoredPosition = Vector2.zero;
                    otherRect.anchorMin = Vector2.zero;
                    otherRect.anchorMax = Vector2.one;
                    otherRect.pivot = new Vector2(0.5f, 0.5f);
                    otherRect.sizeDelta = Vector2.zero;
                }
                
                // Apply AutoFitToSlot cho item được swap
                AutoFitToSlot otherAutoFit = otherItem.GetComponent<AutoFitToSlot>();
                if (otherAutoFit != null)
                {
                    otherAutoFit.Apply();
                }
                else
                {
                    // Nếu không có, setup thủ công
                    if (otherRect != null)
                    {
                        otherRect.offsetMin = new Vector2(4f, 4f);
                        otherRect.offsetMax = new Vector2(-4f, -4f);
                    }
                }
                
                Canvas.ForceUpdateCanvases();
                
                // Đảm bảo Image được enable cho item được swap
                Image otherImage = otherItem.GetComponent<Image>();
                SpriteRenderer otherSpriteRenderer = otherItem.GetComponent<SpriteRenderer>();
                if (otherImage != null && otherSpriteRenderer != null)
                {
                    if (otherImage.sprite == null && otherSpriteRenderer.sprite != null)
                    {
                        otherImage.sprite = otherSpriteRenderer.sprite;
                    }
                    otherImage.enabled = otherImage.sprite != null;
                    otherSpriteRenderer.enabled = false;
                }
                
                originalSlot.currentItem = otherItem;
            }
            else
            {
                originalSlot.currentItem = null;
            }

            //Move item into drop slot
            transform.SetParent(dropSlot.transform, false);
            
            // Restore tất cả RectTransform properties để item có kích thước đúng trong slot
            if (rectTransform != null)
            {
                // Lấy lossyScale của drop slot để tính scale factor
                Vector3 dropSlotLossyScale = dropSlot.transform.lossyScale;
                
                // Tính local size mới dựa trên world size và drop slot lossyScale
                Vector2 newLocalSize = new Vector2(
                    dropSlotLossyScale.x != 0 ? originalWorldSize.x / dropSlotLossyScale.x : originalSizeDelta.x,
                    dropSlotLossyScale.y != 0 ? originalWorldSize.y / dropSlotLossyScale.y : originalSizeDelta.y
                );
                
                // Restore scale và sizeDelta
                transform.localScale = originalScale;
                rectTransform.sizeDelta = newLocalSize;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.anchorMin = originalAnchorMin;
                rectTransform.anchorMax = originalAnchorMax;
                rectTransform.pivot = originalPivot;
            }
            
            // QUAN TRỌNG: Apply AutoFitToSlot sau khi drop vào slot mới
            AutoFitToSlot autoFit = GetComponent<AutoFitToSlot>();
            if (autoFit != null)
            {
                autoFit.Apply();
            }
            else
            {
                // Nếu không có AutoFitToSlot, setup thủ công
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.offsetMin = new Vector2(4f, 4f);
                    rectTransform.offsetMax = new Vector2(-4f, -4f);
                }
            }
            
            // QUAN TRỌNG: Đảm bảo Image được enable và SpriteRenderer được disable sau khi drop
            // (vì có thể item đã bị thay đổi trong quá trình drag)
            Image itemImage = GetComponent<Image>();
            SpriteRenderer itemSpriteRenderer = GetComponent<SpriteRenderer>();
            
            if (itemImage != null && itemSpriteRenderer != null)
            {
                // Trong UI Canvas, luôn dùng Image
                if (itemImage.sprite == null && itemSpriteRenderer.sprite != null)
                {
                    itemImage.sprite = itemSpriteRenderer.sprite;
                }
                itemImage.enabled = itemImage.sprite != null;
                itemSpriteRenderer.enabled = false;
                
                // Đảm bảo Image color alpha = 1
                Color imgColor = itemImage.color;
                imgColor.a = 1f;
                itemImage.color = imgColor;
            }
            
            // Đảm bảo CanvasGroup alpha = 1
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            
            // Force update Canvas
            Canvas.ForceUpdateCanvases();
            
            dropSlot.currentItem = gameObject;
            itemMoved = true;
        }
        else
        {
            //No slot under drop point - return to original slot
            ReturnToOriginalSlot();
        }

        // Thông báo cho diamond panel nếu item được kéo vào
        if (itemMoved && dropSlot != null)
        {
            DungeonDoorDiamondPanel diamondPanel = dropSlot.GetComponentInParent<DungeonDoorDiamondPanel>();
            if (diamondPanel != null)
            {
                diamondPanel.OnItemDroppedInSlot(dropSlot);
            }
        }

        // Nếu item là vũ khí và đã được di chuyển, cập nhật InventorySlot và refresh ActiveInventory
        if (itemMoved)
        {
            Item item = GetComponent<Item>();
            if (item != null && item.IsWeapon)
            {
                // Cập nhật InventorySlot ở slot mới
                InventorySlot inventorySlot = dropSlot.GetComponent<InventorySlot>();
                if (inventorySlot == null)
                {
                    inventorySlot = dropSlot.gameObject.AddComponent<InventorySlot>();
                }
                inventorySlot.SetWeapon(item.weaponInfo);

                // Xóa InventorySlot ở slot cũ (nếu có)
                if (originalSlot != null)
                {
                    InventorySlot oldInventorySlot = originalSlot.GetComponent<InventorySlot>();
                    if (oldInventorySlot != null)
                    {
                        oldInventorySlot.SetWeapon(null);
                    }
                }

                // Refresh ActiveInventory để cập nhật vũ khí đang equip
                if (ActiveInventory.Instance != null)
                {
                    ActiveInventory.Instance.RefreshActiveWeapon();
                }
            }
        }
    }

    /// <summary>
    /// Return item to original slot
    /// </summary>
    private void ReturnToOriginalSlot()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            
            // Restore tất cả RectTransform properties để item có kích thước đúng trong slot
            if (rectTransform != null)
            {
                // Lấy lossyScale của original parent để tính scale factor
                Vector3 originalParentLossyScale = originalParent.lossyScale;
                
                // Tính local size mới dựa trên world size và original parent lossyScale
                Vector2 restoreLocalSize = new Vector2(
                    originalParentLossyScale.x != 0 ? originalWorldSize.x / originalParentLossyScale.x : originalSizeDelta.x,
                    originalParentLossyScale.y != 0 ? originalWorldSize.y / originalParentLossyScale.y : originalSizeDelta.y
                );
                
                transform.localScale = originalScale;
                rectTransform.sizeDelta = restoreLocalSize;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.anchorMin = originalAnchorMin;
                rectTransform.anchorMax = originalAnchorMax;
                rectTransform.pivot = originalPivot;
            }
            
            // QUAN TRỌNG: Apply AutoFitToSlot sau khi return về slot cũ
            AutoFitToSlot autoFit = GetComponent<AutoFitToSlot>();
            if (autoFit != null)
            {
                autoFit.Apply();
            }
            else
            {
                // Nếu không có AutoFitToSlot, setup thủ công
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.offsetMin = new Vector2(4f, 4f);
                    rectTransform.offsetMax = new Vector2(-4f, -4f);
                }
            }
            
            // QUAN TRỌNG: Đảm bảo Image được enable và SpriteRenderer được disable
            Image itemImage = GetComponent<Image>();
            SpriteRenderer itemSpriteRenderer = GetComponent<SpriteRenderer>();
            
            if (itemImage != null && itemSpriteRenderer != null)
            {
                // Trong UI Canvas, luôn dùng Image
                if (itemImage.sprite == null && itemSpriteRenderer.sprite != null)
                {
                    itemImage.sprite = itemSpriteRenderer.sprite;
                }
                itemImage.enabled = itemImage.sprite != null;
                itemSpriteRenderer.enabled = false;
                
                // Đảm bảo Image color alpha = 1
                Color imgColor = itemImage.color;
                imgColor.a = 1f;
                itemImage.color = imgColor;
            }
            
            // Đảm bảo CanvasGroup alpha = 1
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            
            // Force update Canvas
            Canvas.ForceUpdateCanvases();
            
            // Restore original sibling index if needed
            if (originalSiblingIndex >= 0 && originalSiblingIndex < originalParent.childCount)
            {
                transform.SetSiblingIndex(originalSiblingIndex);
            }
        }
    }
}
