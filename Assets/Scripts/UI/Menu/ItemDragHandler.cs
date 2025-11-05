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

        Slot dropSlot = eventData.pointerEnter ? eventData.pointerEnter.GetComponent<Slot>() : null;
        if (dropSlot == null && eventData.pointerEnter)
        {
            var maybeItem = eventData.pointerEnter.GetComponentInParent<Slot>();
            if (maybeItem) dropSlot = maybeItem;
        }

        Slot originalSlot = originalParent ? originalParent.GetComponent<Slot>() : null;
        var thisRect = GetComponent<RectTransform>();
        var uiItem = GetComponent<UIItem>();

        if (dropSlot != null)
        {
            // Swap handling
            if (dropSlot.currentItem != null)
            {
                // swap UI objects
                dropSlot.currentItem.transform.SetParent(originalSlot.transform);
                originalSlot.currentItem = dropSlot.currentItem;
                originalSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                // If original is hotbar, refresh its weapon adapter from swapped item
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
                // If moving out of a hotbar slot, clear its weapon
                if (originalSlot && originalSlot.isHotbarSlot)
                {
                    var invSlot = originalSlot.GetComponentInChildren<InventorySlot>();
                    invSlot?.SetWeapon(null);
                }
            }

            // place into drop slot
            transform.SetParent(dropSlot.transform);
            dropSlot.currentItem = gameObject;
            thisRect.anchoredPosition = Vector2.zero;

            // If dropped into a hotbar slot, set its weapon and refresh equip
            if (dropSlot.isHotbarSlot)
            {
                var invSlot = dropSlot.GetComponentInChildren<InventorySlot>();
                invSlot?.SetWeapon(uiItem ? uiItem.AsWeapon() : null);
                ActiveInventory.Instance.RefreshActiveWeapon(); // re-evaluate if needed
            }
        }
        else
        {
            // No slot -> return to origin
            transform.SetParent(originalParent);
            thisRect.anchoredPosition = Vector2.zero;
        }
    }
}
