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
        
        // QUAN TRỌNG: Kiểm tra xem item có đang ở đúng slot không
        Slot currentSlot = originalParent != null ? originalParent.GetComponent<Slot>() : null;
        if (currentSlot == null)
        {
            Debug.LogWarning($"[ItemDragHandler] Item '{gameObject.name}' is not in a slot! Parent: {originalParent?.name}. This may cause dragging issues.");
        }
        
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
            
            // QUAN TRỌNG: Tính kích thước visual thực tế từ sprite size, không phải từ rect.size
            // Vì với stretch anchors (0-1), rect.size sẽ là kích thước của slot, không phải item
            Vector2 spriteSize = Vector2.zero;
            
            // Thử lấy từ Image component trước
            Image image = GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                spriteSize = image.sprite.rect.size;
            }
            else
            {
                // Nếu không có Image, thử từ SpriteRenderer
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    spriteSize = spriteRenderer.sprite.rect.size;
                }
            }
            
            // Nếu có sprite size, sử dụng nó nhân với lossyScale
            if (spriteSize.x > 0 && spriteSize.y > 0)
            {
                Vector3 currentLossyScale = rectTransform.lossyScale;
                originalWorldSize = new Vector2(
                    spriteSize.x * currentLossyScale.x * originalScale.x,
                    spriteSize.y * currentLossyScale.y * originalScale.y
                );
                Debug.Log($"[ItemDragHandler] Using sprite size for '{gameObject.name}': sprite={spriteSize}, world={originalWorldSize}, scale={currentLossyScale}");
            }
            else
            {
                // Fallback: sử dụng rect.size (chỉ khi anchors không phải stretch)
                Vector2 localRectSize = rectTransform.rect.size;
                Vector3 currentLossyScale = rectTransform.lossyScale;
                
                // Nếu anchors là stretch (0-1), rect.size sẽ là slot size, không đúng
                // Trong trường hợp này, sử dụng sizeDelta nếu có
                if (rectTransform.anchorMin == Vector2.zero && rectTransform.anchorMax == Vector2.one)
                {
                    // Stretch anchors - rect.size không đáng tin cậy
                    // Sử dụng sizeDelta nếu có, nếu không thì dùng default
                    if (originalSizeDelta.x > 0 && originalSizeDelta.y > 0)
                    {
                        originalWorldSize = new Vector2(
                            originalSizeDelta.x * currentLossyScale.x * originalScale.x,
                            originalSizeDelta.y * currentLossyScale.y * originalScale.y
                        );
                        Debug.Log($"[ItemDragHandler] Using sizeDelta for '{gameObject.name}' (stretch anchors): sizeDelta={originalSizeDelta}, world={originalWorldSize}");
                    }
                    else
                    {
                        // Không có sizeDelta hợp lệ, dùng rect size nhưng log warning
                        originalWorldSize = new Vector2(
                            localRectSize.x * currentLossyScale.x,
                            localRectSize.y * currentLossyScale.y
                        );
                        Debug.LogWarning($"[ItemDragHandler] Item '{gameObject.name}' has stretch anchors but no valid sprite size or sizeDelta. Using rect.size (may be incorrect): {originalWorldSize}");
                    }
                }
                else
                {
                    // Không phải stretch anchors - rect.size đáng tin cậy
                    originalWorldSize = new Vector2(
                        localRectSize.x * currentLossyScale.x,
                        localRectSize.y * currentLossyScale.y
                    );
                    Debug.Log($"[ItemDragHandler] Using rect.size for '{gameObject.name}': rect={localRectSize}, world={originalWorldSize}");
                }
            }
            
            // QUAN TRỌNG: Kiểm tra anchors
            // Với stretch anchors (0-1), anchors không bằng nhau là bình thường và không phải vấn đề
            // Chỉ cảnh báo nếu anchors không phải stretch và không bằng nhau (có thể gây constraints)
            bool isStretchAnchors = (rectTransform.anchorMin == Vector2.zero && rectTransform.anchorMax == Vector2.one);
            if (!isStretchAnchors && rectTransform.anchorMin != rectTransform.anchorMax)
            {
                // Anchors không bằng nhau và không phải stretch - có thể gây ra constraints
                Debug.LogWarning($"[ItemDragHandler] Item '{gameObject.name}' has non-matching anchors before drag (min: {rectTransform.anchorMin}, max: {rectTransform.anchorMax}). This may restrict movement.");
            }
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
        
        // QUAN TRỌNG: Lấy kích thước visual thực tế TRƯỚC KHI chuyển parent
        // Với stretch anchors, rect.size = slot size - offsets, đó chính là kích thước visual
        Vector2 visualSizeBeforeParentChange = Vector2.zero;
        Vector3 lossyScaleBeforeParentChange = Vector3.one;
        if (rectTransform != null)
        {
            visualSizeBeforeParentChange = rectTransform.rect.size;
            lossyScaleBeforeParentChange = rectTransform.lossyScale;
            Debug.Log($"[ItemDragHandler] Item '{gameObject.name}' before parent change - visual: {visualSizeBeforeParentChange}, lossyScale: {lossyScaleBeforeParentChange}");
        }
        
        // Set parent
        transform.SetParent(targetParent, false);
        
        // QUAN TRỌNG: Giữ nguyên kích thước visual khi drag (không zoom)
        // Visual size đã là kích thước trên màn hình, dùng trực tiếp
        if (rectTransform != null)
        {
            // Lấy lossyScale mới sau khi chuyển parent
            Vector3 newLossyScale = rectTransform.lossyScale;
            
            // QUAN TRỌNG: Visual size (rect.size) đã là kích thước trên màn hình
            // Với center anchors (0.5, 0.5), sizeDelta = visual size / lossyScale
            // DragContainer có scale = 1, nhưng vẫn bị ảnh hưởng bởi CanvasScaler
            // Nên lossyScale có thể khác 1, cần chia để có sizeDelta đúng
            Vector2 newSizeDelta = new Vector2(
                newLossyScale.x != 0 ? visualSizeBeforeParentChange.x / newLossyScale.x : visualSizeBeforeParentChange.x,
                newLossyScale.y != 0 ? visualSizeBeforeParentChange.y / newLossyScale.y : visualSizeBeforeParentChange.y
            );
            
            // Nếu visual size hợp lệ, sử dụng nó
            if (visualSizeBeforeParentChange.x > 0 && visualSizeBeforeParentChange.y > 0)
            {
                rectTransform.sizeDelta = newSizeDelta;
                Debug.Log($"[ItemDragHandler] Item '{gameObject.name}' drag size - visual: {visualSizeBeforeParentChange}, lossyScale: {lossyScaleBeforeParentChange} -> {newLossyScale}, sizeDelta: {newSizeDelta}");
            }
            else
            {
                // Fallback: sử dụng originalSizeDelta nếu có
                if (originalSizeDelta.x > 0 && originalSizeDelta.y > 0)
                {
                    rectTransform.sizeDelta = originalSizeDelta;
                    Debug.Log($"[ItemDragHandler] Item '{gameObject.name}' using originalSizeDelta: {originalSizeDelta}");
                }
                else
                {
                    // Nếu không có gì, dùng sprite size nhưng scale down
                    Image image = GetComponent<Image>();
                    SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                    Sprite sprite = image?.sprite ?? spriteRenderer?.sprite;
                    
                    if (sprite != null)
                    {
                        Vector2 spriteSize = sprite.rect.size;
                        // Scale down sprite size để phù hợp với inventory slot (thường ~64-100 pixels)
                        float maxSlotSize = 100f; // Giả định slot size tối đa
                        float scaleFactor = Mathf.Min(maxSlotSize / spriteSize.x, maxSlotSize / spriteSize.y, 1f);
                        rectTransform.sizeDelta = spriteSize * scaleFactor;
                        Debug.LogWarning($"[ItemDragHandler] Item '{gameObject.name}' has no visual size. Using scaled sprite size: {rectTransform.sizeDelta}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemDragHandler] Item '{gameObject.name}' has no visual size, sizeDelta, or sprite. Using default 64x64.");
                        rectTransform.sizeDelta = new Vector2(64, 64);
                    }
                }
            }
            
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
            
            // QUAN TRỌNG: Đảm bảo RectTransform không có constraints ngăn cản movement
            // Unity không có API để set constraints trực tiếp, nhưng đảm bảo anchors cho phép free movement
            if (rectTransform.anchorMin != rectTransform.anchorMax)
            {
                // Nếu anchors không bằng nhau, có thể bị lock. Set anchors về center để free movement
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
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
                // QUAN TRỌNG: Set position của item theo chuột (cả X và Y) - không bị giới hạn
                // Sử dụng SetInsetAndSizeFromParentEdge để đảm bảo không bị constraints
                rectTransform.anchoredPosition = localPoint;
                
                // Force update để đảm bảo position được apply
                Canvas.ForceUpdateCanvases();
                
                // Debug: Kiểm tra nếu chỉ có X được update (có thể do constraints)
                if (Mathf.Abs(rectTransform.anchoredPosition.y - localPoint.y) > 0.01f)
                {
                    Debug.LogWarning($"[ItemDragHandler] Y position mismatch detected! Expected: {localPoint.y}, Actual: {rectTransform.anchoredPosition.y}. Forcing update...");
                    // Force set lại cả X và Y bằng cách set position trực tiếp
                    rectTransform.anchoredPosition = localPoint;
                    Canvas.ForceUpdateCanvases();
                    
                    // Nếu vẫn không được, thử dùng world position
                    if (Mathf.Abs(rectTransform.anchoredPosition.y - localPoint.y) > 0.01f)
                    {
                        Vector3 worldPos;
                        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                            parentRect,
                            eventData.position,
                            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                            out worldPos))
                        {
                            rectTransform.position = worldPos;
                            Canvas.ForceUpdateCanvases();
                        }
                    }
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
                    Canvas.ForceUpdateCanvases();
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
            // Kiểm tra slot có phải là DiamondSlot không
            DiamondSlot diamondSlot = dropSlot.GetComponent<DiamondSlot>();
            if (diamondSlot != null)
            {
                DungeonDoorDiamondPanel diamondPanel = dropSlot.GetComponentInParent<DungeonDoorDiamondPanel>();
                if (diamondPanel != null)
                {
                    Debug.Log($"[ItemDragHandler] Item dropped into DiamondSlot. Notifying DungeonDoorDiamondPanel...");
                    diamondPanel.OnItemDroppedInSlot(dropSlot);
                }
                else
                {
                    Debug.LogWarning("[ItemDragHandler] DiamondSlot found but DungeonDoorDiamondPanel not found in parent!");
                }
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
