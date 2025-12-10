using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Stamina : Singleton<Stamina>
{
    public int CurrentStamina { get; private set; }

    [SerializeField] private float timeBetweenStaminaRefresh = 3f;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private float fillAnimationDuration = 0.3f;

    private int startingStamina = 3;
    private int maxStamina;
    private Tweener sliderTween;

    protected override void Awake() {
        base.Awake();

        maxStamina = startingStamina;
        CurrentStamina = startingStamina;
    }

    private void Start() {
        UpdateStaminaSlider();
    }

    public void UseStamina() {
        CurrentStamina--;
        UpdateStaminaSlider();
    }

    public void RefreshStamina() {
        if (CurrentStamina < maxStamina) {
            CurrentStamina++;
        }
        UpdateStaminaSlider();
    }

    public void UpdateUI() {
        UpdateStaminaSlider();
    }

    /// <summary>
    /// Nâng cấp max stamina (dùng cho upgrade system)
    /// </summary>
    public void UpgradeMaxStamina(int amount)
    {
        startingStamina += amount; // Cập nhật starting để khi reset vẫn đúng
        maxStamina += amount;
        CurrentStamina += amount; // Tăng cả current stamina
        UpdateStaminaSlider();
    }

    /// <summary>
    /// Lấy max stamina hiện tại
    /// </summary>
    public int GetMaxStamina()
    {
        return maxStamina;
    }

    /// <summary>
    /// Nâng cấp tốc độ hồi stamina (giảm thời gian chờ giữa các lần hồi)
    /// </summary>
    public void UpgradeRegenSpeed(float amount)
    {
        timeBetweenStaminaRefresh -= amount;
        timeBetweenStaminaRefresh = Mathf.Max(0.1f, timeBetweenStaminaRefresh); // Tối thiểu 0.1 giây
    }

    /// <summary>
    /// Lấy tốc độ hồi stamina hiện tại (thời gian giữa các lần hồi)
    /// </summary>
    public float GetRegenSpeed()
    {
        return timeBetweenStaminaRefresh;
    }

    private IEnumerator RefreshStaminaRoutine() {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenStaminaRefresh);
            RefreshStamina();
        }
    }

    private void UpdateStaminaSlider() {
        if (staminaSlider != null) {
            staminaSlider.maxValue = maxStamina;
            
            // Kill previous tween if exists
            if (sliderTween != null && sliderTween.IsActive()) {
                sliderTween.Kill();
            }
            
            // Animate slider value smoothly
            sliderTween = staminaSlider.DOValue(CurrentStamina, fillAnimationDuration)
                .SetEase(Ease.OutQuad);
        }

        if (CurrentStamina < maxStamina) {
            StopAllCoroutines();
            StartCoroutine(RefreshStaminaRoutine());
        }
    }

    private void OnDestroy() {
        if (sliderTween != null && sliderTween.IsActive()) {
            sliderTween.Kill();
        }
    }
}
