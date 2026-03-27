using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI để hiển thị và nâng cấp stats của player
/// </summary>
public class StatUpgradeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStatUpgradeSystem upgradeSystem;

    [Header("Upgrade Points Display")]
    [SerializeField] private TextMeshProUGUI upgradePointsText;

    [Header("Stat Upgrade Buttons")]
    [SerializeField] private Button upgradeHealthButton;
    [SerializeField] private Button upgradeStaminaButton;
    [SerializeField] private Button upgradeMovementSpeedButton;
    [SerializeField] private Button upgradeStaminaRegenButton;

    [Header("Stat Display Texts")]
    [SerializeField] private TextMeshProUGUI healthValueText;
    [SerializeField] private TextMeshProUGUI staminaValueText;
    [SerializeField] private TextMeshProUGUI movementSpeedValueText;
    [SerializeField] private TextMeshProUGUI staminaRegenValueText;

    [Header("Upgrade Count Texts")]
    [SerializeField] private TextMeshProUGUI healthUpgradeCountText;
    [SerializeField] private TextMeshProUGUI staminaUpgradeCountText;
    [SerializeField] private TextMeshProUGUI movementSpeedUpgradeCountText;
    [SerializeField] private TextMeshProUGUI staminaRegenUpgradeCountText;

    [Header("UI Panel")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.U;

    [Header("Pulse Effect Settings")]
    [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private float pulseMinAlpha = 0.5f;
    [SerializeField] private float pulseMaxAlpha = 1f;

    private bool isPanelOpen = false;
    private Tweener pulseTween;

    private void Reset()
    {
        if (upgradeSystem == null)
            upgradeSystem = FindFirstObjectByType<PlayerStatUpgradeSystem>();
    }

    private void Awake()
    {
        if (upgradeSystem == null)
            upgradeSystem = FindFirstObjectByType<PlayerStatUpgradeSystem>();

        if (upgradeSystem != null)
        {
            upgradeSystem.OnUpgradePointsChanged += UpdateUpgradePointsDisplay;
            upgradeSystem.OnStatsUpgraded += UpdateAllStatDisplays;
        }
    }

    private void Start()
    {
        SetupButtons();
        UpdateAllStatDisplays();
        
        int currentPoints = upgradeSystem != null ? upgradeSystem.AvailableUpgradePoints : 0;
        UpdateUpgradePointsDisplay(currentPoints);

        if (upgradePanel != null)
        {
            upgradePanel.SetActive(isPanelOpen);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }

    private void OnDestroy()
    {
        if (upgradeSystem != null)
        {
            upgradeSystem.OnUpgradePointsChanged -= UpdateUpgradePointsDisplay;
            upgradeSystem.OnStatsUpgraded -= UpdateAllStatDisplays;
        }

        // Kill pulse animation khi destroy
        if (pulseTween != null && pulseTween.IsActive())
        {
            pulseTween.Kill();
        }
    }

    private void SetupButtons()
    {
        if (upgradeHealthButton != null)
        {
            upgradeHealthButton.onClick.RemoveAllListeners();
            upgradeHealthButton.onClick.AddListener(() => OnUpgradeHealthClicked());
        }

        if (upgradeStaminaButton != null)
        {
            upgradeStaminaButton.onClick.RemoveAllListeners();
            upgradeStaminaButton.onClick.AddListener(() => OnUpgradeStaminaClicked());
        }

        if (upgradeMovementSpeedButton != null)
        {
            upgradeMovementSpeedButton.onClick.RemoveAllListeners();
            upgradeMovementSpeedButton.onClick.AddListener(() => OnUpgradeMovementSpeedClicked());
        }

        if (upgradeStaminaRegenButton != null)
        {
            upgradeStaminaRegenButton.onClick.RemoveAllListeners();
            upgradeStaminaRegenButton.onClick.AddListener(() => OnUpgradeStaminaRegenClicked());
        }
    }

    private void OnUpgradeHealthClicked()
    {
        if (upgradeSystem != null && upgradeSystem.UpgradeMaxHealth())
        {
            PlayUpgradeAnimation(upgradeHealthButton?.transform);
        }
    }

    private void OnUpgradeStaminaClicked()
    {
        if (upgradeSystem != null && upgradeSystem.UpgradeMaxStamina())
        {
            PlayUpgradeAnimation(upgradeStaminaButton?.transform);
        }
    }

    private void OnUpgradeMovementSpeedClicked()
    {
        if (upgradeSystem != null && upgradeSystem.UpgradeMovementSpeed())
        {
            PlayUpgradeAnimation(upgradeMovementSpeedButton?.transform);
        }
    }

    private void OnUpgradeStaminaRegenClicked()
    {
        if (upgradeSystem != null && upgradeSystem.UpgradeStaminaRegenSpeed())
        {
            PlayUpgradeAnimation(upgradeStaminaRegenButton?.transform);
        }
    }

    private void PlayUpgradeAnimation(Transform target)
    {
        if (target != null)
        {
            target.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
        }
    }

    private void UpdateUpgradePointsDisplay(int points)
    {
        if (upgradePointsText != null)
        {
            upgradePointsText.text = $"Điểm kỹ năng khả dụng: {points}";
            
            // Bật/tắt hiệu ứng nhấp nháy dựa trên số điểm
            if (points > 0)
            {
                StartPulseEffect();
            }
            else
            {
                StopPulseEffect();
            }
        }

        UpdateButtonStates();
    }

    /// <summary>
    /// Bắt đầu hiệu ứng nhấp nháy cho upgrade points text
    /// </summary>
    private void StartPulseEffect()
    {
        if (upgradePointsText == null) return;

        // Dừng animation cũ nếu có
        StopPulseEffect();

        // Đảm bảo alpha ban đầu là max
        Color color = upgradePointsText.color;
        color.a = pulseMaxAlpha;
        upgradePointsText.color = color;

        // Tạo animation nhấp nháy (fade in/out)
        pulseTween = upgradePointsText.DOFade(pulseMinAlpha, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// Dừng hiệu ứng nhấp nháy và đặt alpha về max
    /// </summary>
    private void StopPulseEffect()
    {
        if (pulseTween != null && pulseTween.IsActive())
        {
            pulseTween.Kill();
            pulseTween = null;
        }

        if (upgradePointsText != null)
        {
            Color color = upgradePointsText.color;
            color.a = pulseMaxAlpha;
            upgradePointsText.color = color;
        }
    }

    private void UpdateAllStatDisplays()
    {
        if (upgradeSystem == null) return;

        // Health
        if (healthValueText != null)
        {
            int currentHealth = upgradeSystem.GetCurrentMaxHealth();
            healthValueText.text = $"{currentHealth}";
        }

        if (healthUpgradeCountText != null)
        {
            healthUpgradeCountText.text = $"+{upgradeSystem.MaxHealthUpgrades}";
        }

        // Stamina
        if (staminaValueText != null)
        {
            int currentStamina = upgradeSystem.GetCurrentMaxStamina();
            staminaValueText.text = $"{currentStamina}";
        }

        if (staminaUpgradeCountText != null)
        {
            staminaUpgradeCountText.text = $"+{upgradeSystem.MaxStaminaUpgrades}";
        }

        // Movement Speed
        if (movementSpeedValueText != null)
        {
            float currentSpeed = upgradeSystem.GetCurrentMovementSpeed();
            movementSpeedValueText.text = $"{currentSpeed:F1}";
        }

        if (movementSpeedUpgradeCountText != null)
        {
            movementSpeedUpgradeCountText.text = $"+{upgradeSystem.MovementSpeedUpgrades}";
        }

        // Stamina Regen Speed
        if (staminaRegenValueText != null)
        {
            float currentRegen = upgradeSystem.GetCurrentStaminaRegenSpeed();
            staminaRegenValueText.text = $"{currentRegen:F1}s";
        }

        if (staminaRegenUpgradeCountText != null)
        {
            staminaRegenUpgradeCountText.text = $"+{upgradeSystem.StaminaRegenSpeedUpgrades}";
        }

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (upgradeSystem == null) return;

        bool hasPoints = upgradeSystem.AvailableUpgradePoints > 0;

        if (upgradeHealthButton != null)
        {
            upgradeHealthButton.interactable = hasPoints;
        }

        if (upgradeStaminaButton != null)
        {
            upgradeStaminaButton.interactable = hasPoints;
        }

        if (upgradeMovementSpeedButton != null)
        {
            upgradeMovementSpeedButton.interactable = hasPoints;
        }

        if (upgradeStaminaRegenButton != null)
        {
            upgradeStaminaRegenButton.interactable = hasPoints;
        }
    }

    public void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(isPanelOpen);
            
            if (isPanelOpen)
            {
                upgradePanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                UpdateAllStatDisplays();
            }
        }
    }

    public void OpenPanel()
    {
        if (!isPanelOpen)
        {
            TogglePanel();
        }
    }

    public void ClosePanel()
    {
        if (isPanelOpen)
        {
            TogglePanel();
        }
    }
}

