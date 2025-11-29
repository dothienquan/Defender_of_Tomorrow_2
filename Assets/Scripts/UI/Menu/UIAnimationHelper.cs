using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// Utility class chuyên dụng cho UI animations sử dụng DOTween
/// Cung cấp các method static để dễ dàng tạo animations cho UI elements
/// </summary>
public static class UIAnimationHelper
{
    #region Fade Animations

    /// <summary>
    /// Fade in một CanvasGroup
    /// </summary>
    public static Tween FadeIn(CanvasGroup canvasGroup, float duration = 0.3f, float targetAlpha = 1f, Ease ease = Ease.OutQuad)
    {
        if (canvasGroup == null) return null;
        canvasGroup.alpha = 0f;
        return canvasGroup.DOFade(targetAlpha, duration).SetEase(ease);
    }

    /// <summary>
    /// Fade out một CanvasGroup
    /// </summary>
    public static Tween FadeOut(CanvasGroup canvasGroup, float duration = 0.3f, float targetAlpha = 0f, Ease ease = Ease.InQuad)
    {
        if (canvasGroup == null) return null;
        return canvasGroup.DOFade(targetAlpha, duration).SetEase(ease);
    }

    /// <summary>
    /// Fade in một Image
    /// </summary>
    public static Tween FadeInImage(Image image, float duration = 0.3f, float targetAlpha = 1f, Ease ease = Ease.OutQuad)
    {
        if (image == null) return null;
        Color color = image.color;
        color.a = 0f;
        image.color = color;
        return image.DOFade(targetAlpha, duration).SetEase(ease);
    }

    /// <summary>
    /// Fade out một Image
    /// </summary>
    public static Tween FadeOutImage(Image image, float duration = 0.3f, float targetAlpha = 0f, Ease ease = Ease.InQuad)
    {
        if (image == null) return null;
        return image.DOFade(targetAlpha, duration).SetEase(ease);
    }

    /// <summary>
    /// Fade in một TextMeshProUGUI
    /// </summary>
    public static Tween FadeInText(TextMeshProUGUI text, float duration = 0.3f, float targetAlpha = 1f, Ease ease = Ease.OutQuad)
    {
        if (text == null) return null;
        Color color = text.color;
        color.a = 0f;
        text.color = color;
        return text.DOFade(targetAlpha, duration).SetEase(ease);
    }

    /// <summary>
    /// Fade out một TextMeshProUGUI
    /// </summary>
    public static Tween FadeOutText(TextMeshProUGUI text, float duration = 0.3f, float targetAlpha = 0f, Ease ease = Ease.InQuad)
    {
        if (text == null) return null;
        return text.DOFade(targetAlpha, duration).SetEase(ease);
    }

    #endregion

    #region Scale Animations

    /// <summary>
    /// Scale in từ nhỏ đến kích thước bình thường
    /// </summary>
    public static Tween ScaleIn(Transform target, float duration = 0.3f, float startScale = 0f, float endScale = 1f, Ease ease = Ease.OutBack)
    {
        if (target == null) return null;
        target.localScale = Vector3.one * startScale;
        return target.DOScale(endScale, duration).SetEase(ease);
    }

    /// <summary>
    /// Scale out từ kích thước bình thường đến nhỏ
    /// </summary>
    public static Tween ScaleOut(Transform target, float duration = 0.3f, float endScale = 0f, Ease ease = Ease.InBack)
    {
        if (target == null) return null;
        return target.DOScale(endScale, duration).SetEase(ease);
    }

    /// <summary>
    /// Bounce scale effect
    /// </summary>
    public static Sequence BounceScale(Transform target, float duration = 0.5f, float scaleAmount = 1.2f, Ease ease = Ease.OutBounce)
    {
        if (target == null) return null;
        Vector3 originalScale = target.localScale;
        Sequence seq = DOTween.Sequence();
        seq.Append(target.DOScale(originalScale * scaleAmount, duration * 0.5f).SetEase(ease));
        seq.Append(target.DOScale(originalScale, duration * 0.5f).SetEase(Ease.InBounce));
        return seq;
    }

    /// <summary>
    /// Pulse effect - scale lên xuống liên tục
    /// </summary>
    public static Tween Pulse(Transform target, float duration = 1f, float scaleAmount = 1.1f, int loops = -1)
    {
        if (target == null) return null;
        Vector3 originalScale = target.localScale;
        return target.DOScale(originalScale * scaleAmount, duration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(loops, LoopType.Yoyo);
    }

    /// <summary>
    /// Pop effect - scale nhanh lên rồi về bình thường
    /// </summary>
    public static Sequence Pop(Transform target, float duration = 0.3f, float scaleAmount = 1.15f)
    {
        if (target == null) return null;
        Vector3 originalScale = target.localScale;
        Sequence seq = DOTween.Sequence();
        seq.Append(target.DOScale(originalScale * scaleAmount, duration * 0.3f).SetEase(Ease.OutQuad));
        seq.Append(target.DOScale(originalScale, duration * 0.7f).SetEase(Ease.InQuad));
        return seq;
    }

    #endregion

    #region Slide Animations

    /// <summary>
    /// Slide in từ bên trái
    /// </summary>
    public static Tween SlideInFromLeft(RectTransform target, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.OutCubic)
    {
        if (target == null) return null;
        Vector2 originalPos = target.anchoredPosition;
        target.anchoredPosition = new Vector2(originalPos.x - offset, originalPos.y);
        return target.DOAnchorPos(originalPos, duration).SetEase(ease);
    }

    /// <summary>
    /// Slide in từ bên phải
    /// </summary>
    public static Tween SlideInFromRight(RectTransform target, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.OutCubic)
    {
        if (target == null) return null;
        Vector2 originalPos = target.anchoredPosition;
        target.anchoredPosition = new Vector2(originalPos.x + offset, originalPos.y);
        return target.DOAnchorPos(originalPos, duration).SetEase(ease);
    }

    /// <summary>
    /// Slide in từ phía trên
    /// </summary>
    public static Tween SlideInFromTop(RectTransform target, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.OutCubic)
    {
        if (target == null) return null;
        Vector2 originalPos = target.anchoredPosition;
        target.anchoredPosition = new Vector2(originalPos.x, originalPos.y + offset);
        return target.DOAnchorPos(originalPos, duration).SetEase(ease);
    }

    /// <summary>
    /// Slide in từ phía dưới
    /// </summary>
    public static Tween SlideInFromBottom(RectTransform target, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.OutCubic)
    {
        if (target == null) return null;
        Vector2 originalPos = target.anchoredPosition;
        target.anchoredPosition = new Vector2(originalPos.x, originalPos.y - offset);
        return target.DOAnchorPos(originalPos, duration).SetEase(ease);
    }

    /// <summary>
    /// Slide out về bên trái
    /// </summary>
    public static Tween SlideOutToLeft(RectTransform target, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.InCubic)
    {
        if (target == null) return null;
        Vector2 originalPos = target.anchoredPosition;
        return target.DOAnchorPos(new Vector2(originalPos.x - offset, originalPos.y), duration).SetEase(ease);
    }

    /// <summary>
    /// Slide out về bên phải
    /// </summary>
    public static Tween SlideOutToRight(RectTransform target, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.InCubic)
    {
        if (target == null) return null;
        Vector2 originalPos = target.anchoredPosition;
        return target.DOAnchorPos(new Vector2(originalPos.x + offset, originalPos.y), duration).SetEase(ease);
    }

    /// <summary>
    /// Slide out về phía trên
    /// </summary>
    public static Tween SlideOutToTop(RectTransform target, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.InCubic)
    {
        if (target == null) return null;
        Vector2 originalPos = target.anchoredPosition;
        return target.DOAnchorPos(new Vector2(originalPos.x, originalPos.y + offset), duration).SetEase(ease);
    }

    /// <summary>
    /// Slide out về phía dưới
    /// </summary>
    public static Tween SlideOutToBottom(RectTransform target, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.InCubic)
    {
        if (target == null) return null;
        Vector2 originalPos = target.anchoredPosition;
        return target.DOAnchorPos(new Vector2(originalPos.x, originalPos.y - offset), duration).SetEase(ease);
    }

    #endregion

    #region Shake Animations

    /// <summary>
    /// Shake effect
    /// </summary>
    public static Tween Shake(Transform target, float duration = 0.5f, float strength = 10f, int vibrato = 10, float randomness = 90f)
    {
        if (target == null) return null;
        return target.DOShakePosition(duration, strength, vibrato, randomness, false, true);
    }

    /// <summary>
    /// Shake rotation
    /// </summary>
    public static Tween ShakeRotation(Transform target, float duration = 0.5f, float strength = 10f, int vibrato = 10, float randomness = 90f)
    {
        if (target == null) return null;
        return target.DOShakeRotation(duration, strength, vibrato, randomness, false);
    }

    /// <summary>
    /// Shake scale
    /// </summary>
    public static Tween ShakeScale(Transform target, float duration = 0.5f, float strength = 0.2f, int vibrato = 10, float randomness = 90f)
    {
        if (target == null) return null;
        return target.DOShakeScale(duration, strength, vibrato, randomness, false);
    }

    #endregion

    #region Rotation Animations

    /// <summary>
    /// Rotate 360 độ
    /// </summary>
    public static Tween Rotate360(Transform target, float duration = 1f, int loops = -1, RotateMode mode = RotateMode.FastBeyond360)
    {
        if (target == null) return null;
        return target.DORotate(new Vector3(0, 0, 360), duration, mode)
            .SetLoops(loops, LoopType.Restart)
            .SetEase(Ease.Linear);
    }

    /// <summary>
    /// Rotate qua lại (wobble)
    /// </summary>
    public static Tween Wobble(Transform target, float duration = 0.5f, float angle = 15f, int loops = -1)
    {
        if (target == null) return null;
        Vector3 originalRotation = target.localEulerAngles;
        return target.DORotate(originalRotation + new Vector3(0, 0, angle), duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(loops, LoopType.Yoyo);
    }

    #endregion

    #region Combined Animations

    /// <summary>
    /// Fade và Scale in cùng lúc
    /// </summary>
    public static Sequence FadeScaleIn(Transform target, CanvasGroup canvasGroup, float duration = 0.3f, float startScale = 0f, Ease ease = Ease.OutBack)
    {
        if (target == null) return null;
        Sequence seq = DOTween.Sequence();
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            seq.Join(canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad));
        }
        
        target.localScale = Vector3.one * startScale;
        seq.Join(target.DOScale(1f, duration).SetEase(ease));
        
        return seq;
    }

    /// <summary>
    /// Fade và Scale out cùng lúc
    /// </summary>
    public static Sequence FadeScaleOut(Transform target, CanvasGroup canvasGroup, float duration = 0.3f, float endScale = 0f, Ease ease = Ease.InBack)
    {
        if (target == null) return null;
        Sequence seq = DOTween.Sequence();
        
        if (canvasGroup != null)
        {
            seq.Join(canvasGroup.DOFade(0f, duration).SetEase(Ease.InQuad));
        }
        
        seq.Join(target.DOScale(endScale, duration).SetEase(ease));
        
        return seq;
    }

    /// <summary>
    /// Slide và Fade in cùng lúc
    /// </summary>
    public static Sequence SlideFadeIn(RectTransform target, CanvasGroup canvasGroup, Vector2 direction, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.OutCubic)
    {
        if (target == null) return null;
        Sequence seq = DOTween.Sequence();
        
        Vector2 originalPos = target.anchoredPosition;
        target.anchoredPosition = originalPos - direction * offset;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            seq.Join(canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad));
        }
        
        seq.Join(target.DOAnchorPos(originalPos, duration).SetEase(ease));
        
        return seq;
    }

    /// <summary>
    /// Slide và Fade out cùng lúc
    /// </summary>
    public static Sequence SlideFadeOut(RectTransform target, CanvasGroup canvasGroup, Vector2 direction, float duration = 0.3f, float offset = 1000f, Ease ease = Ease.InCubic)
    {
        if (target == null) return null;
        Sequence seq = DOTween.Sequence();
        
        Vector2 originalPos = target.anchoredPosition;
        Vector2 endPos = originalPos + direction * offset;
        
        if (canvasGroup != null)
        {
            seq.Join(canvasGroup.DOFade(0f, duration).SetEase(Ease.InQuad));
        }
        
        seq.Join(target.DOAnchorPos(endPos, duration).SetEase(ease));
        
        return seq;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Dừng tất cả animations của một transform
    /// </summary>
    public static void KillAnimations(Transform target)
    {
        if (target == null) return;
        target.DOKill();
    }

    /// <summary>
    /// Dừng tất cả animations của một CanvasGroup
    /// </summary>
    public static void KillAnimations(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;
        canvasGroup.DOKill();
    }

    /// <summary>
    /// Dừng tất cả animations của một Image
    /// </summary>
    public static void KillAnimations(Image image)
    {
        if (image == null) return;
        image.DOKill();
    }

    /// <summary>
    /// Dừng tất cả animations của một TextMeshProUGUI
    /// </summary>
    public static void KillAnimations(TextMeshProUGUI text)
    {
        if (text == null) return;
        text.DOKill();
    }

    /// <summary>
    /// Dừng tất cả animations của một RectTransform
    /// </summary>
    public static void KillAnimations(RectTransform rectTransform)
    {
        if (rectTransform == null) return;
        rectTransform.DOKill();
    }

    #endregion
}

