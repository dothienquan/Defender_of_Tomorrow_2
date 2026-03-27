using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// Script để tạo hiệu ứng highlight và bounce cho item khi drop ra world
/// Gắn vào item prefab hoặc thêm vào item khi spawn
/// Hỗ trợ cả SpriteRenderer (world items) và Image (UI items)
/// </summary>
public class ItemDropEffect : MonoBehaviour
{
    [Header("Highlight Settings")]
    [Tooltip("Màu highlight (sáng lên)")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [Tooltip("Thời gian highlight (giây)")]
    [SerializeField] private float highlightDuration = 2f;
    [Tooltip("Cường độ highlight (0-1)")]
    [SerializeField] private float highlightIntensity = 0.5f;

    [Header("Float Settings")]
    [Tooltip("Độ cao float (units)")]
    [SerializeField] private float floatHeight = 0.3f;
    [Tooltip("Thời gian một chu kỳ float lên xuống (giây)")]
    [SerializeField] private float floatDuration = 1f;
    [Tooltip("Ease type cho float")]
    [SerializeField] private Ease floatEase = Ease.InOutSine;

    private SpriteRenderer spriteRenderer;
    private Image image; // Nếu item có Image component (UI)
    private Color originalColor;
    private Vector3 originalPosition;
    private Tween highlightTween;
    private Tween floatTween;
    private bool isActive = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        image = GetComponent<Image>();
        
        // Kiểm tra xem có component nào để render không
        if (spriteRenderer == null && image == null)
        {
            Debug.LogWarning($"[ItemDropEffect] {gameObject.name} does not have SpriteRenderer or Image component! Effect may not work.");
        }
        
        // Lưu màu gốc
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else if (image != null)
        {
            originalColor = image.color;
        }
        else
        {
            originalColor = Color.white;
        }
        
        originalPosition = transform.position;
    }

    private void OnEnable()
    {
        // Tự động chạy hiệu ứng khi enable
        StartEffect();
    }

    private void OnDisable()
    {
        StopEffect();
    }

    private void OnDestroy()
    {
        StopEffect();
    }

    /// <summary>
    /// Bắt đầu hiệu ứng highlight và bounce
    /// </summary>
    public void StartEffect()
    {
        if (isActive) return;
        
        isActive = true;
        originalPosition = transform.position;
        
        // Bắt đầu highlight
        StartHighlight();
        
        // Bắt đầu float (di chuyển lên xuống liên tục)
        StartFloat();
    }

    /// <summary>
    /// Dừng hiệu ứng
    /// </summary>
    public void StopEffect()
    {
        isActive = false;
        
        if (highlightTween != null && highlightTween.IsActive())
        {
            highlightTween.Kill();
        }
        
        if (floatTween != null && floatTween.IsActive())
        {
            floatTween.Kill();
        }
        
        // Reset về màu gốc
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        else if (image != null)
        {
            image.color = originalColor;
        }
        
        // Reset về vị trí gốc
        transform.position = originalPosition;
    }

    /// <summary>
    /// Bắt đầu hiệu ứng highlight
    /// </summary>
    private void StartHighlight()
    {
        if (spriteRenderer == null && image == null) return;
        
        // Tạo sequence highlight: sáng lên -> tối dần
        Sequence highlightSequence = DOTween.Sequence();
        
        // Sáng lên
        highlightSequence.Append(DOTween.To(
            () => GetCurrentColor(),
            color => SetColor(color),
            Color.Lerp(originalColor, highlightColor, highlightIntensity),
            highlightDuration * 0.3f
        ));
        
        // Tối dần về màu gốc
        highlightSequence.Append(DOTween.To(
            () => GetCurrentColor(),
            color => SetColor(color),
            originalColor,
            highlightDuration * 0.7f
        ));
        
        highlightSequence.OnComplete(() =>
        {
            // Reset về màu gốc
            SetColor(originalColor);
            highlightTween = null;
        });
        
        highlightTween = highlightSequence;
    }

    /// <summary>
    /// Bắt đầu hiệu ứng float (di chuyển lên xuống liên tục)
    /// </summary>
    private void StartFloat()
    {
        Vector3 startPos = originalPosition;
        Vector3 upPos = startPos + Vector3.up * floatHeight;
        Vector3 downPos = startPos;
        
        // Tạo loop animation: lên -> xuống -> lên -> xuống... (liên tục)
        Sequence floatSequence = DOTween.Sequence();
        
        // Lên
        floatSequence.Append(transform.DOMoveY(upPos.y, floatDuration * 0.5f)
            .SetEase(floatEase));
        
        // Xuống
        floatSequence.Append(transform.DOMoveY(downPos.y, floatDuration * 0.5f)
            .SetEase(floatEase));
        
        // Lặp lại vô hạn
        floatSequence.SetLoops(-1, LoopType.Restart);
        
        floatTween = floatSequence;
    }

    /// <summary>
    /// Lấy màu hiện tại
    /// </summary>
    private Color GetCurrentColor()
    {
        if (spriteRenderer != null)
        {
            return spriteRenderer.color;
        }
        else if (image != null)
        {
            return image.color;
        }
        return originalColor;
    }

    /// <summary>
    /// Set màu
    /// </summary>
    private void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        else if (image != null)
        {
            image.color = color;
        }
    }
}

