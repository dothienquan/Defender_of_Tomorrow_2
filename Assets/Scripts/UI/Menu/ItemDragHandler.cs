using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent; //Save OG parent
        transform.SetParent(transform.root); //Above other canvas'
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f; //Semi-transparent during drag
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position; //Follow the mouse
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true; //Enables raycasts
        canvasGroup.alpha = 1f; //No longer transparent

        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>(); //Slot where item dropped
        if (dropSlot == null)
        {
            GameObject dropItem = eventData.pointerEnter;
            if (dropItem != null)
            {
                dropSlot = dropItem.GetComponentInParent<Slot>();
            }
        }
        Slot originalSlot = originalParent.GetComponent<Slot>();

        bool itemMoved = false;

        if (dropSlot != null)
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
                    transform.SetParent(originalParent);
                    GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    return;
                }
            }

            //Is a slot under drop point
            if (dropSlot.currentItem != null)
            {
                //Slot has an item - swap items
                dropSlot.currentItem.transform.SetParent(originalSlot.transform);
                originalSlot.currentItem = dropSlot.currentItem;
                dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }
            else
            {
                originalSlot.currentItem = null;
            }

            //Move item into drop slot
            transform.SetParent(dropSlot.transform);
            dropSlot.currentItem = gameObject;
            itemMoved = true;
        }
        else
        {
            //No slot under drop point
            transform.SetParent(originalParent);
        }

        GetComponent<RectTransform>().anchoredPosition = Vector2.zero; //Center

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
}
