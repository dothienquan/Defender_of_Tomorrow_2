using UnityEngine;
using System;

/// <summary>
/// Hệ thống quản lý upgrade points và stat upgrades:
/// - Mỗi khi lên cấp nhận 2 upgrade points
/// - Có thể nâng cấp: Max HP, Max Stamina, Movement Speed, Stamina Regen Speed
/// </summary>
public class PlayerStatUpgradeSystem : MonoBehaviour
{
    [Header("Upgrade Points")]
    [SerializeField] private int availableUpgradePoints = 0;

    [Header("Stat Upgrades")]
    [SerializeField] private int maxHealthUpgrades = 0;
    [SerializeField] private int maxStaminaUpgrades = 0;
    [SerializeField] private int movementSpeedUpgrades = 0;
    [SerializeField] private int staminaRegenSpeedUpgrades = 0;

    [Header("Upgrade Values Per Point")]
    [SerializeField] private int maxHealthPerUpgrade = 1;
    [SerializeField] private int maxStaminaPerUpgrade = 1;
    [SerializeField] private float movementSpeedPerUpgrade = 0.5f;
    [SerializeField] private float staminaRegenSpeedPerUpgrade = 0.2f; // giảm thời gian hồi stamina (giây)

    [Header("References")]
    [SerializeField] private PlayerLevelSystemLinear levelSystem;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Stamina stamina;
    [SerializeField] private PlayerController playerController;

    public event Action<int> OnUpgradePointsChanged;
    public event Action OnStatsUpgraded;

    public int AvailableUpgradePoints => availableUpgradePoints;
    public int MaxHealthUpgrades => maxHealthUpgrades;
    public int MaxStaminaUpgrades => maxStaminaUpgrades;
    public int MovementSpeedUpgrades => movementSpeedUpgrades;
    public int StaminaRegenSpeedUpgrades => staminaRegenSpeedUpgrades;

    private void Reset()
    {
        levelSystem = GetComponent<PlayerLevelSystemLinear>();
    }

    private void Awake()
    {
        // Tìm các component trên cùng GameObject trước
        if (levelSystem == null) levelSystem = GetComponent<PlayerLevelSystemLinear>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (stamina == null) stamina = GetComponent<Stamina>();
        if (playerController == null) playerController = GetComponent<PlayerController>();

        // Nếu không tìm thấy, thử tìm Singleton instances
        if (playerHealth == null && PlayerHealth.Instance != null)
            playerHealth = PlayerHealth.Instance;
        if (stamina == null && Stamina.Instance != null)
            stamina = Stamina.Instance;
        if (playerController == null && PlayerController.Instance != null)
            playerController = PlayerController.Instance;

        if (levelSystem != null)
        {
            levelSystem.OnLevelUp += OnLevelUp;
        }
    }

    private void OnDestroy()
    {
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp -= OnLevelUp;
        }
    }

    private void OnLevelUp(int newLevel)
    {
        // Mỗi khi lên cấp nhận 2 upgrade points
        AddUpgradePoints(2);
    }

    /// <summary>
    /// Thêm upgrade points
    /// </summary>
    public void AddUpgradePoints(int amount)
    {
        if (amount <= 0) return;
        availableUpgradePoints += amount;
        OnUpgradePointsChanged?.Invoke(availableUpgradePoints);
    }

    /// <summary>
    /// Nâng cấp Max Health
    /// </summary>
    public bool UpgradeMaxHealth()
    {
        if (availableUpgradePoints <= 0) return false;

        availableUpgradePoints--;
        maxHealthUpgrades++;

        if (playerHealth != null)
        {
            playerHealth.UpgradeMaxHealth(maxHealthPerUpgrade);
        }

        OnUpgradePointsChanged?.Invoke(availableUpgradePoints);
        OnStatsUpgraded?.Invoke();
        return true;
    }

    /// <summary>
    /// Nâng cấp Max Stamina
    /// </summary>
    public bool UpgradeMaxStamina()
    {
        if (availableUpgradePoints <= 0) return false;

        availableUpgradePoints--;
        maxStaminaUpgrades++;

        if (stamina != null)
        {
            stamina.UpgradeMaxStamina(maxStaminaPerUpgrade);
        }

        OnUpgradePointsChanged?.Invoke(availableUpgradePoints);
        OnStatsUpgraded?.Invoke();
        return true;
    }

    /// <summary>
    /// Nâng cấp Movement Speed
    /// </summary>
    public bool UpgradeMovementSpeed()
    {
        if (availableUpgradePoints <= 0) return false;

        availableUpgradePoints--;
        movementSpeedUpgrades++;

        if (playerController != null)
        {
            playerController.UpgradeMovementSpeed(movementSpeedPerUpgrade);
        }

        OnUpgradePointsChanged?.Invoke(availableUpgradePoints);
        OnStatsUpgraded?.Invoke();
        return true;
    }

    /// <summary>
    /// Nâng cấp Stamina Regen Speed (giảm thời gian hồi stamina)
    /// </summary>
    public bool UpgradeStaminaRegenSpeed()
    {
        if (availableUpgradePoints <= 0) return false;

        availableUpgradePoints--;
        staminaRegenSpeedUpgrades++;

        if (stamina != null)
        {
            stamina.UpgradeRegenSpeed(staminaRegenSpeedPerUpgrade);
        }

        OnUpgradePointsChanged?.Invoke(availableUpgradePoints);
        OnStatsUpgraded?.Invoke();
        return true;
    }

    /// <summary>
    /// Lấy giá trị hiện tại của stat sau khi upgrade
    /// </summary>
    public int GetCurrentMaxHealth()
    {
        if (playerHealth == null) return 0;
        var field = playerHealth.GetType().GetField("maxHealth",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (int)field.GetValue(playerHealth);
        }
        return playerHealth.MaxHealth;
    }

    public int GetCurrentMaxStamina()
    {
        if (stamina == null) return 0;
        return stamina.GetMaxStamina();
    }

    public float GetCurrentMovementSpeed()
    {
        if (playerController == null) return 0f;
        return playerController.GetMovementSpeed();
    }

    public float GetCurrentStaminaRegenSpeed()
    {
        if (stamina == null) return 0f;
        return stamina.GetRegenSpeed();
    }
}

