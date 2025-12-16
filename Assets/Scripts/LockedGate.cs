using UnityEngine;
using DG.Tweening;

/// <summary>
/// Script cho cổng bị khóa, yêu cầu Key để mở
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LockedGate : MonoBehaviour
{
    [Header("Gate Settings")]
    [SerializeField] private int requiredKeyID = 1; // ID của Key cần để mở cổng
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Collider2D gateCollider; // Collider của cổng (sẽ tắt khi mở)

    [Header("UI Panel")]
    [SerializeField] private GameObject lockPanel; // Panel UI hiển thị khi đến gần
    [SerializeField] private LockedGateUI gateUI; // Component UI để hiển thị trạng thái

    [Header("Visual")]
    [SerializeField] private GameObject lockedVisual; // Visual khi cổng bị khóa
    [SerializeField] private GameObject unlockedVisual; // Visual khi cổng đã mở
    
    [Header("On Unlock")]
    [SerializeField] private GameObject objectToActivate; // Object sẽ được set active sau khi unlock thành công
    
    [Header("Move Object After Unlock")]
    [Tooltip("Object sẽ được di chuyển sau khi panel tắt")]
    [SerializeField] private GameObject objectToMove;
    
    [Tooltip("Vị trí ban đầu của object (world space). Nếu để (0,0,0), sẽ tự động lấy từ vị trí hiện tại lần đầu")]
    [SerializeField] private Vector3 initialObjectPosition = Vector3.zero;
    
    [Tooltip("Offset di chuyển (world space)")]
    [SerializeField] private Vector2 moveOffset = Vector2.zero;
    
    [Tooltip("Delay trước khi di chuyển object (giây)")]
    [SerializeField] private float moveDelay = 0f;
    
    [Tooltip("Thời gian di chuyển (giây)")]
    [SerializeField] private float moveDuration = 1f;
    
    [Tooltip("Easing cho di chuyển")]
    [SerializeField] private Ease moveEase = Ease.InOutQuad;

    private bool isUnlocked = false;
    private bool playerInRange = false;
    private InventoryController inventoryController;
    private float lastRefreshTime = 0f;
    private const float refreshInterval = 0.5f; // Refresh mỗi 0.5 giây
    
    // Flag để kiểm tra đã lưu vị trí ban đầu chưa
    private bool hasInitialPosition = false;

    private void Awake()
    {
        // Tự động tìm collider nếu chưa gán
        if (gateCollider == null)
        {
            gateCollider = GetComponent<Collider2D>();
        }

        // Tự động tìm InventoryController
        inventoryController = FindFirstObjectByType<InventoryController>();

        // Ẩn panel ban đầu
        if (lockPanel != null)
        {
            lockPanel.SetActive(false);
        }

        // Setup visual ban đầu
        UpdateVisuals();
        
        // Lưu và reset vị trí ban đầu của object để di chuyển
        if (objectToMove != null)
        {
            // Kill tween cũ nếu có
            objectToMove.transform.DOKill();
            
            Vector3 currentPos = objectToMove.transform.position;
            
            // Nếu initialObjectPosition chưa được set (là Vector3.zero), cần xác định vị trí ban đầu
            if (initialObjectPosition == Vector3.zero)
            {
                if (moveOffset != Vector2.zero)
                {
                    // Tính toán vị trí ban đầu giả định (trừ offset từ vị trí hiện tại)
                    Vector3 calculatedInitialPos = currentPos - (Vector3)moveOffset;
                    
                    // Giả định: nếu object đã di chuyển từ lần chơi trước, 
                    // vị trí hiện tại sẽ là vị trí đích (initialPosition + offset)
                    // Vậy vị trí ban đầu = vị trí hiện tại - offset
                    // Luôn reset về vị trí ban đầu đã tính toán
                    initialObjectPosition = calculatedInitialPos;
                    objectToMove.transform.position = initialObjectPosition;
                    Debug.Log($"[LockedGate] Reset object '{objectToMove.name}' to calculated initial position: {initialObjectPosition}");
                }
                else
                {
                    // Không có offset, vị trí hiện tại chính là vị trí ban đầu
                    initialObjectPosition = currentPos;
                    Debug.Log($"[LockedGate] Saved initial position of '{objectToMove.name}': {initialObjectPosition}");
                }
            }
            else
            {
                // Đã có vị trí ban đầu được lưu (từ Inspector)
                // Luôn reset object về vị trí ban đầu
                if (moveOffset != Vector2.zero)
                {
                    Vector3 expectedTargetPos = initialObjectPosition + (Vector3)moveOffset;
                    float distanceToTarget = Vector3.Distance(currentPos, expectedTargetPos);
                    
                    // Nếu object đang ở vị trí đích hoặc không ở vị trí ban đầu, reset về vị trí ban đầu
                    float distanceToInitial = Vector3.Distance(currentPos, initialObjectPosition);
                    if (distanceToTarget < 0.1f || distanceToInitial > 0.1f)
                    {
                        objectToMove.transform.position = initialObjectPosition;
                        Debug.Log($"[LockedGate] Reset object '{objectToMove.name}' from {currentPos} to initial position: {initialObjectPosition}");
                    }
                }
            }
            
            hasInitialPosition = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag) || isUnlocked) return;

        playerInRange = true;
        ShowPanel();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Refresh UI khi player vẫn trong range (để cập nhật nếu vừa nhặt Key)
        // Chỉ refresh mỗi refreshInterval để tối ưu performance
        if (playerInRange && !isUnlocked && other.CompareTag(playerTag))
        {
            if (Time.time - lastRefreshTime >= refreshInterval)
            {
                RefreshUI();
                lastRefreshTime = Time.time;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;
        HidePanel();
    }

    private void ShowPanel()
    {
        if (lockPanel != null)
        {
            lockPanel.SetActive(true);
            
            // Đảm bảo UI được update sau khi active
            // Force update Canvas để đảm bảo layout được tính toán
            Canvas.ForceUpdateCanvases();
            
            // Đảm bảo EventSystem hoạt động
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogWarning("[LockedGate] EventSystem not found! UI interactions may not work.");
            }
        }

        // Cập nhật UI với trạng thái Key
        if (gateUI != null)
        {
            bool hasKey = CheckHasKey();
            int keyCount = GetKeyCount();
            gateUI.UpdateUI(hasKey, requiredKeyID, 1); // 1 Key cần thiết
        }
    }

    private void HidePanel()
    {
        if (lockPanel != null)
        {
            lockPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Kiểm tra xem player có Key không
    /// </summary>
    private bool CheckHasKey()
    {
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        if (inventoryController == null)
        {
            Debug.LogWarning("[LockedGate] InventoryController not found!");
            return false;
        }

        bool hasKey = inventoryController.HasItem(requiredKeyID);
        Debug.Log($"[LockedGate] Checking for key ID {requiredKeyID}: {(hasKey ? "FOUND" : "NOT FOUND")}");
        
        // Debug: In ra tất cả items trong inventory
        if (!hasKey)
        {
            Debug.LogWarning($"[LockedGate] Key ID {requiredKeyID} not found in inventory. Checking all items...");
            int itemCount = inventoryController.GetItemCount(requiredKeyID);
            Debug.Log($"[LockedGate] Item count for ID {requiredKeyID}: {itemCount}");
        }
        
        return hasKey;
    }
    
    /// <summary>
    /// Public method để lấy requiredKeyID (dùng cho LockedGateUI)
    /// </summary>
    public int GetRequiredKeyID()
    {
        return requiredKeyID;
    }

    /// <summary>
    /// Mở cổng (được gọi từ UI khi nhấn xác nhận)
    /// </summary>
    public void UnlockGate()
    {
        if (isUnlocked) return;

        // Kiểm tra lại Key trước khi mở
        if (!CheckHasKey())
        {
            Debug.LogWarning("[LockedGate] Player does not have required key!");
            if (gateUI != null)
            {
                gateUI.ShowMessage("Bạn chưa có chìa khóa!");
            }
            return;
        }

        // Mở cổng
        isUnlocked = true;

        // Tắt collider
        if (gateCollider != null)
        {
            gateCollider.enabled = false;
        }

        // Cập nhật visual
        UpdateVisuals();

        // Ẩn panel
        HidePanel();

        // Set active object sau khi panel đã tắt
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            Debug.Log($"[LockedGate] Activated object: {objectToActivate.name}");
        }

        // Di chuyển object sau khi panel đã tắt
        MoveObjectAfterUnlock();

        Debug.Log("[LockedGate] Gate unlocked!");
    }
    
    /// <summary>
    /// Di chuyển object sau khi unlock thành công
    /// </summary>
    private void MoveObjectAfterUnlock()
    {
        if (objectToMove == null)
        {
            Debug.LogWarning("[LockedGate] Cannot move object: objectToMove is null! Please assign it in Inspector.");
            return;
        }
        
        if (moveOffset == Vector2.zero)
        {
            Debug.LogWarning("[LockedGate] moveOffset is zero! Object will not move.");
            return;
        }
        
        // Nếu chưa lưu vị trí ban đầu, lưu ngay bây giờ
        if (!hasInitialPosition)
        {
            initialObjectPosition = objectToMove.transform.position;
            hasInitialPosition = true;
        }
        
        // Tính toán vị trí đích dựa trên vị trí ban đầu (không phải vị trí hiện tại)
        Vector3 targetPosition = initialObjectPosition + (Vector3)moveOffset;
        
        // Kiểm tra xem object đã ở vị trí đích chưa (tolerance nhỏ để tránh floating point errors)
        float distanceToTarget = Vector3.Distance(objectToMove.transform.position, targetPosition);
        if (distanceToTarget < 0.01f)
        {
            Debug.Log($"[LockedGate] Object '{objectToMove.name}' is already at target position. Skipping move.");
            return;
        }
        
        Debug.Log($"[LockedGate] Moving object '{objectToMove.name}' from {objectToMove.transform.position} to {targetPosition} (offset: {moveOffset}), delay: {moveDelay}s, duration: {moveDuration}s");
        
        // Kill tween cũ nếu có
        objectToMove.transform.DOKill();
        
        if (moveDelay > 0f)
        {
            // Có delay, dùng sequence
            Sequence moveSequence = DOTween.Sequence();
            moveSequence.AppendInterval(moveDelay);
            moveSequence.Append(objectToMove.transform.DOMove(targetPosition, moveDuration).SetEase(moveEase));
            moveSequence.OnComplete(() =>
            {
                Debug.Log($"[LockedGate] Moved object '{objectToMove.name}' to position: {targetPosition}");
            });
        }
        else
        {
            // Không có delay, di chuyển ngay
            objectToMove.transform.DOMove(targetPosition, moveDuration)
                .SetEase(moveEase)
                .OnComplete(() =>
                {
                    Debug.Log($"[LockedGate] Moved object '{objectToMove.name}' to position: {targetPosition}");
                });
        }
    }

    private void UpdateVisuals()
    {
        if (lockedVisual != null)
        {
            lockedVisual.SetActive(!isUnlocked);
        }

        if (unlockedVisual != null)
        {
            unlockedVisual.SetActive(isUnlocked);
        }
    }

    /// <summary>
    /// Lấy số lượng Key hiện có
    /// </summary>
    private int GetKeyCount()
    {
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        if (inventoryController == null)
        {
            return 0;
        }

        return inventoryController.GetItemCount(requiredKeyID);
    }

    /// <summary>
    /// Refresh UI khi player vẫn trong range (để cập nhật nếu player vừa nhặt Key)
    /// </summary>
    public void RefreshUI()
    {
        if (playerInRange && !isUnlocked && gateUI != null)
        {
            bool hasKey = CheckHasKey();
            gateUI.UpdateUI(hasKey, requiredKeyID, 1); // 1 Key cần thiết
        }
    }
}

