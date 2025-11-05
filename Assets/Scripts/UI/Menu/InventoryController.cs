using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [Header("Inventory UI")]
    public GameObject inventoryPanel; // grid parent for inventory slots
    public GameObject slotPrefab;     // slot prefab (must have Slot)
    public int slotCount = 16;

    [Header("Item Prefabs (UI)")]
    public GameObject[] itemPrefabs;  // each must have UIItem with itemData.id filled

    Dictionary<string, GameObject> idToPrefab = new Dictionary<string, GameObject>();

    void Awake()
    {
        idToPrefab.Clear();
        foreach (var go in itemPrefabs)
        {
            if (!go) continue;
            var ui = go.GetComponent<UIItem>();
            if (ui && ui.itemData && !string.IsNullOrEmpty(ui.itemData.id))
            {
                idToPrefab[ui.itemData.id] = go;
            }
        }
    }

    void Start()
    {
        // Build empty slots
        List<Slot> slots = new List<Slot>();
        for (int i = 0; i < slotCount; i++)
        {
            Slot slot = Instantiate(slotPrefab, inventoryPanel.transform).GetComponent<Slot>();
            slots.Add(slot);
        }

        // Try load; if not found, do initial fill from itemPrefabs (first N)
        if (!InventoryPersistence.TryLoadInventory(inventoryPanel.transform, idToPrefab))
        {
            for (int i = 0; i < Mathf.Min(itemPrefabs.Length, slotCount); i++)
            {
                var go = Instantiate(itemPrefabs[i], slots[i].transform);
                go.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
                slots[i].currentItem = go;
            }
        }

        // Also load hotbar data
        InventoryPersistence.TryLoadHotbar(idToPrefab);
    }
}
