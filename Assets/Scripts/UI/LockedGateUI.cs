using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

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
    [SerializeField] private string confirmButtonText = "Mở khóa";
    [SerializeField] private string cancelButtonText = "Hủy";
    [SerializeField] private string statusFormat = "{0}/{1}"; // Format: "0/1" hoặc "1/1"
    
    [Header("Confirm Animation")]
    [SerializeField] private float rotationAngle = 45f; // Góc xoay (độ)
    [SerializeField] private float animationDuration = 3f; // Thời gian animation (giây)

    private LockedGate lockedGate;
    private ItemDictionary itemDictionary;
    private bool isAnimating = false; // Tránh click nhiều lần
    private Tween rotationTween; // Lưu tween để có thể kill nếu cần

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
            // Đảm bảo button có thể tương tác
            confirmButton.interactable = true;
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
            // Đảm bảo button có thể tương tác
            cancelButton.interactable = true;
        }

        // Ẩn panel ban đầu
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Khi panel được enable, đảm bảo buttons có thể tương tác
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }
        if (cancelButton != null)
        {
            cancelButton.interactable = true;
        }
        
        // Reset animation state
        isAnimating = false;
        
        // Force update Canvas để đảm bảo UI được render đúng
        Canvas.ForceUpdateCanvases();
        
        // Đảm bảo EventSystem hoạt động
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null)
        {
            // Clear selected object để tránh conflict
            eventSystem.SetSelectedGameObject(null);
        }
        else
        {
            Debug.LogWarning("[LockedGateUI] EventSystem not found! UI interactions may not work.");
        }
    }

    private void OnDestroy()
    {
        // Kill tween khi destroy để tránh memory leak
        if (rotationTween != null && rotationTween.IsActive())
        {
            rotationTween.Kill();
        }
    }

    private void OnDisable()
    {
        // Kill tween khi disable
        if (rotationTween != null && rotationTween.IsActive())
        {
            rotationTween.Kill();
        }
        
        // Reset trạng thái
        isAnimating = false;
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }
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
            keyInfoText.text = $"Yêu cầu";
        }

        // Enable/disable confirm button dựa trên việc có Key
        // LƯU Ý: Luôn cho phép button interactable để có thể click và hiển thị message nếu không có key
        // Logic kiểm tra key sẽ được xử lý trong OnConfirmClicked
        if (confirmButton != null)
        {
            // Luôn cho phép button interactable để có thể click
            confirmButton.interactable = true;
            
            // Cập nhật text button
            TextMeshProUGUI buttonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = confirmButtonText;
            }
        }
        
        // Force update Canvas sau khi update UI
        Canvas.ForceUpdateCanvases();

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
        // Tránh click nhiều lần khi đang animation
        if (isAnimating || confirmButton == null)
        {
            return;
        }

        if (lockedGate == null)
        {
            Debug.LogWarning("[LockedGateUI] LockedGate not found!");
            return;
        }

        // Kiểm tra có chìa khóa trước khi bắt đầu animation
        bool hasKey = CheckHasKey();
        if (!hasKey)
        {
            // Không có chìa khóa, hiển thị message và cho phép bấm lại
            ShowMessage("Bạn chưa có chìa khóa!");
            Debug.LogWarning("[LockedGateUI] Player does not have required key!");
            return;
        }

        // Có chìa khóa, bắt đầu animation
        // Disable button để tránh click nhiều lần
        confirmButton.interactable = false;
        isAnimating = true;

        // Lưu rotation ban đầu
        Vector3 originalRotation = confirmButton.transform.localEulerAngles;
        Vector3 targetRotation = originalRotation + new Vector3(0f, 0f, rotationAngle);

        // Xoay button 45 độ trong 3 giây
        rotationTween = confirmButton.transform.DORotate(targetRotation, animationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear) // Xoay đều
            .OnComplete(() =>
            {
                // Sau khi animation xong, mở khóa
                if (lockedGate != null)
                {
                    lockedGate.UnlockGate();
                }
                
                // Reset rotation về ban đầu
                confirmButton.transform.localEulerAngles = originalRotation;
                
                // Reset trạng thái
                isAnimating = false;
            });
    }

    /// <summary>
    /// Kiểm tra xem player có chìa khóa không
    /// </summary>
    private bool CheckHasKey()
    {
        if (lockedGate == null)
        {
            Debug.LogWarning("[LockedGateUI] LockedGate is null!");
            return false;
        }

        InventoryController inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController == null)
        {
            Debug.LogWarning("[LockedGateUI] InventoryController not found!");
            return false;
        }

        // Sử dụng public method GetRequiredKeyID() từ LockedGate
        int requiredKeyID = lockedGate.GetRequiredKeyID();
        bool hasKey = inventoryController.HasItem(requiredKeyID);
        
        Debug.Log($"[LockedGateUI] Checking for key ID {requiredKeyID}: {(hasKey ? "FOUND" : "NOT FOUND")}");
        
        // Debug: In ra tất cả items trong inventory nếu không tìm thấy
        if (!hasKey)
        {
            int itemCount = inventoryController.GetItemCount(requiredKeyID);
            Debug.LogWarning($"[LockedGateUI] Key ID {requiredKeyID} not found. Item count: {itemCount}");
            
            // Debug: In ra tất cả items trong inventory (nếu có thể)
            // Note: inventoryPanel có thể không public, nên chỉ log item count
            Debug.Log($"[LockedGateUI] Item count for ID {requiredKeyID}: {itemCount}");
        }
        
        return hasKey;
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

