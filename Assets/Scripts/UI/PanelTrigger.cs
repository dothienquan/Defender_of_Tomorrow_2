using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Script để trigger active một panel và có button để tắt panel đó
/// Hỗ trợ chỉ kích hoạt duy nhất một lần và reset khi xóa data game
/// Bao gồm hiệu ứng animation mượt mà khi active/inactive
/// </summary>
public class PanelTrigger : MonoBehaviour
{
    [Header("Panel Settings")]
    [Tooltip("Panel cần được trigger active")]
    [SerializeField] private GameObject targetPanel;

    [Header("Trigger Settings")]
    [Tooltip("Tự động kích hoạt panel khi Start()")]
    [SerializeField] private bool triggerOnStart = false;

    [Tooltip("Chỉ kích hoạt duy nhất một lần (lưu vào PlayerPrefs)")]
    [SerializeField] private bool triggerOnlyOnce = false;

    [Tooltip("Unique ID cho trigger này (dùng để lưu trạng thái đã hiển thị)")]
    [SerializeField] private string triggerID = "";

    [Header("Animation Settings")]
    [Tooltip("Bật animation khi active/inactive panel")]
    [SerializeField] private bool enableAnimation = true;

    [Tooltip("Loại animation: Fade, Scale, hoặc Both")]
    [SerializeField] private AnimationType animationType = AnimationType.Both;

    [Tooltip("Thời gian animation (giây)")]
    [SerializeField] private float animationDuration = 0.3f;

    [Tooltip("Ease type cho animation")]
    [SerializeField] private Ease easeType = Ease.OutBack;

    [Tooltip("Start scale khi scale in (0 = từ nhỏ, 1 = từ bình thường)")]
    [SerializeField] private float startScale = 0f;

    [Header("Close Button")]
    [Tooltip("Button để đóng panel (tự động setup nếu để trống)")]
    [SerializeField] private Button closeButton;

    [Tooltip("Tự động tìm button trong panel nếu closeButton để trống")]
    [SerializeField] private bool autoFindCloseButton = true;

    [Tooltip("Tên button để tự động tìm (mặc định: 'CloseButton' hoặc 'Close')")]
    [SerializeField] private string[] closeButtonNames = { "CloseButton", "Close", "BtnClose" };

    private string triggerKey;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Tween currentTween;
    private Vector3 originalScale;

    public enum AnimationType
    {
        Fade,
        Scale,
        Both
    }

    private void Awake()
    {
        // Tạo unique key cho trigger
        if (triggerOnlyOnce)
        {
            string id = !string.IsNullOrEmpty(triggerID) ? triggerID : gameObject.name;
            triggerKey = $"PanelTrigger_{id}";
        }

        // Setup animation components
        if (targetPanel != null && enableAnimation)
        {
            SetupAnimationComponents();
        }
    }

    /// <summary>
    /// Setup các component cần thiết cho animation
    /// </summary>
    private void SetupAnimationComponents()
    {
        // Tìm hoặc tạo CanvasGroup
        canvasGroup = targetPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null && (animationType == AnimationType.Fade || animationType == AnimationType.Both))
        {
            canvasGroup = targetPanel.AddComponent<CanvasGroup>();
        }

        // Lấy RectTransform
        panelRect = targetPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            originalScale = panelRect.localScale;
        }
    }

    private void Start()
    {
        // Setup close button
        SetupCloseButton();

        // Trigger panel nếu được bật
        if (triggerOnStart)
        {
            TriggerPanel();
        }
    }

    /// <summary>
    /// Setup close button (tự động tìm hoặc dùng button đã gán)
    /// </summary>
    private void SetupCloseButton()
    {
        // Nếu đã có closeButton được gán, dùng nó
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
            return;
        }

        // Nếu autoFindCloseButton được bật, tìm button trong panel
        if (autoFindCloseButton && targetPanel != null)
        {
            // Tìm button theo các tên có thể
            foreach (string buttonName in closeButtonNames)
            {
                Transform buttonTransform = targetPanel.transform.Find(buttonName);
                if (buttonTransform == null)
                {
                    // Thử tìm trong toàn bộ hierarchy của panel
                    Button[] buttons = targetPanel.GetComponentsInChildren<Button>(true);
                    foreach (Button btn in buttons)
                    {
                        if (btn.name.Contains(buttonName) || btn.name.Contains("Close"))
                        {
                            closeButton = btn;
                            closeButton.onClick.RemoveAllListeners();
                            closeButton.onClick.AddListener(ClosePanel);
                            return;
                        }
                    }
                }
                else
                {
                    closeButton = buttonTransform.GetComponent<Button>();
                    if (closeButton != null)
                    {
                        closeButton.onClick.RemoveAllListeners();
                        closeButton.onClick.AddListener(ClosePanel);
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Trigger panel (kích hoạt panel)
    /// </summary>
    public void TriggerPanel()
    {
        if (targetPanel == null)
        {
            Debug.LogWarning($"[PanelTrigger] {gameObject.name}: targetPanel chưa được gán!");
            return;
        }

        // Kiểm tra xem đã trigger chưa (nếu triggerOnlyOnce = true)
        if (triggerOnlyOnce)
        {
            bool hasTriggered = PlayerPrefs.GetInt(triggerKey, 0) == 1;
            if (hasTriggered)
            {
                Debug.Log($"[PanelTrigger] {gameObject.name}: Panel đã được trigger trước đó. Bỏ qua...");
                return;
            }
        }

        // Setup animation components nếu chưa có
        if (enableAnimation && canvasGroup == null && panelRect == null)
        {
            SetupAnimationComponents();
        }

        // Kích hoạt panel
        targetPanel.SetActive(true);

        // Chạy animation in
        if (enableAnimation)
        {
            AnimateIn();
        }

        // Đánh dấu đã trigger (nếu triggerOnlyOnce = true)
        if (triggerOnlyOnce)
        {
            PlayerPrefs.SetInt(triggerKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"[PanelTrigger] {gameObject.name}: Panel đã được trigger và đánh dấu là đã hiển thị.");
        }
    }

    /// <summary>
    /// Đóng panel
    /// </summary>
    public void ClosePanel()
    {
        if (targetPanel == null) return;

        // Chạy animation out trước khi đóng
        if (enableAnimation)
        {
            AnimateOut(() => targetPanel.SetActive(false));
        }
        else
        {
            targetPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Animation khi panel được mở (fade in / scale in)
    /// </summary>
    private void AnimateIn()
    {
        // Kill animation hiện tại nếu có
        KillCurrentTween();

        Sequence sequence = DOTween.Sequence();

        // Setup trạng thái ban đầu
        if (animationType == AnimationType.Fade || animationType == AnimationType.Both)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                sequence.Join(canvasGroup.DOFade(1f, animationDuration).SetEase(easeType));
            }
        }

        if (animationType == AnimationType.Scale || animationType == AnimationType.Both)
        {
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.one * startScale;
                sequence.Join(panelRect.DOScale(originalScale, animationDuration).SetEase(easeType));
            }
        }

        currentTween = sequence;
    }

    /// <summary>
    /// Animation khi panel được đóng (fade out / scale out)
    /// </summary>
    private void AnimateOut(System.Action onComplete = null)
    {
        // Kill animation hiện tại nếu có
        KillCurrentTween();

        Sequence sequence = DOTween.Sequence();

        // Fade out
        if (animationType == AnimationType.Fade || animationType == AnimationType.Both)
        {
            if (canvasGroup != null)
            {
                sequence.Join(canvasGroup.DOFade(0f, animationDuration).SetEase(easeType));
            }
        }

        // Scale out
        if (animationType == AnimationType.Scale || animationType == AnimationType.Both)
        {
            if (panelRect != null)
            {
                sequence.Join(panelRect.DOScale(startScale, animationDuration).SetEase(easeType));
            }
        }

        // Callback khi hoàn thành
        if (onComplete != null)
        {
            sequence.OnComplete(() => onComplete());
        }

        currentTween = sequence;
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

    /// <summary>
    /// Reset trạng thái trigger (dùng khi New Game hoặc xóa data)
    /// </summary>
    public void ResetTriggerState()
    {
        if (triggerOnlyOnce && !string.IsNullOrEmpty(triggerKey))
        {
            PlayerPrefs.DeleteKey(triggerKey);
            PlayerPrefs.Save();
            Debug.Log($"[PanelTrigger] {gameObject.name}: Đã reset trigger state.");
        }
    }

    /// <summary>
    /// Kiểm tra xem panel đã được trigger chưa
    /// </summary>
    public bool HasTriggered()
    {
        if (!triggerOnlyOnce) return false;
        return PlayerPrefs.GetInt(triggerKey, 0) == 1;
    }

    // Trigger bằng Unity Event (cho Inspector)
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerPanel();
        }
    }

    /// <summary>
    /// Reset tất cả PanelTrigger trong scene (dùng khi New Game)
    /// </summary>
    public static void ResetAllTriggers()
    {
        PanelTrigger[] triggers = FindObjectsByType<PanelTrigger>(FindObjectsSortMode.None);
        foreach (PanelTrigger trigger in triggers)
        {
            trigger.ResetTriggerState();
        }
        Debug.Log($"[PanelTrigger] Đã reset {triggers.Length} PanelTrigger trong scene.");
    }

    private void OnDestroy()
    {
        // Kill animation khi destroy
        KillCurrentTween();
    }
}

