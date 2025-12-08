using UnityEngine;

/// <summary>
/// Version của ShopNPC implement IInteractable để tương thích với PlayerInteraction system
/// Sử dụng nếu bạn muốn dùng hệ thống PlayerInteraction thay vì script riêng
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopInteractable : MonoBehaviour, IInteractable
{
    [Header("Shop Settings")]
    [Tooltip("ShopController để mở shop panel")]
    [SerializeField] private ShopController shopController;
    
    [Header("Display")]
    [Tooltip("Tên hiển thị khi player đến gần (ví dụ: 'Shop Keeper' hoặc 'Merchant')")]
    [SerializeField] private string displayName = "Shop Keeper";

    [Header("Auto Find Shop")]
    [Tooltip("Tự động tìm ShopController trong scene nếu chưa được gán")]
    [SerializeField] private bool autoFindShop = true;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;

    private void Awake()
    {
        // Đảm bảo Collider2D là trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError($"[ShopInteractable] {gameObject.name} does not have Collider2D component!");
        }

        // Tự động tìm ShopController nếu chưa được gán
        if (shopController == null && autoFindShop)
        {
            shopController = FindFirstObjectByType<ShopController>();
            if (shopController != null)
            {
                Debug.Log($"[ShopInteractable] Auto-found ShopController for {gameObject.name}.");
            }
            else
            {
                Debug.LogWarning($"[ShopInteractable] ShopController not found! Please assign it manually or ensure ShopController exists in scene.");
            }
        }
    }

    /// <summary>
    /// Được gọi từ PlayerInteraction khi player nhấn F
    /// </summary>
    public void Interact(GameObject interactor)
    {
        if (shopController == null)
        {
            Debug.LogError($"[ShopInteractable] Cannot open shop: ShopController is null!");
            return;
        }

        // Mở shop
        shopController.OpenShop();
        Debug.Log($"[ShopInteractable] Opened shop from {gameObject.name}.");
    }
}
