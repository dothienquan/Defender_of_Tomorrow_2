using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Stamina : Singleton<Stamina>
{
    public int CurrentStamina { get; private set; }

    [SerializeField] private int timeBetweenStaminaRefresh = 3;
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
