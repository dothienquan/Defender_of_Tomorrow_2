using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component cho shop slot - hiển thị weapon và cho phép mua
/// </summary>
public class ShopSlot : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image hiển thị weapon icon")]
    [SerializeField] private Image weaponIcon;
    
    [Tooltip("Text hiển thị weapon name")]
    [SerializeField] private TMP_Text weaponNameText;
    
    [Tooltip("Text hiển thị price")]
    [SerializeField] private TMP_Text priceText;
    
    [Tooltip("Button mua weapon")]
    [SerializeField] private Button buyButton;

    private WeaponInfo weaponInfo;
    private ShopController shopController;

    private void Awake()
    {
        // Tự động tìm UI components nếu chưa được gán
        if (weaponIcon == null)
        {
            weaponIcon = transform.Find("Icon")?.GetComponent<Image>();
        }

        if (weaponNameText == null)
        {
            weaponNameText = transform.Find("Name")?.GetComponent<TMP_Text>();
        }

        if (priceText == null)
        {
            priceText = transform.Find("Price")?.GetComponent<TMP_Text>();
        }

        if (buyButton == null)
        {
            buyButton = GetComponent<Button>();
            if (buyButton == null)
            {
                buyButton = transform.Find("BuyButton")?.GetComponent<Button>();
            }
        }

        // Setup buy button
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    /// <summary>
    /// Setup shop slot với weapon info
    /// </summary>
    public void SetupSlot(WeaponInfo weapon, ShopController shop)
    {
        weaponInfo = weapon;
        shopController = shop;

        if (weaponInfo == null)
        {
            Debug.LogError("[ShopSlot] Cannot setup slot: WeaponInfo is null!");
            return;
        }

        // Setup icon
        if (weaponIcon != null)
        {
            if (weaponInfo.icon != null)
            {
                weaponIcon.sprite = weaponInfo.icon;
                weaponIcon.enabled = true;
            }
            else
            {
                weaponIcon.enabled = false;
                Debug.LogWarning($"[ShopSlot] Weapon '{weaponInfo.itemName}' has no icon!");
            }
        }

        // Setup name
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponInfo.itemName;
        }

        // Setup price
        if (priceText != null)
        {
            priceText.text = weaponInfo.shopPrice.ToString();
        }

        // Update button state
        UpdateButtonState();
    }

    /// <summary>
    /// Cập nhật trạng thái buy button (enable/disable dựa trên gold)
    /// </summary>
    private void UpdateButtonState()
    {
        if (buyButton == null || weaponInfo == null) return;

        if (EconomyManager.Instance != null)
        {
            bool canAfford = EconomyManager.Instance.HasEnoughGold(weaponInfo.shopPrice);
            buyButton.interactable = canAfford;

            // Có thể thay đổi màu button để hiển thị trạng thái
            Image buttonImage = buyButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = canAfford ? Color.white : Color.gray;
            }
        }
    }

    /// <summary>
    /// Khi click buy button
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (shopController == null)
        {
            Debug.LogError("[ShopSlot] ShopController is null! Cannot buy weapon.");
            return;
        }

        if (weaponInfo == null)
        {
            Debug.LogError("[ShopSlot] WeaponInfo is null! Cannot buy weapon.");
            return;
        }

        bool success = shopController.BuyWeapon(weaponInfo);
        
        if (success)
        {
            // Update button state sau khi mua
            UpdateButtonState();
        }
    }

    private void Update()
    {
        // Update button state mỗi frame để phản ánh gold changes
        // (Có thể optimize bằng cách chỉ update khi gold thay đổi)
        if (weaponInfo != null)
        {
            UpdateButtonState();
        }
    }
}
