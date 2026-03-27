using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script để hiển thị cooldown của dash bằng fill UI
/// Tương tự WeaponCooldownUI nhưng cho dash ability
/// </summary>
[RequireComponent(typeof(Image))]
public class DashCooldownUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image component dùng để fill (phải có Image Type = Filled)")]
    [SerializeField] private Image fillImage;
    
    [Tooltip("Icon của dash (lấy từ component gốc, không tự động cập nhật)")]
    [SerializeField] private Image dashIconImage;

    [Header("Settings")]
    [Tooltip("Nếu true, sẽ ẩn fill image khi cooldown = 0 (không có cooldown)")]
    [SerializeField] private bool hideWhenNoCooldown = false;

    [Header("Icon Grayscale")]
    [Tooltip("Nếu true, icon sẽ bị xám đi khi đang cooldown")]
    [SerializeField] private bool grayscaleOnCooldown = true;
    
    [Tooltip("Độ xám khi đang cooldown (0 = đen, 1 = bình thường)")]
    [SerializeField] private float grayscaleIntensity = 0.3f;

    private float currentCooldown = 0f;
    private float maxCooldown = 0f;
    private bool isOnCooldown = false;
    private Color originalIconColor; // Lưu màu ban đầu của icon

    private void Awake()
    {
        // Tự động tìm Image nếu chưa gán
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }
        
        // Đảm bảo Image Type = Filled
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360; // Có thể đổi thành Horizontal, Vertical, Radial90, Radial180, Radial360
            fillImage.fillAmount = 0f; // Bắt đầu với fill = 0 (không có cooldown)
        }
        
        // Tự động tìm dash icon nếu chưa gán (tìm trong children, trừ fillImage)
        if (dashIconImage == null)
        {
            Image[] allImages = GetComponentsInChildren<Image>();
            foreach (Image img in allImages)
            {
                if (img != fillImage)
                {
                    dashIconImage = img;
                    break;
                }
            }
        }
        
        // Lưu màu ban đầu của icon
        if (dashIconImage != null)
        {
            originalIconColor = dashIconImage.color;
        }
    }

    private void Start()
    {
        // Đảm bảo fill image bắt đầu ở 0
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
        }
    }

    private void Update()
    {
        UpdateCooldown();
    }

    /// <summary>
    /// Update cooldown fill dựa trên PlayerController
    /// </summary>
    private void UpdateCooldown()
    {
        if (PlayerController.Instance == null)
        {
            // Không có PlayerController, reset fill
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
            return;
        }

        // Lấy cooldown từ PlayerController
        float remainingCooldown = PlayerController.Instance.GetDashCooldownRemaining();
        float totalCooldown = PlayerController.Instance.GetDashCooldownTotal();

        if (totalCooldown > 0f)
        {
            maxCooldown = totalCooldown;
        }

        if (remainingCooldown > 0f)
        {
            isOnCooldown = true;
            currentCooldown = remainingCooldown;
            
            // Tính fill amount (0 = bắt đầu cooldown, 1 = hết cooldown)
            // Fill tăng dần khi cooldown giảm
            float fillAmount = 1f - (remainingCooldown / maxCooldown);
            
            if (fillImage != null)
            {
                fillImage.fillAmount = fillAmount;
                
                // Hiển thị fill image nếu đang cooldown
                if (hideWhenNoCooldown && !fillImage.gameObject.activeSelf)
                {
                    fillImage.gameObject.SetActive(true);
                }
            }
            
            // Làm xám icon khi đang cooldown
            if (grayscaleOnCooldown && dashIconImage != null)
            {
                // Tính độ sáng dựa trên fill amount (fill càng cao thì càng sáng)
                float brightness = Mathf.Lerp(grayscaleIntensity, 1f, fillAmount);
                Color grayColor = originalIconColor * brightness;
                grayColor.a = originalIconColor.a; // Giữ nguyên alpha
                dashIconImage.color = grayColor;
            }
        }
        else
        {
            isOnCooldown = false;
            currentCooldown = 0f;
            
            if (fillImage != null)
            {
                fillImage.fillAmount = 1f; // Fill đầy khi hết cooldown
                
                // Ẩn fill image nếu không có cooldown
                if (hideWhenNoCooldown)
                {
                    fillImage.gameObject.SetActive(false);
                }
            }
            
            // Reset icon color về ban đầu khi hết cooldown
            if (dashIconImage != null)
            {
                dashIconImage.color = originalIconColor;
            }
        }
    }

    /// <summary>
    /// Set fill amount trực tiếp (dùng khi có hệ thống cooldown tracking riêng)
    /// </summary>
    public void SetCooldownFill(float fillAmount)
    {
        if (fillImage != null)
        {
            fillAmount = Mathf.Clamp01(fillAmount);
            fillImage.fillAmount = fillAmount;
        }
    }

    /// <summary>
    /// Set cooldown trực tiếp (remaining time / max time)
    /// </summary>
    public void SetCooldown(float remainingTime, float maxTime)
    {
        if (maxTime <= 0f)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
            return;
        }

        maxCooldown = maxTime;
        currentCooldown = remainingTime;
        
        float fillAmount = 1f - (remainingTime / maxTime); // Đảo ngược để fill tăng khi cooldown giảm
        SetCooldownFill(fillAmount);
    }
}

