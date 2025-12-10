using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Hệ thống quản lý Mana cho player
/// </summary>
public class Mana : MonoBehaviour
{
    [Header("Mana Settings")]
    [SerializeField] private int maxMana = 100;
    [SerializeField] private float regenSpeed = 1f; // mana hồi mỗi giây
    [SerializeField] private float timeBetweenRegen = 0.1f; // thời gian giữa mỗi lần hồi

    [Header("UI")]
    [SerializeField] private Slider manaSlider;
    [SerializeField] private float fillAnimationDuration = 0.3f;

    private float currentMana;
    private Tweener sliderTween;
    private Coroutine regenCoroutine;

    public float CurrentMana => currentMana;
    public int MaxMana => maxMana;
    public float RegenSpeed => regenSpeed;

    private void Awake()
    {
        currentMana = maxMana;
    }

    private void Start()
    {
        UpdateManaSlider();
        StartRegen();
    }

    /// <summary>
    /// Sử dụng mana
    /// </summary>
    public bool UseMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            currentMana = Mathf.Max(0f, currentMana);
            UpdateManaSlider();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Hồi mana tự động
    /// </summary>
    private void StartRegen()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        regenCoroutine = StartCoroutine(RegenRoutine());
    }

    private IEnumerator RegenRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenRegen);
            
            if (currentMana < maxMana)
            {
                currentMana += regenSpeed * timeBetweenRegen;
                currentMana = Mathf.Min(maxMana, currentMana);
                UpdateManaSlider();
            }
        }
    }

    private void UpdateManaSlider()
    {
        if (manaSlider != null)
        {
            manaSlider.maxValue = maxMana;
            
            if (sliderTween != null && sliderTween.IsActive())
            {
                sliderTween.Kill();
            }
            
            sliderTween = manaSlider.DOValue(currentMana, fillAnimationDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Nâng cấp tốc độ hồi mana (giảm thời gian hồi)
    /// </summary>
    public void UpgradeRegenSpeed(float amount)
    {
        regenSpeed += amount;
        regenSpeed = Mathf.Max(0.1f, regenSpeed); // tối thiểu 0.1
    }

    /// <summary>
    /// Lấy tốc độ hồi mana hiện tại
    /// </summary>
    public float GetRegenSpeed()
    {
        return regenSpeed;
    }

    /// <summary>
    /// Set max mana (dùng khi upgrade)
    /// </summary>
    public void SetMaxMana(int newMax)
    {
        maxMana = newMax;
        currentMana = Mathf.Min(currentMana, maxMana);
        UpdateManaSlider();
    }

    /// <summary>
    /// Restore mana về đầy
    /// </summary>
    public void RestoreFullMana()
    {
        currentMana = maxMana;
        UpdateManaSlider();
    }

    private void OnDestroy()
    {
        if (sliderTween != null && sliderTween.IsActive())
        {
            sliderTween.Kill();
        }
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
    }
}

