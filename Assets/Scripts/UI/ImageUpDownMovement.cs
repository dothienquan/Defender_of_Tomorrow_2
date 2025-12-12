using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Script để làm Image di chuyển lên xuống liên tục
/// Gắn vào GameObject có Image component hoặc RectTransform
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ImageUpDownMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Khoảng cách di chuyển (pixels)")]
    [SerializeField] private float movementDistance = 50f;

    [Tooltip("Thời gian một chu kỳ lên xuống (giây)")]
    [SerializeField] private float duration = 1f;

    [Tooltip("Delay trước khi bắt đầu animation (giây)")]
    [SerializeField] private float startDelay = 0f;

    [Header("Easing")]
    [Tooltip("Ease type cho animation")]
    [SerializeField] private Ease easeType = Ease.InOutSine;

    [Header("Options")]
    [Tooltip("Nếu true, sẽ tự động bắt đầu khi Start")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Nếu true, sẽ tự động bắt đầu lại khi GameObject được enable")]
    [SerializeField] private bool restartOnEnable = true;

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Tween movementTween;
    private bool isPlaying = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("[ImageUpDownMovement] RectTransform component not found!");
            enabled = false;
            return;
        }

        // Lưu vị trí gốc
        originalPosition = rectTransform.anchoredPosition;
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartMovement();
        }
    }

    private void OnEnable()
    {
        if (restartOnEnable && playOnStart)
        {
            StartMovement();
        }
    }

    private void OnDisable()
    {
        StopMovement();
    }

    private void OnDestroy()
    {
        StopMovement();
    }

    /// <summary>
    /// Bắt đầu animation di chuyển lên xuống
    /// </summary>
    public void StartMovement()
    {
        if (rectTransform == null) return;

        // Dừng animation hiện tại nếu có
        StopMovement();

        // Reset về vị trí gốc
        rectTransform.anchoredPosition = originalPosition;

        isPlaying = true;

        // Tạo sequence: lên -> xuống -> lặp lại
        Sequence sequence = DOTween.Sequence();

        if (startDelay > 0f)
        {
            sequence.AppendInterval(startDelay);
        }

        // Di chuyển lên
        sequence.Append(rectTransform.DOAnchorPosY(
            originalPosition.y + movementDistance,
            duration / 2f)
            .SetEase(easeType));

        // Di chuyển xuống
        sequence.Append(rectTransform.DOAnchorPosY(
            originalPosition.y,
            duration / 2f)
            .SetEase(easeType));

        // Lặp lại vô hạn
        sequence.SetLoops(-1, LoopType.Restart);

        movementTween = sequence;
    }

    /// <summary>
    /// Dừng animation
    /// </summary>
    public void StopMovement()
    {
        if (movementTween != null && movementTween.IsActive())
        {
            movementTween.Kill();
            movementTween = null;
        }

        isPlaying = false;

        // Reset về vị trí gốc
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    /// <summary>
    /// Tạm dừng animation (có thể resume)
    /// </summary>
    public void PauseMovement()
    {
        if (movementTween != null && movementTween.IsActive())
        {
            movementTween.Pause();
        }
    }

    /// <summary>
    /// Tiếp tục animation sau khi pause
    /// </summary>
    public void ResumeMovement()
    {
        if (movementTween != null && movementTween.IsActive())
        {
            movementTween.Play();
        }
    }

    /// <summary>
    /// Kiểm tra animation có đang chạy không
    /// </summary>
    public bool IsPlaying => isPlaying;

    /// <summary>
    /// Set khoảng cách di chuyển mới (sẽ restart animation)
    /// </summary>
    public void SetMovementDistance(float distance)
    {
        movementDistance = distance;
        if (isPlaying)
        {
            StartMovement();
        }
    }

    /// <summary>
    /// Set thời gian chu kỳ mới (sẽ restart animation)
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
        if (isPlaying)
        {
            StartMovement();
        }
    }
}


