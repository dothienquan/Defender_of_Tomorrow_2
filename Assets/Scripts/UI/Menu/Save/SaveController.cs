using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Cinemachine;

public class SaveController : MonoBehaviour
{
    private string saveLocation;
    private InventoryController inventoryController;
    private HotbarController hotbarController;

    void Start()
    {
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");
        inventoryController = FindFirstObjectByType<InventoryController>();
        hotbarController = FindFirstObjectByType<HotbarController>();

        LoadGame();
    }

    public void SaveGame()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var confiner = FindFirstObjectByType<CinemachineConfiner>();

        if (player == null)
        {
            Debug.LogError("Cannot save: Player not found in scene!");
            return;
        }

        if (confiner == null)
        {
            Debug.LogError("Cannot save: Confiner not found in scene!");
            return;
        }

        if (inventoryController == null)
        {
            Debug.LogError("Cannot save: InventoryController not found!");
            return;
        }

        if (hotbarController == null)
        {
            Debug.LogError("Cannot save: HotbarController not found!");
            return;
        }

        SaveData saveData = new SaveData
        {
            playerPosition = player.transform.position,
            mapBoundary = confiner.m_BoundingShape2D?.gameObject.name ?? "",
            inventorySaveData = inventoryController.GetInventoryItems(),
            hotbarSaveData = hotbarController.GetHotbarItems()
        };

        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData));
        Debug.Log($"Game saved to {saveLocation}");
    }


    public void LoadGame()
    {
        if (!File.Exists(saveLocation))
        {
            Debug.LogWarning("No save file found. Creating a new one...");
            SaveGame();
            return;
        }

        SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = saveData.playerPosition;
        else
            Debug.LogError("Player not found in scene when trying to load position!");

        var confiner = FindFirstObjectByType<CinemachineConfiner>();
        if (confiner != null)
        {
            var boundary = GameObject.Find(saveData.mapBoundary);
            if (boundary != null)
                confiner.m_BoundingShape2D = boundary.GetComponent<PolygonCollider2D>();
            else
                Debug.LogError($"Map boundary '{saveData.mapBoundary}' not found in scene!");
        }

        if (inventoryController != null)
            inventoryController.SetInventoryItems(saveData.inventorySaveData);
        else
            Debug.LogWarning("InventoryController not found — skipping inventory load.");

        if (hotbarController != null)
            hotbarController.SetHotbarItems(saveData.hotbarSaveData);
        else
            Debug.LogWarning("HotbarController not found — skipping hotbar load.");

        Debug.Log("Game loaded successfully.");
    }

    public void DeleteSave()
    {
        if (File.Exists(saveLocation))
        {
            File.Delete(saveLocation);
            Debug.Log("Save file deleted successfully.");
        }
        else
        {
            Debug.LogWarning("No save file found to delete.");
        }
    }
}
