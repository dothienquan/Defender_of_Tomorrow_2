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
    [SerializeField] private string confirmButtonText = "Mo cong";
    [SerializeField] private string cancelButtonText = "Huy";
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
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        // Ẩn panel ban đầu
        gameObject.SetActive(false);
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
            return false;
        }

        // Sử dụng reflection hoặc public method từ LockedGate
        // Hoặc tự kiểm tra từ InventoryController
        InventoryController inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController == null)
        {
            return false;
        }

        // Lấy requiredKeyID từ LockedGate
        // Vì LockedGate không expose public field, ta cần dùng reflection hoặc thêm public method
        // Tạm thời dùng cách đơn giản: kiểm tra qua lockedGate
        // Nếu lockedGate có method CheckHasKey, dùng nó
        var checkHasKeyMethod = lockedGate.GetType().GetMethod("CheckHasKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (checkHasKeyMethod != null)
        {
            return (bool)checkHasKeyMethod.Invoke(lockedGate, null);
        }

        // Fallback: tìm requiredKeyID từ LockedGate bằng reflection
        var requiredKeyIDField = lockedGate.GetType().GetField("requiredKeyID",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (requiredKeyIDField != null)
        {
            int requiredKeyID = (int)requiredKeyIDField.GetValue(lockedGate);
            return inventoryController.HasItem(requiredKeyID);
        }

        return false;
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

