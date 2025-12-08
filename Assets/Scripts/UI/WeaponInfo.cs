using UnityEngine;


[CreateAssetMenu(menuName = "New Weapon")]
public class WeaponInfo : ItemBase
{
    public GameObject weaponPrefab;
    public float weaponCooldown;
    public int weaponDamage;
    public float weaponRange;
    
    [Header("Shop Settings")]
    [Tooltip("Giá mua vũ khí này trong shop (gold coin). Đặt 0 nếu không thể mua.")]
    public int shopPrice = 0;
}