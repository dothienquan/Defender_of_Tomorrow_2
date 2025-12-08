using UnityEngine;

/// <summary>
/// Script cho NPC Shop - mở shop panel khi player nhấn F
/// Gắn vào NPC GameObject có Collider2D (Is Trigger = true)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopNPC : MonoBehaviour
{
    [Header("Shop Settings")]
    [Tooltip("ShopController để mở shop panel")]
    [SerializeField] private ShopController shopController;
    
    [Tooltip("Phím tương tác (mặc định F)")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    [Header("UI Hint (Optional)")]
    [Tooltip("UI hiển thị hint [F] Mở Shop (optional)")]
    [SerializeField] private InteractionUI interactionUI;
    
    [Tooltip("Text hiển thị trên hint (ví dụ: 'Mở Shop' hoặc tên NPC)")]
    [SerializeField] private string interactionText = "Mở Shop";

    [Header("Auto Find Shop")]
    [Tooltip("Tự động tìm ShopController trong scene nếu chưa được gán")]
    [SerializeField] private bool autoFindShop = true;

    private bool playerInRange = false;

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
            Debug.LogError($"[ShopNPC] {gameObject.name} does not have Collider2D component!");
        }

        // Tự động tìm ShopController nếu chưa được gán
        if (shopController == null && autoFindShop)
        {
            shopController = FindFirstObjectByType<ShopController>();
            if (shopController != null)
            {
                Debug.Log($"[ShopNPC] Auto-found ShopController for {gameObject.name}.");
            }
            else
            {
                Debug.LogWarning($"[ShopNPC] ShopController not found! Please assign it manually or ensure ShopController exists in scene.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            // Hiển thị hint UI nếu có
            if (interactionUI != null)
            {
                interactionUI.Show(interactionText);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            // Ẩn hint UI nếu có
            if (interactionUI != null)
            {
                interactionUI.Hide();
            }
        }
    }

    private void Update()
    {
        // Chỉ xử lý khi player trong range
        if (!playerInRange) return;
        
        // Kiểm tra nhấn phím tương tác
        if (Input.GetKeyDown(interactKey))
        {
            OpenShop();
        }
    }

    /// <summary>
    /// Mở shop panel
    /// </summary>
    private void OpenShop()
    {
        if (shopController == null)
        {
            Debug.LogError($"[ShopNPC] Cannot open shop: ShopController is null!");
            return;
        }

        // Ẩn hint UI
        if (interactionUI != null)
        {
            interactionUI.Hide();
        }

        // Mở shop
        shopController.OpenShop();
        Debug.Log($"[ShopNPC] Opened shop from {gameObject.name}.");
    }

    /// <summary>
    /// Đóng shop (có thể gọi từ bên ngoài nếu cần)
    /// </summary>
    public void CloseShop()
    {
        if (shopController != null)
        {
            shopController.CloseShop();
        }
    }
}
