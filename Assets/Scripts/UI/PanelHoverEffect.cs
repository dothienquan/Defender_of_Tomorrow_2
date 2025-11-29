using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Script chuyên dụng cho panel với hiệu ứng hover
/// Bình thường: trong suốt (transparency cao)
/// Khi hover: giảm độ trong suốt (transparency thấp, opacity cao)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PanelHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Panel References")]
    [SerializeField] private Image panelImage; // Panel background Image
    [SerializeField] private CanvasGroup canvasGroup; // Optional: dùng CanvasGroup thay vì Image color
    
    [Header("Transparency Settings")]
    [SerializeField] [Range(0f, 1f)] private float normalAlpha = 0.3f; // Transparency cao (trong suốt nhiều)
    [SerializeField] [Range(0f, 1f)] private float hoverAlpha = 0.9f; // Transparency thấp (ít trong suốt hơn)
    
    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 0.2f; // Thời gian chuyển đổi
    [SerializeField] private Ease easeType = Ease.OutQuad; // Loại easing
    
    private bool useCanvasGroup = false;
    private Tween currentTween;

    private void Awake()
    {
        // Tự động tìm components nếu chưa gán
        if (panelImage == null)
            panelImage = GetComponent<Image>();
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        // Ưu tiên dùng CanvasGroup nếu có
        useCanvasGroup = canvasGroup != null;
        
        // Set alpha ban đầu
        SetAlpha(normalAlpha);
    }

    private void Start()
    {
        // Đảm bảo alpha ban đầu là normalAlpha
        SetAlpha(normalAlpha);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Khi hover vào: giảm độ trong suốt (tăng opacity)
        TransitionToAlpha(hoverAlpha);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Khi rời chuột: tăng độ trong suốt (giảm opacity)
        TransitionToAlpha(normalAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (useCanvasGroup)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }
        else if (panelImage != null)
        {
            Color color = panelImage.color;
            color.a = alpha;
            panelImage.color = color;
        }
    }

    private void TransitionToAlpha(float targetAlpha)
    {
        // Dừng tween hiện tại nếu có
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
        
        if (useCanvasGroup)
        {
            if (canvasGroup != null)
            {
                currentTween = canvasGroup.DOFade(targetAlpha, transitionDuration)
                    .SetEase(easeType);
            }
        }
        else if (panelImage != null)
        {
            currentTween = panelImage.DOFade(targetAlpha, transitionDuration)
                .SetEase(easeType);
        }
    }

    /// <summary>
    /// Set alpha bình thường (không hover)
    /// </summary>
    public void SetNormalAlpha(float alpha)
    {
        normalAlpha = Mathf.Clamp01(alpha);
        if (!IsPointerOver())
        {
            SetAlpha(normalAlpha);
        }
    }

    /// <summary>
    /// Set alpha khi hover
    /// </summary>
    public void SetHoverAlpha(float alpha)
    {
        hoverAlpha = Mathf.Clamp01(alpha);
        if (IsPointerOver())
        {
            SetAlpha(hoverAlpha);
        }
    }

    /// <summary>
    /// Kiểm tra xem pointer có đang ở trên panel không
    /// </summary>
    private bool IsPointerOver()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    private void OnDestroy()
    {
        // Dừng tween khi destroy để tránh memory leak
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
    }

    private void OnDisable()
    {
        // Dừng tween khi disable
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
    }
}

