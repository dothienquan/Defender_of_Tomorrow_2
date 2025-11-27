using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ItemDictionary : MonoBehaviour
{
    [Tooltip("Kéo các Item prefab vào đây. Prefab phải có component Item.")]
    public List<GameObject> itemPrefabs;
    private Dictionary<int, GameObject> itemDictionary;

    private void Awake()
    {
        itemDictionary = new Dictionary<int, GameObject>();

        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            if (itemPrefabs[i] != null)
            {
                // Lấy component Item từ prefab
                Item item = itemPrefabs[i].GetComponent<Item>();
                if (item != null)
                {
                    // Set ID tự động
                    item.ID = i + 1;
                    itemDictionary[item.ID] = itemPrefabs[i];
                }
                else
                {
                    Debug.LogWarning($"[ItemDictionary] Prefab at index {i} ({itemPrefabs[i].name}) does not have Item component!");
                }
            }
        }

        Debug.Log($"[ItemDictionary] Initialized with {itemDictionary.Count} items.");
    }

    public GameObject GetItemPrefab(int itemID)
    {
        if (itemDictionary.TryGetValue(itemID, out GameObject prefab))
        {
            return prefab;
        }
        
        Debug.LogWarning($"[ItemDictionary] Item with ID {itemID} not found in dictionary");
        return null;
    }

    /// <summary>
    /// Validate items trong Editor
    /// </summary>
    [ContextMenu("Validate Items")]
    private void ValidateItems()
    {
        int validCount = 0;
        int invalidCount = 0;

        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            if (itemPrefabs[i] == null)
            {
                Debug.LogWarning($"[ItemDictionary] Item at index {i} is null!");
                invalidCount++;
                continue;
            }

            Item item = itemPrefabs[i].GetComponent<Item>();
            if (item == null)
            {
                Debug.LogWarning($"[ItemDictionary] Prefab '{itemPrefabs[i].name}' at index {i} does not have Item component!");
                invalidCount++;
            }
            else
            {
                validCount++;
            }
        }

        Debug.Log($"[ItemDictionary] Validation complete: {validCount} valid, {invalidCount} invalid items.");
    }
}
