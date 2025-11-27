using System.Collections.Generic;
using UnityEngine;
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


    void Update()
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (Keyboard.current[hotbarKeys[i]].wasPressedThisFrame)
            {
                //Use item
                UseItemInSlot(i);
            }
        }
    }
                                                                
    void UseItemInSlot(int index)
    {
        if (index < 0 || index >= hotbarPanel.transform.childCount) return;

        Slot slot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
        if (slot.currentItem != null)
        {
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

        // Cập nhật ActiveInventory để equip vũ khí này
        if (ActiveInventory.Instance != null)
        {
            // Set active slot index trong ActiveInventory
            int slotIndex = slot.transform.GetSiblingIndex();
            ActiveInventory.Instance.SetActiveSlot(slotIndex);
        }
    }

    public List<InventorySaveData> GetHotbarItems()
    {
        List<InventorySaveData> hotbarData = new List<InventorySaveData>();
        foreach (Transform slotTransform in hotbarPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                hotbarData.Add(new InventorySaveData { itemID = item.ID, slotIndex = slotTransform.GetSiblingIndex() });
            }
        }
        return hotbarData;
    }

    public void SetHotbarItems(List<InventorySaveData> inventorySaveData)
    {
        foreach (Transform child in hotbarPanel.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(slotPrefab, hotbarPanel.transform);
        }

        foreach (InventorySaveData data in inventorySaveData)
        {
            if (data.slotIndex < slotCount)
            {
                Slot slot = hotbarPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
                    GameObject item = Instantiate(itemPrefab, slot.transform);
                    item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    slot.currentItem = item;
                }
            }

        }
    }
}
