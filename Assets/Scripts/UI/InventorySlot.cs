using UnityEngine;


public class InventorySlot : MonoBehaviour
{
    [SerializeField] private WeaponInfo weaponInfo;


    public WeaponInfo GetWeaponInfo() => weaponInfo;


    // NEW: allow external systems (drag handler / load) to assign
    public void SetWeapon(WeaponInfo newWeapon)
    {
        weaponInfo = newWeapon;
    }
}