using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// UI component cho LockedGate panel
/// </summary>
public class LockedGateUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText; // Text hiển thị trạng thái
    [SerializeField] private TextMeshProUGUI keyInfoText; // Text hiển thị thông tin Key
    [SerializeField] private Button confirmButton; // Nút xác nhận mở cổng
    [SerializeField] private Button cancelButton; // Nút hủy/đóng panel
    [SerializeField] private Image keyIcon; // Icon của Key (tùy chọn)

    [Header("Messages")]
    [SerializeField] private string confirmButtonText = "Mở khóa";
    [SerializeField] private string cancelButtonText = "Hủy";
    [SerializeField] private string statusFormat = "{0}/{1}"; // Format: "0/1" hoặc "1/1"
    
    [Header("Confirm Animation")]
    [Tooltip("Object sẽ được active và di chuyển trước khi xoay")]
    [SerializeField] private GameObject animatedObject;
    
    [Tooltip("Offset di chuyển của object theo trục Y (local position)")]
    [SerializeField] private float moveOffsetY = 0f;
    
    [Tooltip("Rotation Z mặc định của animated object (độ)")]
    [SerializeField] private float defaultRotationZ = 45f;
    
    [Tooltip("Thời gian di chuyển object (giây)")]
    [SerializeField] private float moveDuration = 0.5f;
    
    [Tooltip("Delay sau khi di chuyển xong trước khi bắt đầu xoay (giây)")]
    [SerializeField] private float rotationDelay = 0.5f;
    
    [Tooltip("Góc xoay (độ) - cả object và button sẽ xoay cùng lúc")]
    [SerializeField] private float rotationAngle = 45f;
    
    [Tooltip("Thời gian xoay (giây)")]
    [SerializeField] private float rotationDuration = 3f;
    
    [Tooltip("Ease type cho di chuyển")]
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    
    [Tooltip("Ease type cho xoay")]
    [SerializeField] private Ease rotationEase = Ease.Linear;

    [Header("VFX Settings")]
    [Tooltip("VFX spawn sau khi animated object di chuyển xong")]
    [SerializeField] private GameObject vfxAfterMove;
    
    [Tooltip("Vị trí spawn VFX sau khi di chuyển (để trống sẽ dùng vị trí animated object)")]
    [SerializeField] private Transform vfxAfterMovePosition;
    
    [Tooltip("VFX spawn sau khi cả 2 object xoay xong")]
    [SerializeField] private GameObject vfxAfterRotation;
    
    [Tooltip("Vị trí spawn VFX sau khi xoay (để trống sẽ dùng vị trí animated object)")]
    [SerializeField] private Transform vfxAfterRotationPosition;
    
    [Tooltip("Parent để spawn VFX (thường là Canvas, để trống sẽ tự động tìm Canvas)")]
    [SerializeField] private Transform vfxParent;
    
    [Tooltip("Delay sau VFX thứ 2 trước khi tắt panel (giây)")]
    [SerializeField] private float delayBeforeClose = 1f;

    private LockedGate lockedGate;
    private ItemDictionary itemDictionary;
    private bool isAnimating = false; // Tránh click nhiều lần
    private Sequence animationSequence; // Lưu sequence để có thể kill nếu cần
    private Vector3 animatedObjectOriginalPosition; // Lưu vị trí gốc của object

    private void Awake()
    {
        // Tự động tìm LockedGate
        lockedGate = GetComponentInParent<LockedGate>();
        if (lockedGate == null)
        {
            lockedGate = FindFirstObjectByType<LockedGate>();
        }

        // Tự động tìm ItemDictionary
        itemDictionary = FindFirstObjectByType<ItemDictionary>();

        // Setup buttons
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            // Đảm bảo button có thể tương tác
            confirmButton.interactable = true;
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
            // Đảm bảo button có thể tương tác
            cancelButton.interactable = true;
        }

        // Lưu vị trí gốc của animated object
        if (animatedObject != null)
        {
            RectTransform rectTransform = animatedObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                animatedObjectOriginalPosition = rectTransform.anchoredPosition;
            }
            else
            {
                animatedObjectOriginalPosition = animatedObject.transform.localPosition;
            }
            
            // Set rotation ban đầu về defaultRotationZ
            animatedObject.transform.localEulerAngles = new Vector3(0f, 0f, defaultRotationZ);
            
            // Ẩn object ban đầu
            animatedObject.SetActive(false);
        }

        // Ẩn panel ban đầu
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Khi panel được enable, đảm bảo buttons có thể tương tác
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }
        if (cancelButton != null)
        {
            cancelButton.interactable = true;
        }
        
        // Reset animation state
        isAnimating = false;
        
        // Force update Canvas để đảm bảo UI được render đúng
        Canvas.ForceUpdateCanvases();
        
        // Đảm bảo EventSystem hoạt động
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null)
        {
            // Clear selected object để tránh conflict
            eventSystem.SetSelectedGameObject(null);
        }
        else
        {
            Debug.LogWarning("[LockedGateUI] EventSystem not found! UI interactions may not work.");
        }
    }

    private void OnDestroy()
    {
        // Kill sequence khi destroy để tránh memory leak
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
        }
    }

    private void OnDisable()
    {
        // Kill sequence khi disable
        if (animationSequence != null && animationSequence.IsActive())
        {
            animationSequence.Kill();
        }
        
        // Reset animated object về trạng thái ban đầu
        ResetAnimatedObject();
        
        // Reset trạng thái
        isAnimating = false;
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }
    }

    /// <summary>
    /// Reset animated object về trạng thái ban đầu
    /// </summary>
    private void ResetAnimatedObject()
    {
        if (animatedObject != null)
        {
            animatedObject.SetActive(false);
            
            RectTransform rectTransform = animatedObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = animatedObjectOriginalPosition;
            }
            else
            {
                animatedObject.transform.localPosition = animatedObjectOriginalPosition;
            }
            
            // Reset rotation về defaultRotationZ
            animatedObject.transform.localEulerAngles = new Vector3(0f, 0f, defaultRotationZ);
        }
        
        // Reset confirm button rotation về 0
        if (confirmButton != null)
        {
            confirmButton.transform.localEulerAngles = Vector3.zero;
        }
    }

    /// <summary>
    /// Cập nhật UI với trạng thái Key
    /// </summary>
    public void UpdateUI(bool hasKey, int keyID, int requiredCount = 1)
    {
        // Cập nhật status text dạng "x/y"
        if (statusText != null)
        {
            int currentCount = GetKeyCount(keyID);
            statusText.text = string.Format(statusFormat, currentCount, requiredCount);
        }

        // Cập nhật key info
        if (keyInfoText != null)
        {
            string keyName = GetKeyName(keyID);
            keyInfoText.text = $"Yêu cầu";
        }

        // Enable/disable confirm button dựa trên việc có Key
        // LƯU Ý: Luôn cho phép button interactable để có thể click và hiển thị message nếu không có key
        // Logic kiểm tra key sẽ được xử lý trong OnConfirmClicked
        if (confirmButton != null)
        {
            // Luôn cho phép button interactable để có thể click
            confirmButton.interactable = true;
            
            // Cập nhật text button
            TextMeshProUGUI buttonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = confirmButtonText;
            }
        }
        
        // Force update Canvas sau khi update UI
        Canvas.ForceUpdateCanvases();

        // Cập nhật key icon nếu có
        if (keyIcon != null && hasKey)
        {
            Sprite keySprite = GetKeyIcon(keyID);
            if (keySprite != null)
            {
                keyIcon.sprite = keySprite;
                keyIcon.enabled = true;
            }
            else
            {
                keyIcon.enabled = false;
            }
        }
    }

    private void OnConfirmClicked()
    {
        // Tránh click nhiều lần khi đang animation
        if (isAnimating || confirmButton == null)
        {
            return;
        }

        if (lockedGate == null)
        {
            Debug.LogWarning("[LockedGateUI] LockedGate not found!");
            return;
        }

        // Kiểm tra có chìa khóa trước khi bắt đầu animation
        bool hasKey = CheckHasKey();
        if (!hasKey)
        {
            // Không có chìa khóa, hiển thị message và cho phép bấm lại
            ShowMessage("Bạn chưa có chìa khóa!");
            Debug.LogWarning("[LockedGateUI] Player does not have required key!");
            return;
        }

        // Có chìa khóa, bắt đầu animation
        // Disable button để tránh click nhiều lần
        confirmButton.interactable = false;
        isAnimating = true;

        // Kiểm tra animated object
        if (animatedObject == null)
        {
            Debug.LogWarning("[LockedGateUI] AnimatedObject chưa được gán! Sẽ chỉ xoay button.");
            // Fallback: chỉ xoay button như cũ
            PlayButtonRotationOnly();
            return;
        }

        // Lấy RectTransform hoặc Transform của animated object (phải khai báo trước khi dùng)
        RectTransform animatedRectTransform = animatedObject.GetComponent<RectTransform>();
        Transform animatedTransform = animatedObject.transform;

        // Lưu rotation ban đầu
        Vector3 originalButtonRotation = Vector3.zero; // Button luôn bắt đầu từ 0
        Vector3 originalObjectRotation = new Vector3(0f, 0f, defaultRotationZ); // Object bắt đầu ở 45 độ
        Vector3 targetObjectRotation = originalObjectRotation + new Vector3(0f, 0f, rotationAngle); // Object xoay từ 45° → 45° + rotationAngle
        Vector3 targetButtonRotation = originalButtonRotation + new Vector3(0f, 0f, rotationAngle); // Button xoay từ 0° → rotationAngle
        
        // Reset rotation: button về 0, object về defaultRotationZ (45 độ)
        confirmButton.transform.localEulerAngles = originalButtonRotation;
        animatedTransform.localEulerAngles = originalObjectRotation;

        // Lưu vị trí ban đầu
        Vector2 startPosition2D = Vector2.zero;
        Vector2 targetPosition2D = Vector2.zero;
        Vector3 startPosition3D = Vector3.zero;
        Vector3 targetPosition3D = Vector3.zero;
        
        if (animatedRectTransform != null)
        {
            startPosition2D = animatedRectTransform.anchoredPosition;
            targetPosition2D = startPosition2D + new Vector2(0f, moveOffsetY);
        }
        else
        {
            startPosition3D = animatedTransform.localPosition;
            targetPosition3D = startPosition3D + new Vector3(0f, moveOffsetY, 0f);
        }

        // Tạo sequence animation
        animationSequence = DOTween.Sequence();

        // Bước 1: Active object và đặt về vị trí ban đầu với rotation Z = 45 độ
        animatedObject.SetActive(true);
        if (animatedRectTransform != null)
        {
            animatedRectTransform.anchoredPosition = startPosition2D;
        }
        else
        {
            animatedTransform.localPosition = startPosition3D;
        }
        // Set rotation Z = 45 độ ngay khi active
        animatedTransform.localEulerAngles = originalObjectRotation;

        // Bước 2: Di chuyển object đến vị trí offset
        Tween moveTween;
        if (animatedRectTransform != null)
        {
            moveTween = animatedRectTransform.DOAnchorPos(targetPosition2D, moveDuration)
                .SetEase(moveEase);
        }
        else
        {
            moveTween = animatedTransform.DOLocalMove(targetPosition3D, moveDuration)
                .SetEase(moveEase);
        }
        animationSequence.Append(moveTween);

        // Bước 2.1: Spawn VFX sau khi di chuyển xong
        animationSequence.AppendCallback(() =>
        {
            // Dùng RectTransform position nếu có, nếu không dùng Transform position
            Vector3 fallbackPos;
            RectTransform fallbackRectTransform = null;
            
            if (animatedRectTransform != null)
            {
                fallbackPos = animatedRectTransform.position;
                fallbackRectTransform = animatedRectTransform;
            }
            else
            {
                fallbackPos = animatedTransform.position;
            }
            
            SpawnVFX(vfxAfterMove, vfxAfterMovePosition, fallbackPos, fallbackRectTransform);
        });

        // Bước 2.5: Delay sau khi di chuyển xong
        animationSequence.AppendInterval(rotationDelay);

        // Bước 3: Xoay cả object và button cùng lúc (song song)
        Tween objectRotateTween = animatedTransform.DORotate(targetObjectRotation, rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase);
        
        Tween buttonRotateTween = confirmButton.transform.DORotate(targetButtonRotation, rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase);
        
        // Join cả 2 rotation vào sequence (chạy song song sau delay)
        animationSequence.Join(objectRotateTween);
        animationSequence.Join(buttonRotateTween);

        // Bước 4: Spawn VFX sau khi xoay xong
        animationSequence.AppendCallback(() =>
        {
            // Dùng RectTransform position nếu có, nếu không dùng Transform position
            Vector3 fallbackPos;
            RectTransform fallbackRectTransform = null;
            
            if (animatedRectTransform != null)
            {
                fallbackPos = animatedRectTransform.position;
                fallbackRectTransform = animatedRectTransform;
            }
            else
            {
                fallbackPos = animatedTransform.position;
            }
            
            SpawnVFX(vfxAfterRotation, vfxAfterRotationPosition, fallbackPos, fallbackRectTransform);
        });

        // Bước 5: Delay trước khi tắt panel
        animationSequence.AppendInterval(delayBeforeClose);

        // Bước 6: Mở khóa, tiêu thụ Key và tắt panel
        animationSequence.OnComplete(() =>
        {
            // Tiêu thụ Key từ hotbar
            ConsumeKey();

            // Mở khóa
            if (lockedGate != null)
            {
                lockedGate.UnlockGate();
            }
            
            // Reset về trạng thái ban đầu
            ResetAnimatedObject();
            
            // Tắt panel
            gameObject.SetActive(false);
            
            // Reset trạng thái
            isAnimating = false;
        });
    }

    /// <summary>
    /// Spawn VFX tại vị trí chỉ định
    /// </summary>
    private void SpawnVFX(GameObject vfxPrefab, Transform positionTransform, Vector3 fallbackPosition, RectTransform fallbackRectTransform = null)
    {
        if (vfxPrefab == null) return;

        // Xác định vị trí spawn
        Vector3 spawnPosition;
        Quaternion spawnRotation = Quaternion.identity;
        RectTransform spawnRectTransform = null;
        
        if (positionTransform != null)
        {
            // Dùng vị trí từ Transform được chỉ định
            spawnPosition = positionTransform.position;
            spawnRotation = positionTransform.rotation;
            spawnRectTransform = positionTransform.GetComponent<RectTransform>();
        }
        else
        {
            // Dùng fallback position (vị trí animated object)
            spawnPosition = fallbackPosition;
            spawnRectTransform = fallbackRectTransform;
        }

        // Xác định parent cho VFX
        Transform parent = vfxParent;
        Canvas parentCanvas = null;
        if (parent == null)
        {
            // Tự động tìm Canvas
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parent = parentCanvas.transform;
            }
            else
            {
                // Nếu không tìm thấy Canvas, thử tìm trong scene
                parentCanvas = FindFirstObjectByType<Canvas>();
                if (parentCanvas != null)
                {
                    parent = parentCanvas.transform;
                }
            }
        }
        else
        {
            // Lấy Canvas từ parent đã được gán
            parentCanvas = parent.GetComponent<Canvas>();
        }

        // Spawn VFX
        GameObject vfxInstance;
        
        if (parent != null && parentCanvas != null)
        {
            // Spawn như child của Canvas
            vfxInstance = Instantiate(vfxPrefab, parent);
            
            // Đảm bảo VFX có RectTransform (nếu là UI element)
            RectTransform vfxRectTransform = vfxInstance.GetComponent<RectTransform>();
            if (vfxRectTransform == null)
            {
                // Thêm RectTransform nếu chưa có
                vfxRectTransform = vfxInstance.AddComponent<RectTransform>();
            }
            
            // Set vị trí trên Canvas
            RectTransform parentRectTransform = parent.GetComponent<RectTransform>();
            if (parentRectTransform != null)
            {
                // Nếu có RectTransform từ positionTransform hoặc fallback, dùng anchoredPosition trực tiếp
                if (spawnRectTransform != null)
                {
                    // Copy anchoredPosition từ RectTransform nguồn
                    vfxRectTransform.anchoredPosition = spawnRectTransform.anchoredPosition;
                    Debug.Log($"[LockedGateUI] VFX '{vfxPrefab.name}' spawned at anchored position: {spawnRectTransform.anchoredPosition}");
                }
                else
                {
                    // Convert world/screen position sang local anchored position
                    Camera uiCamera = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
                    
                    if (uiCamera != null)
                    {
                        // Convert world position sang screen point
                        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, spawnPosition);
                        
                        // Convert screen point sang local point trong Canvas
                        Vector2 localPoint;
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            parentRectTransform, screenPoint, uiCamera, out localPoint))
                        {
                            vfxRectTransform.anchoredPosition = localPoint;
                            Debug.Log($"[LockedGateUI] VFX '{vfxPrefab.name}' spawned at converted position: {localPoint}");
                        }
                        else
                        {
                            // Fallback: dùng vị trí trực tiếp nếu convert không thành công
                            vfxRectTransform.anchoredPosition = Vector2.zero;
                            Debug.LogWarning($"[LockedGateUI] Failed to convert position for VFX '{vfxPrefab.name}'. Using zero position.");
                        }
                    }
                    else
                    {
                        // Nếu không có camera, dùng vị trí trực tiếp
                        vfxRectTransform.anchoredPosition = Vector2.zero;
                        Debug.LogWarning($"[LockedGateUI] No camera found for Canvas. VFX '{vfxPrefab.name}' spawned at zero position.");
                    }
                }
            }
            
            // Set rotation
            vfxRectTransform.localRotation = spawnRotation;
            
            // Đảm bảo VFX hiển thị trên cùng (set as last sibling)
            vfxInstance.transform.SetAsLastSibling();
            
            // Set Canvas sorting order cao hơn nếu cần
            Canvas vfxCanvas = vfxInstance.GetComponent<Canvas>();
            if (vfxCanvas == null)
            {
                vfxCanvas = vfxInstance.AddComponent<Canvas>();
            }
            vfxCanvas.overrideSorting = true;
            vfxCanvas.sortingOrder = parentCanvas.sortingOrder + 100; // Cao hơn Canvas gốc
            
            // Đảm bảo có GraphicRaycaster nếu cần
            if (vfxInstance.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                vfxInstance.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }
        else if (parent != null)
        {
            // Parent không phải Canvas, spawn như child bình thường
            vfxInstance = Instantiate(vfxPrefab, parent);
            vfxInstance.transform.position = spawnPosition;
            vfxInstance.transform.rotation = spawnRotation;
        }
        else
        {
            // Spawn ở world space
            vfxInstance = Instantiate(vfxPrefab, spawnPosition, spawnRotation);
        }

        // Debug log để biết vị trí spawn
        Debug.Log($"[LockedGateUI] Spawned VFX '{vfxPrefab.name}' at position: {spawnPosition}, Parent: {(parent != null ? parent.name : "World")}");
        
        // Nếu VFX có ParticleSystem, tự động destroy sau khi hoàn thành
        ParticleSystem particles = vfxInstance.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            // Nếu VFX không loop, tự động destroy sau duration
            if (!particles.main.loop)
            {
                float duration = particles.main.duration + particles.main.startLifetime.constantMax;
                Destroy(vfxInstance, duration);
            }
        }
        else
        {
            // Nếu không có ParticleSystem, destroy sau 5 giây (fallback)
            Destroy(vfxInstance, 5f);
        }
    }

    /// <summary>
    /// Fallback: Chỉ xoay button (nếu không có animated object)
    /// </summary>
    private void PlayButtonRotationOnly()
    {
        Vector3 originalRotation = confirmButton.transform.localEulerAngles;
        Vector3 targetRotation = originalRotation + new Vector3(0f, 0f, rotationAngle);

        animationSequence = DOTween.Sequence();
        animationSequence.Append(confirmButton.transform.DORotate(targetRotation, rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase))
            .OnComplete(() =>
            {
                if (lockedGate != null)
                {
                    lockedGate.UnlockGate();
                }
                
                confirmButton.transform.localEulerAngles = originalRotation;
                isAnimating = false;
            });
    }

    /// <summary>
    /// Kiểm tra xem player có chìa khóa không (kiểm tra trong hotbar)
    /// </summary>
    private bool CheckHasKey()
    {
        if (lockedGate == null)
        {
            Debug.LogWarning("[LockedGateUI] LockedGate is null!");
            return false;
        }

        HotbarController hotbarController = FindFirstObjectByType<HotbarController>();
        if (hotbarController == null)
        {
            Debug.LogWarning("[LockedGateUI] HotbarController not found!");
            return false;
        }

        // Sử dụng public method GetRequiredKeyID() từ LockedGate
        int requiredKeyID = lockedGate.GetRequiredKeyID();
        bool hasKey = hotbarController.HasItem(requiredKeyID);
        
        Debug.Log($"[LockedGateUI] Checking for key ID {requiredKeyID} in hotbar: {(hasKey ? "FOUND" : "NOT FOUND")}");
        
        // Debug: In ra số lượng key trong hotbar nếu không tìm thấy
        if (!hasKey)
        {
            int itemCount = hotbarController.GetItemCount(requiredKeyID);
            Debug.LogWarning($"[LockedGateUI] Key ID {requiredKeyID} not found in hotbar. Item count: {itemCount}");
        }
        
        return hasKey;
    }

    private void OnCancelClicked()
    {
        // Ẩn panel
        if (lockedGate != null)
        {
            // LockedGate sẽ tự ẩn khi player rời khỏi trigger
            // Hoặc có thể gọi trực tiếp
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Hiển thị message tạm thời
    /// </summary>
    public void ShowMessage(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private string GetKeyName(int keyID)
    {
        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (itemDictionary != null)
        {
            GameObject keyPrefab = itemDictionary.GetItemPrefab(keyID);
            if (keyPrefab != null)
            {
                Item item = keyPrefab.GetComponent<Item>();
                if (item != null && !string.IsNullOrEmpty(item.Name))
                {
                    return item.Name;
                }
            }
        }

        return "Key";
    }

    private Sprite GetKeyIcon(int keyID)
    {
        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (itemDictionary != null)
        {
            GameObject keyPrefab = itemDictionary.GetItemPrefab(keyID);
            if (keyPrefab != null)
            {
                Item item = keyPrefab.GetComponent<Item>();
                if (item != null)
                {
                    // Thử lấy sprite từ Image hoặc SpriteRenderer
                    UnityEngine.UI.Image image = keyPrefab.GetComponent<UnityEngine.UI.Image>();
                    if (image != null && image.sprite != null)
                    {
                        return image.sprite;
                    }

                    SpriteRenderer spriteRenderer = keyPrefab.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        return spriteRenderer.sprite;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Lấy số lượng Key hiện có trong hotbar
    /// </summary>
    private int GetKeyCount(int keyID)
    {
        HotbarController hotbarController = FindFirstObjectByType<HotbarController>();
        if (hotbarController != null)
        {
            return hotbarController.GetItemCount(keyID);
        }
        return 0;
    }

    /// <summary>
    /// Tiêu thụ Key từ hotbar (xóa Key sau khi sử dụng)
    /// </summary>
    private void ConsumeKey()
    {
        if (lockedGate == null)
        {
            Debug.LogWarning("[LockedGateUI] Cannot consume key: LockedGate is null!");
            return;
        }

        HotbarController hotbarController = FindFirstObjectByType<HotbarController>();
        if (hotbarController == null)
        {
            Debug.LogWarning("[LockedGateUI] Cannot consume key: HotbarController not found!");
            return;
        }

        int requiredKeyID = lockedGate.GetRequiredKeyID();
        bool removed = hotbarController.RemoveItem(requiredKeyID);
        
        if (removed)
        {
            Debug.Log($"[LockedGateUI] Successfully consumed key ID {requiredKeyID} from hotbar.");
        }
        else
        {
            Debug.LogWarning($"[LockedGateUI] Failed to consume key ID {requiredKeyID} from hotbar.");
        }
    }
}

