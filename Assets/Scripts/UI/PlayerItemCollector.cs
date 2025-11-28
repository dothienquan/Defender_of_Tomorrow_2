using UnityEngine;
using System.Collections.Generic;

public class PlayerItemCollector : MonoBehaviour
{
    private InventoryController inventoryController;
    private RaftMaterialCollector raftMaterialCollector;
    
    [Header("Collection Settings")]
    [SerializeField] private float collectDistance = 0.5f; // Khoảng cách để collect item (khi item bay đến đủ gần)
    
    private HashSet<GameObject> nearbyItems = new HashSet<GameObject>(); // Danh sách items gần player

    void Start()
    {
        inventoryController = FindFirstObjectByType<InventoryController>();
        raftMaterialCollector = GetComponent<RaftMaterialCollector>();
    }

    private void Update()
    {
        // Kiểm tra các items gần player và collect nếu đủ gần
        CheckAndCollectNearbyItems();
    }

    private void CheckAndCollectNearbyItems()
    {
        // Tạo danh sách tạm để tránh modify collection trong khi iterate
        List<GameObject> itemsToCheck = new List<GameObject>(nearbyItems);
        
        foreach (GameObject itemObj in itemsToCheck)
        {
            if (itemObj == null)
            {
                nearbyItems.Remove(itemObj);
                continue;
            }

            float distance = Vector3.Distance(transform.position, itemObj.transform.position);
            
            if (distance <= collectDistance)
            {
                CollectItem(itemObj);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Item"))
        {
            // Thêm vào danh sách items gần player
            if (!nearbyItems.Contains(collision.gameObject))
            {
                nearbyItems.Add(collision.gameObject);
            }

            // Vẫn collect ngay nếu chạm trực tiếp (backward compatibility)
            Item item = collision.GetComponent<Item>();
            if (item != null)
            {
                CollectItem(collision.gameObject);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Item"))
        {
            nearbyItems.Remove(collision.gameObject);
        }
    }

    private void CollectItem(GameObject itemObj)
    {
        if (itemObj == null) return;

        Item item = itemObj.GetComponent<Item>();
        if (item == null) return;

        // Lấy prefab từ ItemDictionary dựa trên Item.ID
        ItemDictionary itemDictionary = FindFirstObjectByType<ItemDictionary>();
        GameObject itemPrefab = null;

        if (itemDictionary != null && item.ID > 0)
        {
            itemPrefab = itemDictionary.GetItemPrefab(item.ID);
        }

        // Nếu không tìm thấy prefab từ ItemDictionary, thử dùng itemObj trực tiếp (backward compatibility)
        if (itemPrefab == null)
        {
            Debug.LogWarning($"[PlayerItemCollector] Item prefab with ID {item.ID} not found in ItemDictionary. Using item object directly.");
            itemPrefab = itemObj;
        }

        // Add item to inventory
        bool itemAdded = inventoryController.AddItem(itemPrefab);

        if (itemAdded)
        {
            if (raftMaterialCollector != null && item.isRaftMaterial)
            {
                raftMaterialCollector.AddMaterial(1);
            }
            
            item.PickUp();
            
            // Remove from tracking
            nearbyItems.Remove(itemObj);
            
            // Destroy item
            Destroy(itemObj);
        }
    }
}
