using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script để hiển thị cooldown của weapon bằng fill UI
/// Gắn vào GameObject có Image component với Image Type = Filled
/// </summary>
[RequireComponent(typeof(Image))]
public class WeaponCooldownUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image component dùng để fill (phải có Image Type = Filled)")]
    [SerializeField] private Image fillImage;
    
    [Tooltip("Icon của weapon (lấy từ component gốc, không tự động cập nhật)")]
    [SerializeField] private Image weaponIconImage;
    
    [Header("Settings")]
    [Tooltip("Nếu true, sẽ ẩn icon khi không có weapon")]
    [SerializeField] private bool hideWhenNoWeapon = false; // Mặc định false để icon luôn hiển thị
    
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
        
        // Tự động tìm weapon icon nếu chưa gán (tìm trong children, trừ fillImage)
        if (weaponIconImage == null)
        {
            Image[] allImages = GetComponentsInChildren<Image>();
            foreach (Image img in allImages)
            {
                if (img != fillImage)
                {
                    weaponIconImage = img;
                    break;
                }
            }
        }
        
        // Lưu màu ban đầu của icon
        if (weaponIconImage != null)
        {
            originalIconColor = weaponIconImage.color;
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
    /// Update cooldown fill dựa trên ActiveWeapon
    /// </summary>
    private void UpdateCooldown()
    {
        if (ActiveWeapon.Instance == null)
        {
            // Không có ActiveWeapon, reset fill
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
            
            if (hideWhenNoWeapon)
            {
                gameObject.SetActive(false);
            }
            return;
        }

        // Lấy weapon hiện tại
        MonoBehaviour currentWeapon = ActiveWeapon.Instance.CurrentActiveWeapon;
        
        if (currentWeapon == null)
        {
            // Không có weapon, reset fill
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
            
            // Reset icon color về ban đầu
            if (weaponIconImage != null)
            {
                weaponIconImage.color = originalIconColor;
            }
            
            if (hideWhenNoWeapon)
            {
                gameObject.SetActive(false);
            }
            
            return;
        }

        // Đảm bảo gameObject được active
        if (hideWhenNoWeapon && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // Lấy WeaponInfo
        IWeapon weapon = currentWeapon as IWeapon;
        if (weapon == null)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
            return;
        }

        WeaponInfo weaponInfo = weapon.GetWeaponInfo();
        if (weaponInfo == null)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
            // Reset icon color về ban đầu
            if (weaponIconImage != null)
            {
                weaponIconImage.color = originalIconColor;
            }
            return;
        }

        // Lấy max cooldown từ ActiveWeapon để đảm bảo đồng bộ
        float activeWeaponMaxCooldown = ActiveWeapon.Instance.GetMaxCooldown();
        if (activeWeaponMaxCooldown > 0f)
        {
            maxCooldown = activeWeaponMaxCooldown;
        }
        else if (weaponInfo.weaponCooldown > 0f)
        {
            maxCooldown = weaponInfo.weaponCooldown;
        }

        // Lấy cooldown từ ActiveWeapon bằng reflection
        // Vì ActiveWeapon không expose public method để lấy cooldown, ta cần dùng reflection
        float remainingCooldown = GetRemainingCooldown();
        
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
            if (grayscaleOnCooldown && weaponIconImage != null)
            {
                // Tính độ sáng dựa trên fill amount (fill càng cao thì càng sáng)
                float brightness = Mathf.Lerp(grayscaleIntensity, 1f, fillAmount);
                Color grayColor = originalIconColor * brightness;
                grayColor.a = originalIconColor.a; // Giữ nguyên alpha
                weaponIconImage.color = grayColor;
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
            if (weaponIconImage != null)
            {
                weaponIconImage.color = originalIconColor;
            }
        }
    }

    /// <summary>
    /// Lấy thời gian cooldown còn lại từ ActiveWeapon
    /// </summary>
    private float GetRemainingCooldown()
    {
        if (ActiveWeapon.Instance == null)
        {
            return 0f;
        }

        return ActiveWeapon.Instance.GetRemainingCooldown();
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
        
        float fillAmount = remainingTime / maxTime;
        SetCooldownFill(fillAmount);
    }
}

