using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI component cho LockedGate panel
/// </summary>
public class LockedGateUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText; // Text hiển thị trạng thái
    [SerializeField] private TextMeshProUGUI keyInfoText; // Text hiển thị thông tin Key
    [SerializeField] private Button confirmButton; // Nút xác nhận mở cổng
    [SerializeField] private Button cancelButton; // Nút hủy/đóng panel
    [SerializeField] private Image keyIcon; // Icon của Key (tùy chọn)

    [Header("Messages")]
    [SerializeField] private string confirmButtonText = "Mo cong";
    [SerializeField] private string cancelButtonText = "Huy";
    [SerializeField] private string statusFormat = "{0}/{1}"; // Format: "0/1" hoặc "1/1"

    private LockedGate lockedGate;
    private ItemDictionary itemDictionary;

    private void Awake()
    {
        // Tự động tìm LockedGate
        lockedGate = GetComponentInParent<LockedGate>();
        if (lockedGate == null)
        {
            lockedGate = FindFirstObjectByType<LockedGate>();
        }

        // Tự động tìm ItemDictionary
        itemDictionary = FindFirstObjectByType<ItemDictionary>();

        // Setup buttons
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        // Ẩn panel ban đầu
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Cập nhật UI với trạng thái Key
    /// </summary>
    public void UpdateUI(bool hasKey, int keyID, int requiredCount = 1)
    {
        // Cập nhật status text dạng "x/y"
        if (statusText != null)
        {
            int currentCount = GetKeyCount(keyID);
            statusText.text = string.Format(statusFormat, currentCount, requiredCount);
        }

        // Cập nhật key info
        if (keyInfoText != null)
        {
            string keyName = GetKeyName(keyID);
            keyInfoText.text = $"Yeu cau";
        }

        // Enable/disable confirm button dựa trên việc có Key
        if (confirmButton != null)
        {
            confirmButton.interactable = hasKey;
            
            // Cập nhật text button
            TextMeshProUGUI buttonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = confirmButtonText;
            }
        }

        // Cập nhật key icon nếu có
        if (keyIcon != null && hasKey)
        {
            Sprite keySprite = GetKeyIcon(keyID);
            if (keySprite != null)
            {
                keyIcon.sprite = keySprite;
                keyIcon.enabled = true;
            }
            else
            {
                keyIcon.enabled = false;
            }
        }
    }

    private void OnConfirmClicked()
    {
        if (lockedGate != null)
        {
            lockedGate.UnlockGate();
        }
        else
        {
            Debug.LogWarning("[LockedGateUI] LockedGate not found!");
        }
    }

    private void OnCancelClicked()
    {
        // Ẩn panel
        if (lockedGate != null)
        {
            // LockedGate sẽ tự ẩn khi player rời khỏi trigger
            // Hoặc có thể gọi trực tiếp
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Hiển thị message tạm thời
    /// </summary>
    public void ShowMessage(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private string GetKeyName(int keyID)
    {
        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (itemDictionary != null)
        {
            GameObject keyPrefab = itemDictionary.GetItemPrefab(keyID);
            if (keyPrefab != null)
            {
                Item item = keyPrefab.GetComponent<Item>();
                if (item != null && !string.IsNullOrEmpty(item.Name))
                {
                    return item.Name;
                }
            }
        }

        return "Key";
    }

    private Sprite GetKeyIcon(int keyID)
    {
        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (itemDictionary != null)
        {
            GameObject keyPrefab = itemDictionary.GetItemPrefab(keyID);
            if (keyPrefab != null)
            {
                Item item = keyPrefab.GetComponent<Item>();
                if (item != null)
                {
                    // Thử lấy sprite từ Image hoặc SpriteRenderer
                    UnityEngine.UI.Image image = keyPrefab.GetComponent<UnityEngine.UI.Image>();
                    if (image != null && image.sprite != null)
                    {
                        return image.sprite;
                    }

                    SpriteRenderer spriteRenderer = keyPrefab.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        return spriteRenderer.sprite;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Lấy số lượng Key hiện có trong inventory
    /// </summary>
    private int GetKeyCount(int keyID)
    {
        InventoryController inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController != null)
        {
            return inventoryController.GetItemCount(keyID);
        }
        return 0;
    }
}

