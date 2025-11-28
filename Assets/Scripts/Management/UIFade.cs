using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFade : Singleton<UIFade>
{
    [SerializeField] private Image fadeScreen;
    [SerializeField] private float fadeSpeed = 1f; // Fallback speed nếu không dùng duration

    private IEnumerator fadeRoutine;

    /// <summary>
    /// Fade to black với fade speed mặc định
    /// </summary>
    public void FadeToBlack() {
        FadeToBlack(-1f);
    }

    /// <summary>
    /// Fade to black với duration cụ thể
    /// </summary>
    public void FadeToBlack(float duration) {
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
        FadeToClear(-1f);
    }

    /// <summary>
    /// Fade to clear với duration cụ thể
    /// </summary>
    public void FadeToClear(float duration) {
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
                yield return null;
            }

            // Đảm bảo đạt target alpha chính xác
            fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, targetAlpha);
        }
    }

    /// <summary>
    /// Kiểm tra xem fade có đang chạy không
    /// </summary>
    public bool IsFading()
    {
        return fadeRoutine != null;
    }
}
