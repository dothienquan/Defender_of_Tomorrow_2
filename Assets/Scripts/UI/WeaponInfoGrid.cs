using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Script quản lý grid layout hiển thị thông tin các vũ khí
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class WeaponInfoGrid : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject weaponSlotPrefab;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    [Header("Settings")]
    [Tooltip("Nếu true, sẽ tự động tìm tất cả WeaponInfo trong project khi Start")]
    [SerializeField] private bool autoLoadWeaponsOnStart = false;
    
    [Tooltip("Danh sách WeaponInfo để hiển thị (có thể set trong Inspector hoặc load tự động)")]
    [SerializeField] private List<WeaponInfo> weaponsToDisplay = new List<WeaponInfo>();

    [Header("Grid Settings")]
    [Tooltip("Số cột trong grid (0 = tự động tính)")]
    [SerializeField] private int preferredColumns = 3;

    private List<WeaponInfoSlot> weaponSlots = new List<WeaponInfoSlot>();

    private void Awake()
    {
        // Tìm GridLayoutGroup
        if (gridLayoutGroup == null)
        {
            gridLayoutGroup = GetComponent<GridLayoutGroup>();
        }

        if (gridLayoutGroup == null)
        {
            Debug.LogError($"[WeaponInfoGrid] GridLayoutGroup component not found on {gameObject.name}!");
            enabled = false;
            return;
        }

        // Setup grid layout
        if (preferredColumns > 0)
        {
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = preferredColumns;
        }
    }

    private void Start()
    {
        if (autoLoadWeaponsOnStart)
        {
            LoadAllWeapons();
        }
        else if (weaponsToDisplay != null && weaponsToDisplay.Count > 0)
        {
            DisplayWeapons(weaponsToDisplay);
        }
    }

    /// <summary>
    /// Tự động load tất cả WeaponInfo trong project
    /// </summary>
    public void LoadAllWeapons()
    {
        // Load tất cả WeaponInfo từ Resources hoặc ScriptableObjects
        WeaponInfo[] allWeapons = Resources.LoadAll<WeaponInfo>("");
        
        // Nếu không tìm thấy trong Resources, thử tìm trong project (chỉ trong Editor)
        #if UNITY_EDITOR
        if (allWeapons == null || allWeapons.Length == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WeaponInfo");
            List<WeaponInfo> weapons = new List<WeaponInfo>();
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                WeaponInfo weapon = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponInfo>(path);
                if (weapon != null)
                {
                    weapons.Add(weapon);
                }
            }
            allWeapons = weapons.ToArray();
        }
        #endif

        if (allWeapons != null && allWeapons.Length > 0)
        {
            DisplayWeapons(allWeapons.ToList());
            Debug.Log($"[WeaponInfoGrid] Loaded {allWeapons.Length} weapons from project.");
        }
        else
        {
            Debug.LogWarning("[WeaponInfoGrid] No weapons found in project. Make sure WeaponInfo ScriptableObjects exist.");
        }
    }

    /// <summary>
    /// Hiển thị danh sách vũ khí trong grid
    /// </summary>
    public void DisplayWeapons(List<WeaponInfo> weapons)
    {
        if (weapons == null || weapons.Count == 0)
        {
            ClearGrid();
            return;
        }

        // Đảm bảo có đủ slots
        EnsureSlotsCount(weapons.Count);

        // Setup từng slot
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null && i < weaponSlots.Count)
            {
                weaponSlots[i].Setup(weapons[i]);
            }
        }

        // Clear các slot không sử dụng
        for (int i = weapons.Count; i < weaponSlots.Count; i++)
        {
            weaponSlots[i].ClearSlot();
            weaponSlots[i].gameObject.SetActive(false);
        }

        weaponsToDisplay = weapons;
    }

    /// <summary>
    /// Thêm một vũ khí vào grid
    /// </summary>
    public void AddWeapon(WeaponInfo weapon)
    {
        if (weapon == null) return;

        // Tìm slot trống hoặc tạo mới
        WeaponInfoSlot emptySlot = weaponSlots.FirstOrDefault(slot => slot.GetWeaponInfo() == null);
        
        if (emptySlot == null)
        {
            // Tạo slot mới
            emptySlot = CreateSlot();
            weaponSlots.Add(emptySlot);
        }

        emptySlot.Setup(weapon);
        emptySlot.gameObject.SetActive(true);

        // Thêm vào danh sách
        if (!weaponsToDisplay.Contains(weapon))
        {
            weaponsToDisplay.Add(weapon);
        }
    }

    /// <summary>
    /// Xóa một vũ khí khỏi grid
    /// </summary>
    public void RemoveWeapon(WeaponInfo weapon)
    {
        if (weapon == null) return;

        WeaponInfoSlot slot = weaponSlots.FirstOrDefault(s => s.GetWeaponInfo() == weapon);
        if (slot != null)
        {
            slot.ClearSlot();
            slot.gameObject.SetActive(false);
        }

        weaponsToDisplay.Remove(weapon);
    }

    /// <summary>
    /// Xóa tất cả vũ khí khỏi grid
    /// </summary>
    public void ClearGrid()
    {
        foreach (var slot in weaponSlots)
        {
            slot.ClearSlot();
            slot.gameObject.SetActive(false);
        }

        weaponsToDisplay.Clear();
    }

    /// <summary>
    /// Đảm bảo có đủ số lượng slots
    /// </summary>
    private void EnsureSlotsCount(int count)
    {
        // Tạo thêm slots nếu cần
        while (weaponSlots.Count < count)
        {
            WeaponInfoSlot newSlot = CreateSlot();
            weaponSlots.Add(newSlot);
        }
    }

    /// <summary>
    /// Tạo một slot mới
    /// </summary>
    private WeaponInfoSlot CreateSlot()
    {
        GameObject slotObject;

        if (weaponSlotPrefab != null)
        {
            slotObject = Instantiate(weaponSlotPrefab, transform);
        }
        else
        {
            // Tạo slot mặc định nếu không có prefab
            slotObject = new GameObject("WeaponSlot");
            slotObject.transform.SetParent(transform);
            
            RectTransform rectTransform = slotObject.AddComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;

            // Thêm Image làm background
            Image bg = slotObject.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            // Thêm WeaponInfoSlot component
            WeaponInfoSlot slot = slotObject.AddComponent<WeaponInfoSlot>();

            // Tạo UI structure cơ bản
            CreateDefaultSlotUI(slotObject, slot);
            
            return slot;
        }

        WeaponInfoSlot slotComponent = slotObject.GetComponent<WeaponInfoSlot>();
        if (slotComponent == null)
        {
            slotComponent = slotObject.AddComponent<WeaponInfoSlot>();
        }

        return slotComponent;
    }

    /// <summary>
    /// Tạo UI structure mặc định cho slot
    /// </summary>
    private void CreateDefaultSlotUI(GameObject parent, WeaponInfoSlot slot)
    {
        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(parent.transform);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(60, 60);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;

        // Name
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(parent.transform);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.2f, 0.7f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Weapon Name";
        nameText.fontSize = 16;
        nameText.fontStyle = FontStyles.Bold;

        // Cooldown
        GameObject cooldownObj = new GameObject("Cooldown");
        cooldownObj.transform.SetParent(parent.transform);
        RectTransform cooldownRect = cooldownObj.AddComponent<RectTransform>();
        cooldownRect.anchorMin = new Vector2(0.2f, 0.4f);
        cooldownRect.anchorMax = new Vector2(1f, 0.6f);
        cooldownRect.offsetMin = Vector2.zero;
        cooldownRect.offsetMax = Vector2.zero;
        TextMeshProUGUI cooldownText = cooldownObj.AddComponent<TextMeshProUGUI>();
        cooldownText.text = "Tốc độ đánh: 0s";
        cooldownText.fontSize = 12;

        // Damage
        GameObject damageObj = new GameObject("Damage");
        damageObj.transform.SetParent(parent.transform);
        RectTransform damageRect = damageObj.AddComponent<RectTransform>();
        damageRect.anchorMin = new Vector2(0.2f, 0.1f);
        damageRect.anchorMax = new Vector2(1f, 0.3f);
        damageRect.offsetMin = Vector2.zero;
        damageRect.offsetMax = Vector2.zero;
        TextMeshProUGUI damageText = damageObj.AddComponent<TextMeshProUGUI>();
        damageText.text = "Sát thương: 0";
        damageText.fontSize = 12;

        // Assign references (sử dụng reflection)
        var slotType = typeof(WeaponInfoSlot);
        var iconField = slotType.GetField("weaponIconImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameField = slotType.GetField("weaponNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cooldownField = slotType.GetField("cooldownText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var damageField = slotType.GetField("damageText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        iconField?.SetValue(slot, iconImage);
        nameField?.SetValue(slot, nameText);
        cooldownField?.SetValue(slot, cooldownText);
        damageField?.SetValue(slot, damageText);
    }

    /// <summary>
    /// Refresh grid (re-display weapons)
    /// </summary>
    public void Refresh()
    {
        if (weaponsToDisplay != null && weaponsToDisplay.Count > 0)
        {
            DisplayWeapons(weaponsToDisplay);
        }
    }

    /// <summary>
    /// Set số cột trong grid
    /// </summary>
    public void SetColumns(int columns)
    {
        preferredColumns = columns;
        if (gridLayoutGroup != null && columns > 0)
        {
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = columns;
        }
    }

}

