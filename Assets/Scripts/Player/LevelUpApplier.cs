using UnityEngine;
using System;
using System.Reflection;

/// <summary>
/// Áp dụng hiệu ứng khi lên cấp:
/// - +1 máu tối đa mỗi cấp (và heal +1)
/// - Cấp 1→10: +1 tối đa Stamina mỗi cấp
/// - Cấp ≥11: giảm thời gian hồi Stamina 0.2s mỗi cấp (tối thiểu 0.2s)
/// </summary>
public class LevelUpApplier : MonoBehaviour
{
    [Header("Refs")]
    public PlayerLevelSystemLinear levelSystem;
    public MonoBehaviour playerHealth;  // tham chiếu PlayerHealth.cs
    public MonoBehaviour stamina;       // tham chiếu Stamina.cs

    [Header("Tham số Stamina")]
    public float staminaRefreshReducePerLevel = 0.2f;
    public float staminaRefreshMin = 0.2f;

    private System.Reflection.FieldInfo _fiMaxHealth;
    private System.Reflection.FieldInfo _fiCurrentHealth;
    private System.Reflection.FieldInfo _fiStaminaMax;
    private System.Reflection.FieldInfo _fiStaminaCurrent;
    private System.Reflection.FieldInfo _fiStaminaRefresh;

    private void Reset()
    {
        levelSystem = GetComponent<PlayerLevelSystemLinear>();
    }

    private void Awake()
    {
        if (levelSystem == null) levelSystem = GetComponent<PlayerLevelSystemLinear>();
        if (levelSystem != null) levelSystem.OnLevelUp += HandleLevelUp;
        CacheFields();
    }

    private void OnDestroy()
    {
        if (levelSystem != null) levelSystem.OnLevelUp -= HandleLevelUp;
    }

    private void CacheFields()
    {
        if (playerHealth != null)
        {
            _fiMaxHealth = TryFindField(playerHealth, new[] { "maxHealth", "playerMaxHealth", "maxHP" });
            _fiCurrentHealth = TryFindField(playerHealth, new[] { "currentHealth", "health", "hp", "_currentHealth" });
        }
        if (stamina != null)
        {
            _fiStaminaMax = TryFindField(stamina, new[] { "startingStamina", "maxStamina", "_maxStamina" });
            _fiStaminaCurrent = TryFindField(stamina, new[] { "CurrentStamina", "currentStamina", "_currentStamina" });
            _fiStaminaRefresh = TryFindField(stamina, new[] { "timeBetweenStaminaRefresh", "staminaRefreshTime", "_refreshTime" });
        }
    }

    private System.Reflection.FieldInfo TryFindField(MonoBehaviour target, string[] names)
    {
        var t = target.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) return f;
        }
        return null;
    }

    private void HandleLevelUp(int newLevel)
    {
        // Health
        if (playerHealth != null && _fiMaxHealth != null)
        {
            int maxH = (int)_fiMaxHealth.GetValue(playerHealth);
            maxH += 1;
            _fiMaxHealth.SetValue(playerHealth, maxH);

            if (_fiCurrentHealth != null)
            {
                int cur = (int)_fiCurrentHealth.GetValue(playerHealth);
                cur = Mathf.Min(maxH, cur + 1);
                _fiCurrentHealth.SetValue(playerHealth, cur);
            }
            InvokeIfExists(playerHealth, new[] { "UpdateHealthUI", "RefreshUI", "UpdateUI" });
        }

        // Stamina
        if (stamina != null)
        {
            if (newLevel <= 10 && _fiStaminaMax != null)
            {
                int maxSt = (int)_fiStaminaMax.GetValue(stamina);
                maxSt += 1;
                _fiStaminaMax.SetValue(stamina, maxSt);

                if (_fiStaminaCurrent != null)
                {
                    int cur = (int)_fiStaminaCurrent.GetValue(stamina);
                    cur = Mathf.Min(maxSt, cur + 1);
                    _fiStaminaCurrent.SetValue(stamina, cur);
                }
                InvokeIfExists(stamina, new[] { "RebuildUI", "RefreshUI", "UpdateUI", "SyncIcons" });
            }
            else if (newLevel >= 11 && _fiStaminaRefresh != null)
            {
                float t = Convert.ToSingle(_fiStaminaRefresh.GetValue(stamina));
                t = Mathf.Max(staminaRefreshMin, t - staminaRefreshReducePerLevel);
                _fiStaminaRefresh.SetValue(stamina, t);
            }
        }
    }

    private void InvokeIfExists(MonoBehaviour target, string[] methodNames)
    {
        var t = target.GetType();
        foreach (var m in methodNames)
        {
            var mi = t.GetMethod(m, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mi != null && mi.GetParameters().Length == 0)
            {
                mi.Invoke(target, null);
                return;
            }
        }
    }
}