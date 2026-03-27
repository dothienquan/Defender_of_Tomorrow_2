using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [SerializeField] private string keyId = "MainKey"; // nếu sau này có nhiều loại key

    [Header("Dialogue Settings")]
    [Tooltip("Dialogue hiển thị khi nhặt Key")]
    public DialogueObject pickupDialogue;

    [Tooltip("Tên hiển thị trong dialogue (mặc định: 'Key')")]
    public string dialogueSpeakerName = "Key";

    [Tooltip("Avatar hiển thị trong dialogue (có thể để trống)")]
    public Sprite dialogueAvatar;

    [Tooltip("UI Reference cho Dialogue (KHÔNG CẦN GÁN - sẽ tự động tìm trong scene khi cần)")]
    [SerializeField] private DialogueUI dialogueUI; // Không dùng, chỉ để backward compatibility

    [Tooltip("Dialogue chỉ hiển thị một lần (lưu vào PlayerPrefs)")]
    public bool showDialogueOnlyOnce = false;

    [Tooltip("Unique ID cho dialogue trigger (dùng để lưu trạng thái đã hiển thị)")]
    public string dialogueTriggerID = "";

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
                
                // Hiển thị dialogue nếu có
                ShowPickupDialogue();
                
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
            
            // Hiển thị dialogue nếu có
            ShowPickupDialogue();
            
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("[KeyPickup] Inventory is full, cannot add key: " + keyId);
        }
    }

    /// <summary>
    /// Hiển thị dialogue khi nhặt Key
    /// </summary>
    private void ShowPickupDialogue()
    {
        // Kiểm tra xem có dialogue không
        if (pickupDialogue == null)
        {
            return; // Không có dialogue, bỏ qua
        }

        // Kiểm tra xem đã hiển thị chưa (nếu showDialogueOnlyOnce = true)
        if (showDialogueOnlyOnce)
        {
            string dialogueKey = GetDialogueTriggerKey();
            bool hasShown = PlayerPrefs.GetInt(dialogueKey, 0) == 1;
            if (hasShown)
            {
                Debug.Log($"[KeyPickup] Dialogue đã được hiển thị trước đó cho Key: {keyId}. Bỏ qua...");
                return;
            }
        }

        // Tìm DialogueUI trong scene (luôn tìm lại vì Key được spawn động từ prefab)
        // Không thể gán DialogueUI vào prefab vì Key được drop ra sau khi đánh quái
        DialogueUI foundDialogueUI = FindFirstObjectByType<DialogueUI>();
        if (foundDialogueUI == null)
        {
            // Thử tìm bằng cách khác nếu FindFirstObjectByType không tìm thấy
            DialogueUI[] allDialogueUIs = FindObjectsByType<DialogueUI>(FindObjectsSortMode.None);
            if (allDialogueUIs != null && allDialogueUIs.Length > 0)
            {
                foundDialogueUI = allDialogueUIs[0];
            }
            
            if (foundDialogueUI == null)
            {
                Debug.LogWarning($"[KeyPickup] DialogueUI không tìm thấy trong scene! Không thể hiển thị dialogue cho Key: {keyId}. Đảm bảo có DialogueUI trong scene.");
                return;
            }
        }

        // Kiểm tra xem dialogue đang active không (tránh trigger nhiều lần)
        if (foundDialogueUI.gameObject.activeSelf)
        {
            Debug.Log($"[KeyPickup] Dialogue đang hiển thị. Bỏ qua dialogue cho Key: {keyId}");
            return;
        }

        // Hiển thị dialogue
        foundDialogueUI.Show(pickupDialogue, dialogueSpeakerName, dialogueAvatar);

        // Đánh dấu đã hiển thị (nếu showDialogueOnlyOnce = true)
        if (showDialogueOnlyOnce)
        {
            string dialogueKey = GetDialogueTriggerKey();
            PlayerPrefs.SetInt(dialogueKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"[KeyPickup] Dialogue đã được hiển thị và đánh dấu cho Key: {keyId}");
        }
    }

    /// <summary>
    /// Lấy key để lưu trạng thái dialogue trigger
    /// </summary>
    private string GetDialogueTriggerKey()
    {
        string id = !string.IsNullOrEmpty(dialogueTriggerID) ? dialogueTriggerID : keyId;
        return $"KeyPickup_Dialogue_{id}";
    }

    /// <summary>
    /// Reset trạng thái dialogue trigger (dùng khi New Game)
    /// </summary>
    public void ResetDialogueTriggerState()
    {
        if (showDialogueOnlyOnce)
        {
            string dialogueKey = GetDialogueTriggerKey();
            PlayerPrefs.DeleteKey(dialogueKey);
            PlayerPrefs.Save();
            Debug.Log($"[KeyPickup] Đã reset dialogue trigger state cho Key: {keyId}");
        }
    }
}
