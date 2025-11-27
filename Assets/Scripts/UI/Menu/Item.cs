using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public int ID;
    public string Name;

    [Header("Raft")]
    public bool isRaftMaterial;   // tick = item này dùng làm nguyên liệu bè

    [Header("Weapon")]
    [Tooltip("Nếu item này là vũ khí, gán WeaponInfo vào đây")]
    public WeaponInfo weaponInfo;

    /// <summary>
    /// Kiểm tra xem item này có phải là vũ khí không
    /// </summary>
    public bool IsWeapon => weaponInfo != null;

    /// <summary>
    /// Lấy WeaponInfo nếu item là vũ khí
    /// </summary>
    public WeaponInfo GetWeaponInfo() => weaponInfo;

    public virtual void PickUp()
    {
        Sprite itemIcon = null;
        
        // Hỗ trợ cả Image (UI) và SpriteRenderer (World)
        Image image = GetComponent<Image>();
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (image != null)
        {
            itemIcon = image.sprite;
        }
        else if (spriteRenderer != null)
        {
            itemIcon = spriteRenderer.sprite;
        }
        // Nếu không có sprite từ component, thử lấy từ WeaponInfo
        else if (weaponInfo != null && weaponInfo.icon != null)
        {
            itemIcon = weaponInfo.icon;
        }
        
        if(ItemPickupUIController.Instance != null && itemIcon != null)
        {
            ItemPickupUIController.Instance.ShowItemPopup(Name, itemIcon);  
        }
    }

    public virtual void UseItem()
    {
        // Nếu là vũ khí, HotbarController sẽ xử lý việc equip
        // Nếu không phải vũ khí, xử lý logic sử dụng item thông thường
        if (!IsWeapon)
        {
            Debug.Log("Use item: " + Name);
        }
    }
}
