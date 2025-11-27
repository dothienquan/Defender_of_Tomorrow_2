using UnityEngine;

public class ActiveInventory : Singleton<ActiveInventory>
{
    [Header("Hotbar Reference")]
    [Tooltip("Gán HotbarController vào đây để lấy vũ khí từ hotbar")]
    [SerializeField] private HotbarController hotbarController;

    private int activeSlotIndexNum = 0;
    private PlayerControls playerControls;

    protected override void Awake()
    {
        base.Awake();
        playerControls = new PlayerControls();
        
        // Tự động tìm HotbarController nếu chưa gán
        if (hotbarController == null)
        {
            hotbarController = FindFirstObjectByType<HotbarController>();
        }
    }

    private void Start()
    {
        playerControls.Inventory.Keyboard.performed += ctx => ToggleActiveSlot((int)ctx.ReadValue<float>());
    }

    private void OnEnable() { playerControls.Enable(); }

    public void EquipStartingWeapon() { ToggleActiveHighlight(0); }

    private void ToggleActiveSlot(int numValue) { ToggleActiveHighlight(numValue - 1); }

    private void ToggleActiveHighlight(int indexNum)
    {
        // Nếu có hotbar, dùng số slot của hotbar, nếu không dùng transform.childCount
        int maxSlots = hotbarController != null && hotbarController.hotbarPanel != null 
            ? hotbarController.hotbarPanel.transform.childCount 
            : transform.childCount;
        
        activeSlotIndexNum = Mathf.Clamp(indexNum, 0, maxSlots - 1);

        // Highlight trong ActiveInventory (nếu có)
        if (transform.childCount > 0)
        {
            foreach (Transform inventorySlot in this.transform)
            {
                if (inventorySlot.childCount > 0)
                {
                    inventorySlot.GetChild(0).gameObject.SetActive(false);
                }
            }

            if (activeSlotIndexNum < transform.childCount && transform.GetChild(activeSlotIndexNum).childCount > 0)
            {
                transform.GetChild(activeSlotIndexNum).GetChild(0).gameObject.SetActive(true);
            }
        }

        ChangeActiveWeapon();
    }

    // NEW: public refresh so drag handler can force re-evaluation
    public void RefreshActiveWeapon()
    {
        ChangeActiveWeapon();
    }

    /// <summary>
    /// Set active slot index (được gọi từ HotbarController khi equip vũ khí)
    /// </summary>
    public void SetActiveSlot(int slotIndex)
    {
        activeSlotIndexNum = slotIndex;
        ToggleActiveHighlight(slotIndex);
    }

    private void ChangeActiveWeapon()
    {
        if (ActiveWeapon.Instance.CurrentActiveWeapon != null)
        {
            Destroy(ActiveWeapon.Instance.CurrentActiveWeapon.gameObject);
        }

        WeaponInfo weaponInfo = GetWeaponInfoFromActiveSlot();

        if (weaponInfo == null)
        {
            ActiveWeapon.Instance.WeaponNull();
            return;
        }

        GameObject weaponToSpawn = weaponInfo.weaponPrefab;
        if (weaponToSpawn == null)
        {
            Debug.LogWarning($"[ActiveInventory] Weapon prefab is null for weapon: {weaponInfo.itemName}!");
            ActiveWeapon.Instance.WeaponNull();
            return;
        }

        GameObject newWeapon = Object.Instantiate(weaponToSpawn, ActiveWeapon.Instance.transform);
        
        // Đảm bảo weapon instance sử dụng đúng WeaponInfo từ Item
        // Tìm tất cả các component IWeapon và set WeaponInfo nếu có thể
        MonoBehaviour[] weaponComponents = newWeapon.GetComponents<MonoBehaviour>();
        bool weaponInfoSet = false;
        
        foreach (var component in weaponComponents)
        {
            if (component is IWeapon)
            {
                // Kiểm tra xem component có field WeaponInfo không và set nó
                var weaponType = component.GetType();
                var weaponInfoField = weaponType.GetField("weaponInfo", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public);
                
                if (weaponInfoField != null && weaponInfoField.FieldType == typeof(WeaponInfo))
                {
                    weaponInfoField.SetValue(component, weaponInfo);
                    weaponInfoSet = true;
                    Debug.Log($"[ActiveInventory] Set WeaponInfo for {weaponType.Name}: {weaponInfo.itemName}");
                }
            }
        }
        
        if (!weaponInfoSet)
        {
            Debug.LogWarning($"[ActiveInventory] Could not set WeaponInfo for weapon prefab: {weaponToSpawn.name}. Make sure the weapon script has a 'weaponInfo' field.");
        }
        
        MonoBehaviour weaponComponent = newWeapon.GetComponent<MonoBehaviour>();
        if (weaponComponent == null || !(weaponComponent is IWeapon))
        {
            Debug.LogError($"[ActiveInventory] Weapon prefab {weaponToSpawn.name} does not have a component implementing IWeapon!");
            Destroy(newWeapon);
            ActiveWeapon.Instance.WeaponNull();
            return;
        }
        
        ActiveWeapon.Instance.NewWeapon(weaponComponent);
    }

    /// <summary>
    /// Lấy WeaponInfo từ slot hiện tại (ưu tiên từ hotbar, sau đó từ ActiveInventory)
    /// </summary>
    private WeaponInfo GetWeaponInfoFromActiveSlot()
    {
        // Ưu tiên lấy từ hotbar
        if (hotbarController != null && hotbarController.hotbarPanel != null)
        {
            if (activeSlotIndexNum < hotbarController.hotbarPanel.transform.childCount)
            {
                Transform hotbarSlot = hotbarController.hotbarPanel.transform.GetChild(activeSlotIndexNum);
                Slot slot = hotbarSlot.GetComponent<Slot>();
                
                if (slot != null && slot.currentItem != null)
                {
                    // Kiểm tra xem item có phải là vũ khí không
                    Item item = slot.currentItem.GetComponent<Item>();
                    if (item != null && item.IsWeapon)
                    {
                        // Đảm bảo InventorySlot có WeaponInfo
                        InventorySlot inventorySlot = hotbarSlot.GetComponent<InventorySlot>();
                        if (inventorySlot == null)
                        {
                            inventorySlot = hotbarSlot.gameObject.AddComponent<InventorySlot>();
                        }
                        
                        // Set weapon info nếu chưa có
                        if (inventorySlot.GetWeaponInfo() == null && item.weaponInfo != null)
                        {
                            inventorySlot.SetWeapon(item.weaponInfo);
                        }
                        
                        return inventorySlot.GetWeaponInfo();
                    }
                }
            }
        }

        // Fallback: lấy từ ActiveInventory (cách cũ)
        if (activeSlotIndexNum < transform.childCount)
        {
            Transform childTransform = transform.GetChild(activeSlotIndexNum);
            InventorySlot inventorySlot = childTransform.GetComponentInChildren<InventorySlot>();
            if (inventorySlot != null)
            {
                return inventorySlot.GetWeaponInfo();
            }
        }

        return null;
    }
}
