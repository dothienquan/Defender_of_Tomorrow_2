using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Controller để hiển thị UI khi player level up
/// - Active Text component khi level up
/// - Fade out sau một khoảng thời gian
/// </summary>
public class LevelUpUIController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("PlayerLevelSystemLinear để subscribe event OnLevelUp")]
    [SerializeField] private PlayerLevelSystemLinear levelSystem;
    
    [Header("Level Up UI")]
    [Tooltip("Text component hiển thị khi level up (TextMeshProUGUI hoặc Text)")]
    [SerializeField] private GameObject levelUpText;

    [Header("Settings")]
    [Tooltip("Thời gian hiển thị text trước khi fade out (giây)")]
    [SerializeField] private float displayDuration = 2f;
    
    [Tooltip("Thời gian fade out (giây)")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    private TextMeshProUGUI tmpText;
    private Text unityText;
    private Color originalTextColor;
    private Tween currentTween;

    private void Awake()
    {
        // Tìm PlayerLevelSystemLinear nếu chưa được gán
        if (levelSystem == null)
        {
            levelSystem = FindFirstObjectByType<PlayerLevelSystemLinear>();
        }

        // Tìm Text component
        if (levelUpText != null)
        {
            tmpText = levelUpText.GetComponent<TextMeshProUGUI>();
            unityText = levelUpText.GetComponent<Text>();
            
            // Lưu màu gốc
            if (tmpText != null)
            {
                originalTextColor = tmpText.color;
            }
            else if (unityText != null)
            {
                originalTextColor = unityText.color;
            }
        }

        // Setup UI ban đầu (ẩn)
        SetupUI();
    }

    private void OnEnable()
    {
        // Subscribe event khi enable
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp += OnLevelUp;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe event khi disable
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp -= OnLevelUp;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe event
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp -= OnLevelUp;
        }

        // Kill tween nếu còn
        KillCurrentTween();
    }

    /// <summary>
    /// Setup UI ban đầu - ẩn text
    /// </summary>
    private void SetupUI()
    {
        if (levelUpText != null)
        {
            levelUpText.SetActive(false);
        }

        // Đảm bảo text có màu đầy đủ khi active lại
        if (tmpText != null)
        {
            tmpText.color = originalTextColor;
        }
        else if (unityText != null)
        {
            unityText.color = originalTextColor;
        }
    }

    /// <summary>
    /// Xử lý khi player level up
    /// </summary>
    private void OnLevelUp(int newLevel)
    {
        ShowLevelUpUI(newLevel);
    }

    /// <summary>
    /// Hiển thị UI level up - active text và fade out sau displayDuration
    /// </summary>
    private void ShowLevelUpUI(int newLevel)
    {
        if (levelUpText == null)
        {
            Debug.LogWarning("[LevelUpUIController] LevelUpText is null! Cannot show level up UI.");
            return;
        }

        // Dừng animation hiện tại nếu có
        KillCurrentTween();

        // Active text
        levelUpText.SetActive(true);

        // Đảm bảo text có màu đầy đủ
        if (tmpText != null)
        {
            tmpText.color = originalTextColor;
        }
        else if (unityText != null)
        {
            unityText.color = originalTextColor;
        }

        // Tạo sequence: hiển thị -> fade out -> ẩn
        Sequence sequence = DOTween.Sequence();

        // Giữ nguyên độ trong suốt trong thời gian hiển thị
        sequence.AppendInterval(displayDuration);

        // Fade out
        if (tmpText != null)
        {
            sequence.Append(tmpText.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InQuad));
        }
        else if (unityText != null)
        {
            sequence.Append(unityText.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InQuad));
        }

        // Ẩn text sau khi fade out
        sequence.OnComplete(() =>
        {
            if (levelUpText != null)
            {
                levelUpText.SetActive(false);
            }
        });

        currentTween = sequence;
        
        Debug.Log($"[LevelUpUIController] Showing level up UI for level {newLevel}.");
    }

    /// <summary>
    /// Dừng animation hiện tại
    /// </summary>
    private void KillCurrentTween()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
    }
}

