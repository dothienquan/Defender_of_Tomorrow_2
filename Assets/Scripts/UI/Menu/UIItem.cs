using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class UIItem : MonoBehaviour
{
    [Header("Data")]
    public ItemBase itemData; // Assign in prefab (WeaponInfo or other types later)

    [Header("Visuals")]
    [Tooltip("Image component used to display the item's icon. If left empty, it will try to auto-find one on this object or a child.")]
    public Image iconImage;

    void Awake()
    {
        // Auto-find an Image if none assigned
        if (iconImage == null) iconImage = GetComponent<Image>();
        if (iconImage == null) iconImage = GetComponentInChildren<Image>(true);

        // Apply sprite from itemData.icon
        if (iconImage != null)
        {
            iconImage.sprite = itemData ? itemData.icon : null;
            iconImage.preserveAspect = true;
            iconImage.enabled = iconImage.sprite != null;
            // Ensure raycasts work so drag & drop detects hits
            iconImage.raycastTarget = true;
        }
    }

#if UNITY_EDITOR
    // Refresh the icon in editor when values change
    void OnValidate()
    {
        if (iconImage == null) iconImage = GetComponent<Image>();
        if (iconImage == null) iconImage = GetComponentInChildren<Image>(true);

        if (iconImage != null)
        {
            iconImage.sprite = itemData ? itemData.icon : null;
            iconImage.enabled = iconImage.sprite != null;
        }
    }
#endif

    public WeaponInfo AsWeapon() => itemData as WeaponInfo;
}
