using UnityEngine;


public class InventorySaveHotkey : MonoBehaviour
{
    public InventoryController inventoryController; // assign


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            InventoryPersistence.Save(inventoryController.inventoryPanel.transform);
            Debug.Log("Inventory saved.");
        }
    }
}