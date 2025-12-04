using System.IO;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Cinemachine;

public class SaveController : MonoBehaviour
{
    private string saveLocation;
    private InventoryController inventoryController;
    private HotbarController hotbarController;

    [Header("Load Settings")]
    [Tooltip("Delay trước khi load game để đảm bảo UI đã sẵn sàng")]
    [SerializeField] private float loadDelay = 0.1f;

    void Start()
    {
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");
        
        // Delay load để đảm bảo UI đã được khởi tạo
        StartCoroutine(LoadGameDelayed());
    }

    private IEnumerator LoadGameDelayed()
    {
        // Đợi một frame để đảm bảo tất cả Start() đã được gọi
        yield return null;
        
        // Đợi thêm một chút để UI hoàn toàn sẵn sàng
        yield return new WaitForSeconds(loadDelay);
        
        // Tìm controllers
        inventoryController = FindFirstObjectByType<InventoryController>();
        hotbarController = FindFirstObjectByType<HotbarController>();
        
        // Kiểm tra controllers có sẵn sàng không
        if (inventoryController == null)
        {
            Debug.LogWarning("[SaveController] InventoryController not found, retrying...");
            yield return new WaitForSeconds(0.1f);
            inventoryController = FindFirstObjectByType<InventoryController>();
        }
        
        if (hotbarController == null)
        {
            Debug.LogWarning("[SaveController] HotbarController not found, retrying...");
            yield return new WaitForSeconds(0.1f);
            hotbarController = FindFirstObjectByType<HotbarController>();
        }
        
        // Load game
        LoadGame();
    }

    public void SaveGame()
    {
        // Tìm lại controllers nếu chưa có (tránh null reference)
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        if (hotbarController == null)
        {
            hotbarController = FindFirstObjectByType<HotbarController>();
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        var confiner = FindFirstObjectByType<CinemachineConfiner>();

        if (player == null)
        {
            Debug.LogError("[SaveController] Cannot save: Player not found in scene!");
            return;
        }

        if (confiner == null)
        {
            Debug.LogError("[SaveController] Cannot save: Confiner not found in scene!");
            return;
        }

        if (inventoryController == null)
        {
            Debug.LogError("[SaveController] Cannot save: InventoryController not found!");
            return;
        }

        if (hotbarController == null)
        {
            Debug.LogError("[SaveController] Cannot save: HotbarController not found!");
            return;
        }

        // Lấy dữ liệu inventory và hotbar
        List<InventorySaveData> inventoryData = inventoryController.GetInventoryItems();
        List<InventorySaveData> hotbarData = hotbarController.GetHotbarItems();

        // Debug: Kiểm tra dữ liệu trước khi save
        Debug.Log($"[SaveController] Preparing to save:");
        Debug.Log($"[SaveController] - Inventory items: {inventoryData?.Count ?? 0}");
        Debug.Log($"[SaveController] - Hotbar items: {hotbarData?.Count ?? 0}");
        
        if (inventoryData != null && inventoryData.Count > 0)
        {
            foreach (var data in inventoryData)
            {
                Debug.Log($"[SaveController] - Inventory: Slot {data.slotIndex}, Item ID {data.itemID}");
            }
        }
        
        if (hotbarData != null && hotbarData.Count > 0)
        {
            foreach (var data in hotbarData)
            {
                Debug.Log($"[SaveController] - Hotbar: Slot {data.slotIndex}, Item ID {data.itemID}");
            }
        }

        SaveData saveData = new SaveData
        {
            playerPosition = player.transform.position,
            mapBoundary = confiner.m_BoundingShape2D?.gameObject.name ?? "",
            inventorySaveData = inventoryData ?? new List<InventorySaveData>(),
            hotbarSaveData = hotbarData ?? new List<InventorySaveData>()
        };

        string jsonData = JsonUtility.ToJson(saveData);
        
        // Debug: In ra JSON để kiểm tra trước khi save
        Debug.Log($"[SaveController] Save JSON (full): {jsonData}");
        
        File.WriteAllText(saveLocation, jsonData);
        
        // Verify file was written
        if (File.Exists(saveLocation))
        {
            string savedContent = File.ReadAllText(saveLocation);
            Debug.Log($"[SaveController] Verified save file exists. Size: {savedContent.Length} bytes");
        }
        else
        {
            Debug.LogError($"[SaveController] Save file was not created at {saveLocation}!");
        }
        
        Debug.Log($"[SaveController] Game saved to {saveLocation}");
        Debug.Log($"[SaveController] Saved {inventoryData?.Count ?? 0} inventory items and {hotbarData?.Count ?? 0} hotbar items.");
    }


    public void LoadGame()
    {
        if (string.IsNullOrEmpty(saveLocation))
        {
            saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");
        }

        if (!File.Exists(saveLocation))
        {
            Debug.LogWarning($"[SaveController] No save file found at {saveLocation}. Starting new game...");
            return;
        }

        try
        {
            string jsonData = File.ReadAllText(saveLocation);
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogWarning("[SaveController] Save file is empty. Starting new game...");
                return;
            }

            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);
            if (saveData == null)
            {
                Debug.LogError("[SaveController] Failed to parse save data from JSON!");
                return;
            }

            Debug.Log($"[SaveController] Loaded save data: {saveData.inventorySaveData?.Count ?? 0} inventory items, {saveData.hotbarSaveData?.Count ?? 0} hotbar items.");

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                player.transform.position = saveData.playerPosition;
            else
                Debug.LogError("[SaveController] Player not found in scene when trying to load position!");

            var confiner = FindFirstObjectByType<CinemachineConfiner>();
            if (confiner != null)
            {
                var boundary = GameObject.Find(saveData.mapBoundary);
                if (boundary != null)
                    confiner.m_BoundingShape2D = boundary.GetComponent<PolygonCollider2D>();
                else
                    Debug.LogError($"[SaveController] Map boundary '{saveData.mapBoundary}' not found in scene!");
            }

            // Tìm lại controllers nếu chưa có
            if (inventoryController == null)
            {
                inventoryController = FindFirstObjectByType<InventoryController>();
            }

            if (hotbarController == null)
            {
                hotbarController = FindFirstObjectByType<HotbarController>();
            }

            if (inventoryController != null)
            {
                if (saveData.inventorySaveData != null && saveData.inventorySaveData.Count > 0)
                {
                    Debug.Log($"[SaveController] Loading {saveData.inventorySaveData.Count} items into inventory...");
                    inventoryController.SetInventoryItems(saveData.inventorySaveData);
                }
                else
                {
                    Debug.Log("[SaveController] No inventory items to load.");
                }
            }
            else
            {
                Debug.LogWarning("[SaveController] InventoryController not found - skipping inventory load.");
            }

            if (hotbarController != null)
            {
                if (saveData.hotbarSaveData != null && saveData.hotbarSaveData.Count > 0)
                {
                    Debug.Log($"[SaveController] Loading {saveData.hotbarSaveData.Count} items into hotbar...");
                    hotbarController.SetHotbarItems(saveData.hotbarSaveData);
                }
                else
                {
                    Debug.Log("[SaveController] No hotbar items to load.");
                }
            }
            else
            {
                Debug.LogWarning("[SaveController] HotbarController not found - skipping hotbar load.");
            }

            Debug.Log("[SaveController] Game loaded successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveController] Error loading game: {e.Message}\n{e.StackTrace}");
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(saveLocation))
        {
            File.Delete(saveLocation);
            Debug.Log("[SaveController] Save file deleted successfully.");
        }
        else
        {
            Debug.LogWarning("[SaveController] No save file found to delete.");
        }
    }
}
