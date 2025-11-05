using UnityEngine;


[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class UIItem : MonoBehaviour
{
    [Header("Data")]
    public ItemBase itemData; // Assign in prefab (WeaponInfo or other types later)


    public WeaponInfo AsWeapon() => itemData as WeaponInfo;
}