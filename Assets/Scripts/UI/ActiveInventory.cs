using UnityEngine;

public class ActiveInventory : Singleton<ActiveInventory>
{
    private int activeSlotIndexNum = 0;
    private PlayerControls playerControls;

    protected override void Awake()
    {
        base.Awake();
        playerControls = new PlayerControls();
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
        activeSlotIndexNum = Mathf.Clamp(indexNum, 0, transform.childCount - 1);

        foreach (Transform inventorySlot in this.transform)
        {
            inventorySlot.GetChild(0).gameObject.SetActive(false);
        }

        this.transform.GetChild(activeSlotIndexNum).GetChild(0).gameObject.SetActive(true);
        ChangeActiveWeapon();
    }

    // NEW: public refresh so drag handler can force re-evaluation
    public void RefreshActiveWeapon()
    {
        ChangeActiveWeapon();
    }

    private void ChangeActiveWeapon()
    {
        if (ActiveWeapon.Instance.CurrentActiveWeapon != null)
        {
            Destroy(ActiveWeapon.Instance.CurrentActiveWeapon.gameObject);
        }

        Transform childTransform = transform.GetChild(activeSlotIndexNum);
        InventorySlot inventorySlot = childTransform.GetComponentInChildren<InventorySlot>();
        if (inventorySlot == null)
        {
            ActiveWeapon.Instance.WeaponNull();
            return;
        }

        WeaponInfo weaponInfo = inventorySlot.GetWeaponInfo();
        if (weaponInfo == null)
        {
            ActiveWeapon.Instance.WeaponNull();
            return;
        }

        GameObject weaponToSpawn = weaponInfo.weaponPrefab;
        GameObject newWeapon = Object.Instantiate(weaponToSpawn, ActiveWeapon.Instance.transform);
        ActiveWeapon.Instance.NewWeapon(newWeapon.GetComponent<MonoBehaviour>());
    }
}
