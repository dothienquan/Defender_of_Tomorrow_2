using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TextSlowBlink : MonoBehaviour
{
    [Header("Blink Settings")]
    [SerializeField] private float blinkSpeed = 0.8f; // lower = slower
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 1f;

    private TMP_Text text;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}
