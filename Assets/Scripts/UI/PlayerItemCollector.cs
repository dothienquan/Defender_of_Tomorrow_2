using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{
    private InventoryController inventoryController;
    private RaftMaterialCollector raftMaterialCollector;   // <-- thêm
    void Start()
    {
        inventoryController = FindFirstObjectByType<InventoryController>();
        raftMaterialCollector = GetComponent<RaftMaterialCollector>(); // <-- thêm

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Item"))
        {
            Item item = collision.GetComponent<Item>();

            if (item != null)
            {
                //Add item inventory
                bool itemAdded = inventoryController.AddItem(collision.gameObject);

                if (itemAdded)
                {
                    if (raftMaterialCollector != null && item.isRaftMaterial)
                    {
                        raftMaterialCollector.AddMaterial(1);
                    }
                    item.PickUp();
                    Destroy(collision.gameObject);
                }
            }
            

        }
    }
}
