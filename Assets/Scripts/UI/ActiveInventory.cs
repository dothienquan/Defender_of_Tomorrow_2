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

    private void OnEnable() 
    { 
        if (playerControls != null)
        {
            playerControls.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Disable();
        }
    }

    private void OnDestroy()
    {
        // Đảm bảo PlayerControls được disable trước khi destroy
        if (playerControls != null)
        {
            playerControls.Disable();
            playerControls.Dispose();
        }
    }

    public void EquipStartingWeapon() 
    { 
        // Kiểm tra xem có hotbar hoặc ActiveInventory slots không trước khi equip
        bool hasSlots = false;
        
        if (hotbarController != null && hotbarController.hotbarPanel != null)
        {
            hasSlots = hotbarController.hotbarPanel.transform.childCount > 0;
        }
        
        if (!hasSlots && transform.childCount > 0)
        {
            hasSlots = true;
        }
        
        if (hasSlots)
        {
            ToggleActiveHighlight(0);
        }
        else
        {
            // Không có slot nào, chỉ set weapon null
            if (ActiveWeapon.Instance != null)
            {
                ActiveWeapon.Instance.WeaponNull();
            }
        }
    }

    private void ToggleActiveSlot(int numValue) { ToggleActiveHighlight(numValue - 1); }

    private void ToggleActiveHighlight(int indexNum)
    {
        // Nếu có hotbar, dùng số slot của hotbar, nếu không dùng transform.childCount
        int maxSlots = 0;
        if (hotbarController != null && hotbarController.hotbarPanel != null)
        {
            maxSlots = hotbarController.hotbarPanel.transform.childCount;
        }
        else if (transform.childCount > 0)
        {
            maxSlots = transform.childCount;
        }
        else
        {
            // Không có slot nào, chỉ cần change weapon (có thể không có weapon)
            ChangeActiveWeapon();
            return;
        }
        
        // Clamp index để đảm bảo trong phạm vi hợp lệ
        activeSlotIndexNum = Mathf.Clamp(indexNum, 0, Mathf.Max(0, maxSlots - 1));

        // Highlight trong ActiveInventory (nếu có và có child)
        if (transform.childCount > 0 && activeSlotIndexNum < transform.childCount)
        {
            // Tắt tất cả highlight
            foreach (Transform inventorySlot in this.transform)
            {
                if (inventorySlot != null && inventorySlot.childCount > 0)
                {
                    Transform highlight = inventorySlot.GetChild(0);
                    if (highlight != null)
                    {
                        highlight.gameObject.SetActive(false);
                    }
                }
            }

            // Bật highlight cho slot active
            Transform activeSlot = transform.GetChild(activeSlotIndexNum);
            if (activeSlot != null && activeSlot.childCount > 0)
            {
                Transform highlight = activeSlot.GetChild(0);
                if (highlight != null)
                {
                    highlight.gameObject.SetActive(true);
                }
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
            int hotbarChildCount = hotbarController.hotbarPanel.transform.childCount;
            if (hotbarChildCount > 0 && activeSlotIndexNum >= 0 && activeSlotIndexNum < hotbarChildCount)
            {
                Transform hotbarSlot = hotbarController.hotbarPanel.transform.GetChild(activeSlotIndexNum);
                if (hotbarSlot != null)
                {
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
        }

        // Fallback: lấy từ ActiveInventory (cách cũ)
        if (transform.childCount > 0 && activeSlotIndexNum >= 0 && activeSlotIndexNum < transform.childCount)
        {
            Transform childTransform = transform.GetChild(activeSlotIndexNum);
            if (childTransform != null)
            {
                InventorySlot inventorySlot = childTransform.GetComponentInChildren<InventorySlot>();
                if (inventorySlot != null)
                {
                    return inventorySlot.GetWeaponInfo();
                }
            }
        }

        return null;
    }
}
