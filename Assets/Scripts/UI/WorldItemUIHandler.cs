using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script để ngăn world items (có UI components) bị snap vào Canvas
/// Gắn vào các item prefab có cả UI và world components
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WorldItemUIHandler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tự động disable UI components khi ở trong world")]
    [SerializeField] private bool autoDisableUIInWorld = true;
    
    [Tooltip("Tự động enable UI components khi được thêm vào inventory")]
    [SerializeField] private bool autoEnableUIInInventory = true;

    private RectTransform rectTransform;
    private CanvasRenderer canvasRenderer;
    private CanvasGroup canvasGroup;
    private Image image;
    private SpriteRenderer spriteRenderer;
    private bool isInInventory = false;
    private Transform originalParent;
    private Vector3 originalWorldPosition;

    private void Awake()
    {
        // Lấy các UI components
        rectTransform = GetComponent<RectTransform>();
        canvasRenderer = GetComponent<CanvasRenderer>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Lưu thông tin ban đầu
        originalParent = transform.parent;
        
        // QUAN TRỌNG: Kiểm tra và xử lý ngay trong Awake() để tránh bị reset về (0,0,0)
        Canvas parentCanvas = transform.parent?.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Nếu bị parent vào Canvas, remove ngay lập tức
            Debug.LogWarning($"[WorldItemUIHandler] {gameObject.name} is parented to Canvas in Awake()! Removing immediately...");
            
            // Lưu world position TRƯỚC KHI remove (nếu có thể)
            Vector3 worldPos = transform.position;
            
            // Remove khỏi Canvas ngay lập tức
            transform.SetParent(null, true);
            
            // Nếu position không phải (0,0,0), lưu lại
            if (Vector3.Distance(worldPos, Vector3.zero) > 0.01f)
            {
                originalWorldPosition = worldPos;
                transform.position = worldPos; // Đảm bảo position được giữ nguyên
            }
            else
            {
                // Position là (0,0,0), đánh dấu chưa lưu
                originalWorldPosition = Vector3.zero;
            }
        }
        else
        {
            // Lưu world position thực tế
            originalWorldPosition = transform.position;
        }
    }

    private void Start()
    {
        if (autoDisableUIInWorld && !isInInventory)
        {
            DisableUIComponents();
            EnsureWorldPosition();
        }
        
        // Đảm bảo position không bị reset về (0, 0, 0) sau khi Start
        if (!isInInventory && transform.position == Vector3.zero && originalWorldPosition != Vector3.zero)
        {
            Debug.LogWarning($"[WorldItemUIHandler] {gameObject.name} position was reset to (0,0,0)! Restoring from originalWorldPosition: {originalWorldPosition}");
            transform.position = originalWorldPosition;
        }
    }

    private void OnEnable()
    {
        // Đảm bảo item không bị snap vào Canvas khi enable
        if (!isInInventory)
        {
            EnsureWorldPosition();
        }
    }

    /// <summary>
    /// Disable các UI components khi item ở trong world
    /// </summary>
    private void DisableUIComponents()
    {
        // CanvasRenderer không có property enabled, nhưng có thể disable GameObject
        // Tuy nhiên, để ngăn snap vào Canvas, chúng ta sẽ disable Image và CanvasGroup
        // và đảm bảo item không bị parent vào Canvas

        // Disable Image nếu có (sử dụng SpriteRenderer thay thế)
        if (image != null)
        {
            image.enabled = false;
        }

        // Disable CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.enabled = false;
        }

        // Đảm bảo SpriteRenderer được enable
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    /// <summary>
    /// Enable các UI components khi item được thêm vào inventory
    /// </summary>
    public void EnableUIComponents()
    {
        isInInventory = true;

        // CanvasRenderer không có property enabled, nhưng sẽ tự động hoạt động khi Image được enable

        // Enable Image
        if (image != null)
        {
            image.enabled = true;
        }

        // Enable CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.enabled = true;
        }

        // Disable SpriteRenderer (không cần trong UI)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Đảm bảo item không bị snap vào Canvas
    /// </summary>
    private void EnsureWorldPosition()
    {
        if (rectTransform == null) return;

        // Kiểm tra xem item có bị parent vào Canvas không
        Transform currentParent = transform.parent;
        if (currentParent != null)
        {
            Canvas parentCanvas = currentParent.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"[WorldItemUIHandler] {gameObject.name} was snapped to Canvas! Removing from Canvas...");
                
                // Lưu world position TRƯỚC KHI remove khỏi Canvas (nếu chưa lưu)
                Vector3 worldPos = transform.position;
                if (originalWorldPosition == Vector3.zero || Vector3.Distance(worldPos, Vector3.zero) < 0.01f)
                {
                    // Nếu position là (0,0,0) hoặc chưa lưu, không reset
                    // Chỉ remove khỏi Canvas, giữ nguyên position hiện tại
                    Debug.LogWarning($"[WorldItemUIHandler] {gameObject.name} position is (0,0,0) or invalid. Keeping current position after removing from Canvas.");
                }
                else
                {
                    // Đã có position hợp lệ, lưu lại
                    originalWorldPosition = worldPos;
                }
                
                // Remove khỏi Canvas và đặt lại parent ban đầu hoặc null
                if (originalParent != null && originalParent.GetComponentInParent<Canvas>() == null)
                {
                    // Chỉ set parent nếu originalParent không phải Canvas
                    transform.SetParent(originalParent, true);
                }
                else
                {
                    // Set parent = null để item không bị ảnh hưởng bởi Canvas
                    transform.SetParent(null, true);
                }

                // Chỉ set position nếu có position hợp lệ (không phải 0,0,0)
                if (originalWorldPosition != Vector3.zero && Vector3.Distance(originalWorldPosition, Vector3.zero) > 0.01f)
                {
                    transform.position = originalWorldPosition;
                }
                else
                {
                    // Nếu position không hợp lệ, giữ nguyên position hiện tại (sau khi remove khỏi Canvas)
                    // Position sẽ được giữ nguyên từ world space
                    Debug.Log($"[WorldItemUIHandler] {gameObject.name} keeping current world position: {transform.position}");
                }
            }
        }
    }

    /// <summary>
    /// Được gọi khi item được thêm vào inventory
    /// </summary>
    public void OnAddedToInventory()
    {
        if (autoEnableUIInInventory)
        {
            EnableUIComponents();
        }
    }

    /// <summary>
    /// Được gọi khi item được remove khỏi inventory
    /// </summary>
    public void OnRemovedFromInventory()
    {
        isInInventory = false;
        if (autoDisableUIInWorld)
        {
            DisableUIComponents();
            EnsureWorldPosition();
        }
    }

    /// <summary>
    /// Force update để đảm bảo item không bị snap
    /// </summary>
    [ContextMenu("Force Fix World Position")]
    public void ForceFixWorldPosition()
    {
        originalWorldPosition = transform.position;
        originalParent = transform.parent;
        DisableUIComponents();
        EnsureWorldPosition();
    }
}
