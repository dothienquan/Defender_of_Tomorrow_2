using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Controller cho shop panel - quản lý việc mua bán vũ khí
/// </summary>
public class ShopController : MonoBehaviour
{
    [Header("Shop UI References")]
    [Tooltip("Panel chứa shop slots (nên là Content của ScrollRect)")]
    [SerializeField] private Transform shopSlotsParent;
    
    [Tooltip("Prefab cho shop slot (có ShopSlot component)")]
    [SerializeField] private GameObject shopSlotPrefab;
    
    [Tooltip("Text hiển thị gold hiện tại")]
    [SerializeField] private TMP_Text goldText;
    
    [Tooltip("Button đóng shop")]
    [SerializeField] private Button closeButton;
    
    [Tooltip("ScrollRect component (optional, nếu có sẽ tự động setup)")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Shop Items")]
    [Tooltip("Danh sách WeaponInfo của các vũ khí có thể mua trong shop")]
    [SerializeField] private List<WeaponInfo> shopWeapons = new List<WeaponInfo>();

    private InventoryController inventoryController;
    private ItemDictionary itemDictionary;

    private void Awake()
    {
        // Tìm InventoryController và ItemDictionary
        inventoryController = FindFirstObjectByType<InventoryController>();
        if (itemDictionary == null && ItemDictionary.Instance != null)
        {
            itemDictionary = ItemDictionary.Instance;
        }
        else if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (inventoryController == null)
        {
            Debug.LogError("[ShopController] InventoryController not found! Shop cannot add items to inventory.");
        }

        if (itemDictionary == null)
        {
            Debug.LogError("[ShopController] ItemDictionary not found! Shop cannot find item prefabs.");
        }

        // Tự động tìm ScrollRect nếu chưa được gán
        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
            if (scrollRect != null)
            {
                Debug.Log("[ShopController] Found ScrollRect automatically.");
            }
        }

        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }
    }

    /// <summary>
    /// Setup ScrollRect để list items không bị tràn
    /// </summary>
    private void SetupScrollRect()
    {
        if (scrollRect == null)
        {
            Debug.LogWarning("[ShopController] ScrollRect is null. Skipping ScrollRect setup.");
            return;
        }

        if (shopSlotsParent == null)
        {
            Debug.LogWarning("[ShopController] ShopSlotsParent is null. Cannot setup ScrollRect.");
            return;
        }

        // Đảm bảo shopSlotsParent là Content của ScrollRect
        RectTransform contentRect = shopSlotsParent.GetComponent<RectTransform>();
        if (contentRect == null)
        {
            Debug.LogError("[ShopController] ShopSlotsParent does not have RectTransform component!");
            return;
        }

        if (scrollRect.content != contentRect)
        {
            scrollRect.content = contentRect;
            Debug.Log("[ShopController] Set shopSlotsParent as ScrollRect content.");
        }

        // QUAN TRỌNG: Đảm bảo Content có Content Size Fitter để tự động tính size
        ContentSizeFitter sizeFitter = shopSlotsParent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = shopSlotsParent.gameObject.AddComponent<ContentSizeFitter>();
            Debug.Log("[ShopController] Added ContentSizeFitter to shopSlotsParent.");
        }
        
        // Setup ContentSizeFitter
        if (sizeFitter != null)
        {
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Tự động tăng height theo children
        }
        
        // Đảm bảo có Layout Group để sắp xếp items
        VerticalLayoutGroup layoutGroup = shopSlotsParent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = shopSlotsParent.gameObject.AddComponent<VerticalLayoutGroup>();
            Debug.Log("[ShopController] Added VerticalLayoutGroup to shopSlotsParent.");
        }
        
        // Setup LayoutGroup properties
        if (layoutGroup != null)
        {
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
        }

        // Setup ScrollRect properties
        scrollRect.horizontal = false; // Chỉ scroll dọc
        scrollRect.vertical = true;    // Scroll dọc
        scrollRect.movementType = ScrollRect.MovementType.Clamped; // Giới hạn scroll không vượt quá bounds
        scrollRect.elasticity = 0f; // Không có bounce effect
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 40f; // Độ nhạy mouse wheel

        // QUAN TRỌNG: Đảm bảo ScrollRect có thể nhận input
        // Kiểm tra xem có GraphicRaycaster không (cần để nhận mouse input)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[ShopController] Added GraphicRaycaster to Canvas for ScrollRect input.");
            }
        }

        // Force update layout sau khi setup
        if (shopSlotsParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(shopSlotsParent.GetComponent<RectTransform>());
        }

        // Clamp scroll position sau khi setup
        ClampScrollPosition();

        Debug.Log("[ShopController] ScrollRect setup complete.");
    }

    private void Start()
    {
        // Setup ScrollRect nếu có
        SetupScrollRect();
        
        // Tạo shop slots
        CreateShopSlots();
        
        // Update gold text
        UpdateGoldText();
        
        // Subscribe to gold changes
        if (EconomyManager.Instance != null)
        {
            // Refresh gold text khi gold thay đổi
            InvokeRepeating(nameof(UpdateGoldText), 0f, 0.5f);
        }
    }

    private void OnEnable()
    {
        UpdateGoldText();
    }

    /// <summary>
    /// Tạo shop slots từ danh sách shopWeapons
    /// </summary>
    private void CreateShopSlots()
    {
        if (shopSlotsParent == null)
        {
            Debug.LogError("[ShopController] ShopSlotsParent is null! Cannot create shop slots.");
            return;
        }

        if (shopSlotPrefab == null)
        {
            Debug.LogError("[ShopController] ShopSlotPrefab is null! Cannot create shop slots.");
            return;
        }

        // Xóa các slot cũ nếu có
        foreach (Transform child in shopSlotsParent)
        {
            Destroy(child.gameObject);
        }

        // Tạo slot cho mỗi weapon
        foreach (WeaponInfo weaponInfo in shopWeapons)
        {
            if (weaponInfo == null)
            {
                Debug.LogWarning("[ShopController] Found null WeaponInfo in shopWeapons list. Skipping...");
                continue;
            }

            // Chỉ tạo slot cho weapons có price > 0
            if (weaponInfo.shopPrice <= 0)
            {
                Debug.Log($"[ShopController] Weapon '{weaponInfo.itemName}' has price 0 or negative. Skipping...");
                continue;
            }

            // Tạo shop slot
            GameObject slotObj = Instantiate(shopSlotPrefab, shopSlotsParent);
            ShopSlot shopSlot = slotObj.GetComponent<ShopSlot>();
            
            if (shopSlot == null)
            {
                Debug.LogError($"[ShopController] ShopSlotPrefab does not have ShopSlot component! Adding component...");
                shopSlot = slotObj.AddComponent<ShopSlot>();
            }

            // Setup shop slot
            shopSlot.SetupSlot(weaponInfo, this);
        }

        Debug.Log($"[ShopController] Created {shopSlotsParent.childCount} shop slots.");

        // QUAN TRỌNG: Force rebuild layout sau khi tạo tất cả slots
        // Để Content Size Fitter tính toán đúng size
        if (shopSlotsParent != null)
        {
            RectTransform contentRect = shopSlotsParent.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                // Force rebuild layout
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                
                // Đảm bảo Content size được tính toán đúng
                ContentSizeFitter sizeFitter = shopSlotsParent.GetComponent<ContentSizeFitter>();
                if (sizeFitter != null)
                {
                    // Trigger layout update
                    Canvas.ForceUpdateCanvases();
                }
            }
        }

        // Reset scroll position về đầu và clamp để tránh khoảng trắng
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f; // 1 = top, 0 = bottom
            ClampScrollPosition();
        }
    }

    /// <summary>
    /// Clamp scroll position để không scroll quá item đầu tiên hoặc cuối cùng
    /// Được gọi sau khi content được rebuild để đảm bảo scroll position hợp lệ
    /// </summary>
    private void ClampScrollPosition()
    {
        if (scrollRect == null || shopSlotsParent == null) return;

        RectTransform contentRect = shopSlotsParent.GetComponent<RectTransform>();
        RectTransform viewportRect = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        
        if (contentRect == null || viewportRect == null) return;

        // Tính toán bounds
        float contentHeight = contentRect.rect.height;
        float viewportHeight = viewportRect.rect.height;

        // Nếu content nhỏ hơn viewport, không cần scroll - giữ ở top
        if (contentHeight <= viewportHeight)
        {
            scrollRect.verticalNormalizedPosition = 1f; // 1 = top
            return;
        }

        // Clamp normalized position (0 = bottom, 1 = top)
        // MovementType.Clamped sẽ tự động clamp, nhưng gọi thêm để đảm bảo
        float normalizedPos = scrollRect.verticalNormalizedPosition;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
    }

    /// <summary>
    /// Mua weapon từ shop
    /// </summary>
    public bool BuyWeapon(WeaponInfo weaponInfo)
    {
        if (weaponInfo == null)
        {
            Debug.LogError("[ShopController] Cannot buy: WeaponInfo is null!");
            return false;
        }

        if (weaponInfo.shopPrice <= 0)
        {
            Debug.LogWarning($"[ShopController] Cannot buy '{weaponInfo.itemName}': Price is {weaponInfo.shopPrice}!");
            return false;
        }

        // Kiểm tra gold
        if (EconomyManager.Instance == null)
        {
            Debug.LogError("[ShopController] EconomyManager.Instance is null! Cannot check gold.");
            return false;
        }

        if (!EconomyManager.Instance.HasEnoughGold(weaponInfo.shopPrice))
        {
            Debug.LogWarning($"[ShopController] Not enough gold to buy '{weaponInfo.itemName}'. Need {weaponInfo.shopPrice}, have {EconomyManager.Instance.CurrentGold}.");
            return false;
        }

        // Tìm item prefab từ ItemDictionary
        if (itemDictionary == null)
        {
            Debug.LogError("[ShopController] ItemDictionary is null! Cannot find item prefab.");
            return false;
        }

        // Tìm item prefab có WeaponInfo này
        GameObject itemPrefab = null;
        
        // Tìm trong ItemDictionary
        for (int i = 0; i < itemDictionary.itemPrefabs.Count; i++)
        {
            GameObject prefab = itemDictionary.itemPrefabs[i];
            if (prefab == null) continue;

            Item item = prefab.GetComponent<Item>();
            if (item != null && item.weaponInfo == weaponInfo)
            {
                itemPrefab = prefab;
                break;
            }
        }

        if (itemPrefab == null)
        {
            Debug.LogError($"[ShopController] Cannot find item prefab for weapon '{weaponInfo.itemName}' in ItemDictionary!");
            return false;
        }

        // Thêm vào inventory
        if (inventoryController == null)
        {
            Debug.LogError("[ShopController] InventoryController is null! Cannot add item to inventory.");
            return false;
        }

        bool itemAdded = inventoryController.AddItem(itemPrefab);
        
        if (itemAdded)
        {
            // Trừ gold
            bool goldSpent = EconomyManager.Instance.SpendGold(weaponInfo.shopPrice);
            
            if (goldSpent)
            {
                UpdateGoldText();
                Debug.Log($"[ShopController] Successfully bought '{weaponInfo.itemName}' for {weaponInfo.shopPrice} gold!");
                return true;
            }
            else
            {
                // Nếu không trừ được gold, remove item khỏi inventory (rollback)
                Debug.LogError($"[ShopController] Failed to spend gold after adding item! This should not happen.");
                // TODO: Remove item from inventory if needed
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[ShopController] Cannot add '{weaponInfo.itemName}' to inventory. Inventory may be full.");
            return false;
        }
    }

    /// <summary>
    /// Cập nhật gold text
    /// </summary>
    private void UpdateGoldText()
    {
        if (goldText == null) return;

        if (EconomyManager.Instance != null)
        {
            goldText.text = EconomyManager.Instance.CurrentGold.ToString("D3");
        }
        else
        {
            goldText.text = "000";
        }
    }

    /// <summary>
    /// Đóng shop panel
    /// </summary>
    public void CloseShop()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Mở shop panel
    /// </summary>
    public void OpenShop()
    {
        gameObject.SetActive(true);
        UpdateGoldText();
    }

    private void OnDestroy()
    {
        // Unsubscribe
        CancelInvoke(nameof(UpdateGoldText));
    }
}
