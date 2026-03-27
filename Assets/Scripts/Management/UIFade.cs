using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFade : Singleton<UIFade>
{
    [SerializeField] private Image fadeScreen;
    [SerializeField] private float fadeSpeed = 1f; // Fallback speed nếu không dùng duration

    private IEnumerator fadeRoutine;

    protected override void Awake()
    {
        base.Awake();
        EnsureFadeScreenOnTop();
    }

    /// <summary>
    /// Fade to black với fade speed mặc định
    /// </summary>
    public void FadeToBlack() {
        EnsureFadeScreenOnTop();
        FadeToBlack(-1f);
    }

    /// <summary>
    /// Fade to black với duration cụ thể
    /// </summary>
    public void FadeToBlack(float duration) {
        EnsureFadeScreenOnTop();
        if (fadeRoutine != null) {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = FadeRoutine(1, duration);
        StartCoroutine(fadeRoutine);
    }

    /// <summary>
    /// Fade to clear với fade speed mặc định
    /// </summary>
    public void FadeToClear() {
        EnsureFadeScreenOnTop();
        FadeToClear(-1f);
    }

    /// <summary>
    /// Fade to clear với duration cụ thể
    /// </summary>
    public void FadeToClear(float duration) {
        EnsureFadeScreenOnTop();
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = FadeRoutine(0, duration);
        StartCoroutine(fadeRoutine);
    }

    /// <summary>
    /// Fade routine với duration tùy chọn
    /// </summary>
    private IEnumerator FadeRoutine(float targetAlpha, float duration = -1f) {
        float startAlpha = fadeScreen.color.a;
        float elapsedTime = 0f;

        // Nếu duration < 0, dùng fadeSpeed cũ
        if (duration < 0f)
        {
            while (!Mathf.Approximately(fadeScreen.color.a, targetAlpha))
            {
                float alpha = Mathf.MoveTowards(fadeScreen.color.a, targetAlpha, fadeSpeed * Time.deltaTime);
                fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, alpha);
                UpdateRaycastTarget(alpha);
                yield return null;
            }
        }
        else
        {
            // Dùng duration cụ thể
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, alpha);
                UpdateRaycastTarget(alpha);
                yield return null;
            }

            // Đảm bảo đạt target alpha chính xác
            fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, targetAlpha);
            UpdateRaycastTarget(targetAlpha);
        }
    }

    /// <summary>
    /// Cập nhật raycast target dựa trên alpha: tắt raycast khi hoàn toàn trong suốt
    /// </summary>
    private void UpdateRaycastTarget(float alpha)
    {
        if (fadeScreen != null)
        {
            // Chỉ chặn raycast khi có alpha (có thể nhìn thấy)
            // Khi alpha = 0 (hoàn toàn trong suốt), tắt raycast để cho phép tương tác với UI bên dưới
            fadeScreen.raycastTarget = alpha > 0.01f;
        }
    }

    /// <summary>
    /// Kiểm tra xem fade có đang chạy không
    /// </summary>
    public bool IsFading()
    {
        return fadeRoutine != null;
    }

    /// <summary>
    /// Set alpha ngay lập tức (không fade)
    /// </summary>
    /// <param name="alpha">Alpha value (0 = clear, 1 = black)</param>
    public void SetAlphaImmediate(float alpha)
    {
        if (fadeScreen != null)
        {
            alpha = Mathf.Clamp01(alpha);
            fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, alpha);
            UpdateRaycastTarget(alpha);
            EnsureFadeScreenOnTop();
        }
    }

    /// <summary>
    /// Đảm bảo fade screen luôn nằm trên cùng (sibling index cao nhất và Canvas sort order cao nhất)
    /// </summary>
    private void EnsureFadeScreenOnTop()
    {
        if (fadeScreen != null && fadeScreen.transform != null)
        {
            Transform parent = fadeScreen.transform.parent;
            if (parent != null)
            {
                // Đặt fade screen làm child cuối cùng (render trên cùng trong cùng Canvas)
                fadeScreen.transform.SetAsLastSibling();

                // Đảm bảo Canvas chứa fade screen có sort order cao nhất
                Canvas parentCanvas = parent.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    // Tìm tất cả Canvas trong scene và đảm bảo fade Canvas có sort order cao nhất
                    Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    int maxSortOrder = int.MinValue;
                    
                    foreach (Canvas canvas in allCanvases)
                    {
                        if (canvas != parentCanvas && canvas.sortingOrder > maxSortOrder)
                        {
                            maxSortOrder = canvas.sortingOrder;
                        }
                    }

                    // Đặt sort order của fade Canvas cao hơn tất cả Canvas khác
                    if (parentCanvas.sortingOrder <= maxSortOrder)
                    {
                        parentCanvas.sortingOrder = maxSortOrder + 1;
                    }
                }
            }
        }
    }
}
