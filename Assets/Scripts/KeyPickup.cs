using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [SerializeField] private string keyId = "MainKey"; // nếu sau này có nhiều loại key

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Kiểm tra xem Key có component Item không (nếu có thì dùng hệ thống inventory)
        Item item = GetComponent<Item>();
        if (item != null && item.ID > 0)
        {
            // Key có component Item với ID hợp lệ, sử dụng inventory system
            CollectKeyAsItem();
        }
        else
        {
            // Key không có component Item hoặc ID không hợp lệ
            // Nếu Key có tag "Item", để PlayerItemCollector xử lý
            if (gameObject.CompareTag("Item"))
            {
                // PlayerItemCollector sẽ xử lý nếu Key có component Item
                // Nếu không, chỉ log và destroy
                Debug.Log("[KeyPickup] Key picked but missing Item component or ID. Key: " + keyId);
                Destroy(gameObject);
            }
            else
            {
                // Xử lý theo cách cũ (backward compatibility)
                Debug.Log("[KeyPickup] Player picked key: " + keyId);
                // TODO: xử lý logic giữ key ở đâu đó nếu không dùng inventory
                Destroy(gameObject);
            }
        }
    }

    private void CollectKeyAsItem()
    {
        Item item = GetComponent<Item>();
        if (item == null || item.ID <= 0)
        {
            Debug.LogWarning("[KeyPickup] Key does not have valid Item component with ID!");
            return;
        }

        // Tìm ItemDictionary để lấy prefab
        ItemDictionary itemDictionary = FindFirstObjectByType<ItemDictionary>();
        if (itemDictionary == null)
        {
            Debug.LogWarning("[KeyPickup] ItemDictionary not found!");
            return;
        }

        // Lấy prefab từ ItemDictionary dựa trên ID
        GameObject itemPrefab = itemDictionary.GetItemPrefab(item.ID);
        if (itemPrefab == null)
        {
            Debug.LogWarning($"[KeyPickup] Key prefab with ID {item.ID} not found in ItemDictionary! Make sure Key is added to ItemDictionary.");
            return;
        }

        // Tìm InventoryController
        InventoryController inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController == null)
        {
            Debug.LogWarning("[KeyPickup] InventoryController not found!");
            return;
        }

        // Add item to inventory (sử dụng prefab từ ItemDictionary)
        bool itemAdded = inventoryController.AddItem(itemPrefab);

        if (itemAdded)
        {
            item.PickUp();
            Debug.Log("[KeyPickup] Key added to inventory: " + keyId + " (ID: " + item.ID + ")");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("[KeyPickup] Inventory is full, cannot add key: " + keyId);
        }
    }
}
