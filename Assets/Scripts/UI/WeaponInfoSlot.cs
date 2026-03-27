using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component cho mỗi slot trong grid hiển thị thông tin vũ khí
/// </summary>
public class WeaponInfoSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI damageText;

    [Header("Optional UI Elements")]
    [SerializeField] private GameObject cooldownContainer;
    [SerializeField] private GameObject damageContainer;

    private WeaponInfo weaponInfo;

    private void Awake()
    {
        // Auto-find components nếu chưa được assign
        if (weaponIconImage == null)
            weaponIconImage = GetComponentInChildren<Image>();

        // Tìm các text components theo tên hoặc thứ tự
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        
        if (texts != null && texts.Length > 0)
        {
            // Tìm theo tên GameObject
            foreach (var text in texts)
            {
                string name = text.gameObject.name.ToLower();
                
                if (weaponNameText == null && (name.Contains("name") || name.Contains("title")))
                {
                    weaponNameText = text;
                }
                else if (cooldownText == null && name.Contains("cooldown"))
                {
                    cooldownText = text;
                }
                else if (damageText == null && name.Contains("damage"))
                {
                    damageText = text;
                }
            }

            // Fallback: gán theo thứ tự nếu không tìm thấy theo tên
            if (weaponNameText == null && texts.Length > 0)
                weaponNameText = texts[0];
            if (cooldownText == null && texts.Length > 1)
                cooldownText = texts[1];
            if (damageText == null && texts.Length > 2)
                damageText = texts[2];
        }
    }

    /// <summary>
    /// Setup slot với WeaponInfo
    /// </summary>
    public void Setup(WeaponInfo info)
    {
        weaponInfo = info;

        if (weaponInfo == null)
        {
            ClearSlot();
            return;
        }

        // Set icon
        if (weaponIconImage != null)
        {
            weaponIconImage.sprite = weaponInfo.icon;
            weaponIconImage.enabled = weaponInfo.icon != null;
            weaponIconImage.preserveAspect = true;
        }

        // Set name
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponInfo.itemName;
        }

        // Set cooldown
        if (cooldownText != null)
        {
            cooldownText.text = $"Tốc độ đánh: {weaponInfo.weaponCooldown:F1}s";
        }

        // Set damage
        if (damageText != null)
        {
            damageText.text = $"Sát thương: {weaponInfo.weaponDamage}";
        }

        // Show/hide containers
        if (cooldownContainer != null)
        {
            cooldownContainer.SetActive(true);
        }

        if (damageContainer != null)
        {
            damageContainer.SetActive(true);
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Xóa thông tin trong slot
    /// </summary>
    public void ClearSlot()
    {
        weaponInfo = null;

        if (weaponIconImage != null)
        {
            weaponIconImage.sprite = null;
            weaponIconImage.enabled = false;
        }

        if (weaponNameText != null)
        {
            weaponNameText.text = "";
        }

        if (cooldownText != null)
        {
            cooldownText.text = "";
        }

        if (damageText != null)
        {
            damageText.text = "";
        }

        if (cooldownContainer != null)
        {
            cooldownContainer.SetActive(false);
        }

        if (damageContainer != null)
        {
            damageContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Lấy WeaponInfo hiện tại
    /// </summary>
    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }
}

