using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySaveData
{
    public string[] inventoryIds; // per inventory slot index
    public string[] hotbarIds;    // per hotbar index
    public int activeHotbarIndex;
}

public static class InventoryPersistence
{
    const string KEY = "INV_SAVE_V1";

    // --- SAVE ---
    public static void Save(Transform inventoryPanel)
    {
        var data = new InventorySaveData();

        // Inventory grid
        var invSlots = inventoryPanel.GetComponentsInChildren<Slot>(true);
        data.inventoryIds = new string[invSlots.Length];
        for (int i = 0; i < invSlots.Length; i++)
        {
            var ui = invSlots[i].GetUIItem();
            data.inventoryIds[i] = ui && ui.itemData ? ui.itemData.id : string.Empty;
        }

        // Hotbar
        var hotbar = ActiveInventory.Instance.transform;
        int hotCount = hotbar.childCount;
        data.hotbarIds = new string[hotCount];
        for (int i = 0; i < hotCount; i++)
        {
            var invSlot = hotbar.GetChild(i).GetComponentInChildren<InventorySlot>();
            var w = invSlot ? invSlot.GetWeaponInfo() : null;
            data.hotbarIds[i] = w ? w.id : string.Empty;
        }

        data.activeHotbarIndex = GetPrivateActiveIndexSafe();

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    // --- LOAD ---
    public static bool TryLoadInventory(Transform inventoryPanel, Dictionary<string, GameObject> idToPrefab)
    {
        if (!PlayerPrefs.HasKey(KEY)) return false;
        var json = PlayerPrefs.GetString(KEY);
        var data = JsonUtility.FromJson<InventorySaveData>(json);
        if (data == null || data.inventoryIds == null) return false;

        var invSlots = inventoryPanel.GetComponentsInChildren<Slot>(true);
        int count = Mathf.Min(invSlots.Length, data.inventoryIds.Length);

        for (int i = 0; i < count; i++)
        {
            // clear existing
            if (invSlots[i].currentItem)
            {
                GameObject.Destroy(invSlots[i].currentItem);
                invSlots[i].currentItem = null;
            }

            var id = data.inventoryIds[i];
            if (!string.IsNullOrEmpty(id) && idToPrefab.TryGetValue(id, out var prefab))
            {
                var go = GameObject.Instantiate(prefab, invSlots[i].transform);
                go.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
                invSlots[i].currentItem = go;
            }
        }

        return true;
    }

    public static bool TryLoadHotbar(Dictionary<string, GameObject> idToPrefab)
    {
        if (!PlayerPrefs.HasKey(KEY)) return false;
        var json = PlayerPrefs.GetString(KEY);
        var data = JsonUtility.FromJson<InventorySaveData>(json);
        if (data == null || data.hotbarIds == null) return false;

        Transform hotbar = ActiveInventory.Instance.transform;
        int count = Mathf.Min(hotbar.childCount, data.hotbarIds.Length);

        for (int i = 0; i < count; i++)
        {
            var slot = hotbar.GetChild(i).GetComponent<Slot>();
            var invSlot = hotbar.GetChild(i).GetComponentInChildren<InventorySlot>();

            // clear UI item
            if (slot && slot.currentItem)
            {
                GameObject.Destroy(slot.currentItem);
                slot.currentItem = null;
            }

            var id = data.hotbarIds[i];
            if (!string.IsNullOrEmpty(id) && idToPrefab.TryGetValue(id, out var prefab))
            {
                // instantiate UI item to hotbar slot
                var go = GameObject.Instantiate(prefab, slot.transform);
                go.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
                slot.currentItem = go;

                // assign weapon to adapter
                var ui = go.GetComponent<UIItem>();
                invSlot?.SetWeapon(ui ? ui.AsWeapon() : null);
            }
            else
            {
                invSlot?.SetWeapon(null);
            }
        }

        // restore active highlight
        int active = Mathf.Clamp(data.activeHotbarIndex, 0, hotbar.childCount - 1);
        // mimic ToggleActiveHighlight without key event
        for (int i = 0; i < hotbar.childCount; i++)
        {
            var t = hotbar.GetChild(i);
            if (t.childCount > 0)
            {
                // assumes GetChild(0) is your highlight object like original code
                t.GetChild(0).gameObject.SetActive(i == active);
            }
        }
        ActiveInventory.Instance.RefreshActiveWeapon();
        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KEY);
    }

    private static int GetPrivateActiveIndexSafe()
    {
        // We can't read private activeSlotIndexNum; store by scanning highlight state.
        Transform hotbar = ActiveInventory.Instance.transform;
        for (int i = 0; i < hotbar.childCount; i++)
        {
            var t = hotbar.GetChild(i);
            if (t.childCount > 0 && t.GetChild(0).gameObject.activeSelf) return i;
        }
        return 0;
    }
}
