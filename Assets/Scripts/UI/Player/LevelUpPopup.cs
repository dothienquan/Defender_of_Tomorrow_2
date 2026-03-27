using UnityEngine;
using TMPro;
using DG.Tweening;

public class LevelUpPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float duration = 1.2f;

    private void Awake()
    {
        if (text == null) text = GetComponentInChildren<TextMeshProUGUI>();

        // start small
        transform.localScale = Vector3.one * 0.2f;

        // scale up
        transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

        // fade out
        text.DOFade(0f, 0.8f).SetDelay(0.4f);

        // destroy after done
        Destroy(gameObject, duration);
    }
}
