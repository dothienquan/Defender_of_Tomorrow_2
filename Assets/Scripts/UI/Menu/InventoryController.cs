using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [Header("Inventory UI")]
    public GameObject inventoryPanel; // Grid parent chứa sẵn 18 Slot
    public GameObject slotPrefab;     // Không còn dùng, nhưng giữ để tránh lỗi inspector
    public int slotCount = 18;        // Chỉ để reference

    [Header("Item Prefabs (UI)")]
    public GameObject[] itemPrefabs;  // Mỗi prefab có UIItem với itemData.id

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
        // Dùng slot có sẵn trong hierarchy
        List<Slot> slots = new List<Slot>(inventoryPanel.GetComponentsInChildren<Slot>(true));
        if (slots.Count == 0)
        {
            Debug.LogWarning("No slots found in inventoryPanel! Please add Slot components manually.");
            return;
        }

        // Thử load từ save; nếu không có, thì auto fill (tùy chọn)
        if (!InventoryPersistence.TryLoadInventory(inventoryPanel.transform, idToPrefab))
        {
            for (int i = 0; i < Mathf.Min(itemPrefabs.Length, slots.Count); i++)
            {
                var go = Instantiate(itemPrefabs[i], slots[i].transform);
                go.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
                slots[i].currentItem = go;
            }
        }

        // Load hotbar
        InventoryPersistence.TryLoadHotbar(idToPrefab);
    }
}
