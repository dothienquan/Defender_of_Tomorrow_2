using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent; // slot transform
        transform.SetParent(transform.root); // above other canvas
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        Slot dropSlot = null;
        if (eventData.pointerEnter)
        {
            // tìm slot chính xác bằng GetComponentInParent, nhưng loại trừ bản thân item đang kéo
            var potentialSlot = eventData.pointerEnter.GetComponentInParent<Slot>();
            if (potentialSlot != null && potentialSlot.transform != transform)
                dropSlot = potentialSlot;
        }

        Slot originalSlot = originalParent ? originalParent.GetComponent<Slot>() : null;
        var thisRect = GetComponent<RectTransform>();
        var uiItem = GetComponent<UIItem>();

        if (dropSlot != null && dropSlot != originalSlot)
        {
            // --- SWAP LOGIC ---
            if (dropSlot.currentItem != null)
            {
                // swap UI objects
                var otherItem = dropSlot.currentItem;
                dropSlot.currentItem = gameObject;
                otherItem.transform.SetParent(originalSlot.transform, false);
                originalSlot.currentItem = otherItem;
                otherItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                // Refresh weapon adapter nếu hotbar
                if (originalSlot.isHotbarSlot)
                {
                    var invSlot = originalSlot.GetComponentInChildren<InventorySlot>();
                    var swappedUI = originalSlot.GetUIItem();
                    invSlot?.SetWeapon(swappedUI ? swappedUI.AsWeapon() : null);
                }
            }
            else
            {
                if (originalSlot) originalSlot.currentItem = null;
                if (originalSlot && originalSlot.isHotbarSlot)
                {
                    var invSlot = originalSlot.GetComponentInChildren<InventorySlot>();
                    invSlot?.SetWeapon(null);
                }
            }

            // đặt item vào slot mới
            transform.SetParent(dropSlot.transform, false);
            dropSlot.currentItem = gameObject;
            thisRect.anchoredPosition = Vector2.zero;

            if (dropSlot.isHotbarSlot)
            {
                var invSlot = dropSlot.GetComponentInChildren<InventorySlot>();
                invSlot?.SetWeapon(uiItem ? uiItem.AsWeapon() : null);
                ActiveInventory.Instance.RefreshActiveWeapon();
            }
        }
        else
        {
            // trả về chỗ cũ
            transform.SetParent(originalParent, false);
            thisRect.anchoredPosition = Vector2.zero;
        }
    }
}
