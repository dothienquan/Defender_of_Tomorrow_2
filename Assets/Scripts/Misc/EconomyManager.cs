using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EconomyManager : Singleton<EconomyManager>
{
    private TMP_Text goldText;
    private int currentGold = 0;

    const string COIN_AMOUNT_TEXT = "Gold Amount Text";

    public int CurrentGold => currentGold;

    private void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // Tìm gold text UI nếu có
        UpdateGoldText();
    }

    /// <summary>
    /// Thêm gold (khi nhặt coin)
    /// </summary>
    public void UpdateCurrentGold() 
    {
        AddGold(1);
    }

    /// <summary>
    /// Thêm gold với số lượng tùy chỉnh
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[EconomyManager] Cannot add negative gold: {amount}");
            return;
        }

        currentGold += amount;
        UpdateGoldText();
        Debug.Log($"[EconomyManager] Added {amount} gold. Total: {currentGold}");
    }

    /// <summary>
    /// Trừ gold (khi mua item)
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[EconomyManager] Cannot spend negative gold: {amount}");
            return false;
        }

        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateGoldText();
            Debug.Log($"[EconomyManager] Spent {amount} gold. Remaining: {currentGold}");
            return true;
        }
        else
        {
            Debug.LogWarning($"[EconomyManager] Not enough gold! Need {amount}, have {currentGold}");
            return false;
        }
    }

    /// <summary>
    /// Set gold (dùng khi load game)
    /// </summary>
    public void SetGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[EconomyManager] Cannot set negative gold: {amount}. Setting to 0.");
            currentGold = 0;
        }
        else
        {
            currentGold = amount;
        }
        UpdateGoldText();
        Debug.Log($"[EconomyManager] Gold set to {currentGold}");
    }

    /// <summary>
    /// Kiểm tra có đủ gold không
    /// </summary>
    public bool HasEnoughGold(int amount)
    {
        return currentGold >= amount;
    }

    /// <summary>
    /// Cập nhật UI text
    /// </summary>
    private void UpdateGoldText()
    {
        if (goldText == null)
        {
            GameObject goldTextObj = GameObject.Find(COIN_AMOUNT_TEXT);
            if (goldTextObj != null)
            {
                goldText = goldTextObj.GetComponent<TMP_Text>();
            }
        }

        if (goldText != null)
        {
            goldText.text = currentGold.ToString("D3");
        }
    }

    /// <summary>
    /// Force update gold text (dùng khi UI được tạo sau)
    /// </summary>
    public void RefreshGoldText()
    {
        goldText = null; // Reset để tìm lại
        UpdateGoldText();
    }

    // ========== DEBUG/CHEAT METHODS ==========
    
    /// <summary>
    /// Set gold từ Context Menu (chỉ dùng trong Editor hoặc Debug)
    /// </summary>
    [ContextMenu("Set Gold to 100")]
    private void SetGold100()
    {
        SetGold(100);
    }

    [ContextMenu("Set Gold to 500")]
    private void SetGold500()
    {
        SetGold(500);
    }

    [ContextMenu("Set Gold to 1000")]
    private void SetGold1000()
    {
        SetGold(1000);
    }

    [ContextMenu("Add 100 Gold")]
    private void AddGold100()
    {
        AddGold(100);
    }

    [ContextMenu("Reset Gold to 0")]
    private void ResetGold()
    {
        SetGold(0);
    }
}
