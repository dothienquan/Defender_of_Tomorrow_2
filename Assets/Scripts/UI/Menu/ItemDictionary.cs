using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class ItemDictionary : MonoBehaviour
{
    public static ItemDictionary Instance { get; private set; }

    [Tooltip("Kéo các Item prefab vào đây. Prefab phải có component Item.")]
    public List<GameObject> itemPrefabs;

    private Dictionary<int, GameObject> itemDictionary = new Dictionary<int, GameObject>();

    private void Awake()
    {
        // Singleton + DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ItemDictionary] Duplicate instance found, destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        itemDictionary.Clear();

        int nextAutoId = 1;
        int validCount = 0;
        int invalidCount = 0;

        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            GameObject prefab = itemPrefabs[i];
            if (prefab == null)
            {
                Debug.LogWarning($"[ItemDictionary] Item at index {i} is null!");
                invalidCount++;
                continue;
            }

            Item item = prefab.GetComponent<Item>();
            if (item == null)
            {
                Debug.LogWarning($"[ItemDictionary] Prefab '{prefab.name}' at index {i} does not have Item component!");
                invalidCount++;
                continue;
            }

            // Nếu bạn đã set sẵn ID trong Inspector và > 0 thì giữ nguyên
            if (item.ID <= 0)
            {
                item.ID = nextAutoId;
                nextAutoId++;
            }
            else
            {
                // Đảm bảo auto-ID sau này không đè lên ID đã set tay
                if (item.ID >= nextAutoId)
                {
                    nextAutoId = item.ID + 1;
                }
            }

            if (itemDictionary.ContainsKey(item.ID))
            {
                Debug.LogWarning($"[ItemDictionary] Duplicate Item ID {item.ID} for prefab '{prefab.name}'. " +
                                 "Only the first one will be used.");
                invalidCount++;
                continue;
            }

            itemDictionary[item.ID] = prefab;
            validCount++;
        }

        Debug.Log($"[ItemDictionary] Initialized singleton with {itemDictionary.Count} items. " +
                  $"Valid: {validCount}, Invalid: {invalidCount}");
    }

    public GameObject GetItemPrefab(int itemID)
    {
        if (itemID <= 0)
        {
            Debug.LogError($"[ItemDictionary] Invalid itemID {itemID} requested.");
            return null;
        }

        // Phòng trường hợp dictionary chưa init vì lý do nào đó
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Debug.LogWarning("[ItemDictionary] Dictionary is empty or null, reinitializing...");
            InitializeDictionary();
        }

        if (itemDictionary != null && itemDictionary.TryGetValue(itemID, out GameObject prefab))
        {
            return prefab;
        }

        // Build list of available IDs for error message
        string availableIDs;
        if (itemDictionary != null && itemDictionary.Count > 0)
        {
            List<int> ids = new List<int>(itemDictionary.Keys);
            availableIDs = string.Join(", ", ids);
        }
        else
        {
            availableIDs = "none (dictionary is empty)";
        }

        Debug.LogError($"[ItemDictionary] Item with ID {itemID} not found in dictionary. " +
                       $"Dictionary has {itemDictionary?.Count ?? 0} items. Available IDs: {availableIDs}");
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
