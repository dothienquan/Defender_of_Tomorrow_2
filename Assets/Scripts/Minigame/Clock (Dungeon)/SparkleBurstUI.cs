using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SparkleBurstUI : MonoBehaviour
{
    [Header("Settings")]
    public Sprite sparkleSprite;
    public int sparkleCount = 8;
    public float duration = 0.45f;
    public float startScale = 0.3f;
    public float endScale = 0.0f;
    public float burstRadius = 60f; // UI px distance

    private static SparkleBurstUI _instance;

    private void Awake()
    {
        _instance = this;
    }

    /// <summary>
    /// Spawns sparkles at a UI transform's position (Screen Space Overlay).
    /// </summary>
    public static void Spawn(Transform target)
    {
        if (_instance == null)
        {
            Debug.LogWarning("[SparkleBurstUI] No instance in scene. Please place this script on a UI GameObject.");
            return;
        }

        _instance.SpawnInternal(target as RectTransform);
    }

    private void SpawnInternal(RectTransform target)
    {
        if (sparkleSprite == null || target == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[SparkleBurstUI] Must be placed under a Canvas.");
            return;
        }

        for (int i = 0; i < sparkleCount; i++)
        {
            // Create sparkle object under the same canvas
            GameObject go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);

            // Position at target
            rt.position = target.position;

            Image img = go.GetComponent<Image>();
            img.sprite = sparkleSprite;
            img.color = new Color(1, 1, 1, 1);

            // Random direction
            Vector2 dir = Random.insideUnitCircle.normalized;
            Vector2 endPos = (Vector2)rt.anchoredPosition + dir * burstRadius;

            // Start small
            rt.localScale = Vector3.one * startScale;

            // Animation sequence
            Sequence seq = DOTween.Sequence();
            seq.SetEase(Ease.InOutSine); // EIO as chosen

            // Move + pop then fade
            seq.Append(rt.DOAnchorPos(endPos, duration * 0.7f));
            seq.Join(rt.DOScale(1f, duration * 0.2f));        // pop up
            seq.Append(rt.DOScale(endScale, duration * 0.8f)); // shrink fade
            seq.Join(img.DOFade(0f, duration * 0.8f));

            seq.OnComplete(() =>
            {
                Destroy(go);
            });
        }
    }
}
