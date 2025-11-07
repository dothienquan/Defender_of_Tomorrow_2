using UnityEngine;


[CreateAssetMenu(menuName = "New Weapon")]
public class WeaponInfo : ItemBase
{
    public GameObject weaponPrefab;
    public float weaponCooldown;
    public int weaponDamage;
    public float weaponRange;
}