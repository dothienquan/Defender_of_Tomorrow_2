using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class FadeAndMoveOnTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject objectToFadeAndMove;
    [SerializeField] private Transform targetPosition;

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool runOnce = true;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    [Header("Move Settings")]
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private float delayAfterFade = 0.1f;

    private bool hasTriggered = false;
    private Collider2D triggerCollider;
    private List<Renderer> renderers = new List<Renderer>();
    private List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
    private SpriteRenderer spriteRenderer;
    private Dictionary<SpriteRenderer, float> originalSpriteAlphas = new Dictionary<SpriteRenderer, float>();
    private Dictionary<CanvasGroup, float> originalCanvasAlphas = new Dictionary<CanvasGroup, float>();

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        CacheRenderComponents();
    }

    private void CacheRenderComponents()
    {
        if (objectToFadeAndMove == null) return;

        renderers.Clear();
        canvasGroups.Clear();
        originalSpriteAlphas.Clear();
        originalCanvasAlphas.Clear();

        renderers.AddRange(objectToFadeAndMove.GetComponentsInChildren<Renderer>());
        canvasGroups.AddRange(objectToFadeAndMove.GetComponentsInChildren<CanvasGroup>());
        spriteRenderer = objectToFadeAndMove.GetComponent<SpriteRenderer>();

        // Cache original alpha values
        if (spriteRenderer != null)
        {
            originalSpriteAlphas[spriteRenderer] = spriteRenderer.color.a;
        }

        foreach (var renderer in renderers)
        {
            if (renderer is SpriteRenderer sr && sr != spriteRenderer)
            {
                originalSpriteAlphas[sr] = sr.color.a;
            }
        }

        foreach (var canvasGroup in canvasGroups)
        {
            originalCanvasAlphas[canvasGroup] = canvasGroup.alpha;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (runOnce && hasTriggered) return;
        if (objectToFadeAndMove == null || targetPosition == null) return;

        hasTriggered = true;
        ExecuteFadeAndMove();
    }

    private void ExecuteFadeAndMove()
    {
        Sequence sequence = DOTween.Sequence();

        // Fade out
        if (spriteRenderer != null)
        {
            sequence.Join(spriteRenderer.DOFade(0f, fadeDuration).SetEase(fadeEase));
        }

        foreach (var renderer in renderers)
        {
            if (renderer is SpriteRenderer sr && sr != spriteRenderer)
            {
                sequence.Join(sr.DOFade(0f, fadeDuration).SetEase(fadeEase));
            }
        }

        foreach (var canvasGroup in canvasGroups)
        {
            sequence.Join(canvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase));
        }

        // Move after fade
        sequence.AppendInterval(delayAfterFade);
        sequence.AppendCallback(() =>
        {
            if (objectToFadeAndMove != null && targetPosition != null)
            {
                objectToFadeAndMove.transform.DOMove(targetPosition.position, moveDuration)
                    .SetEase(moveEase)
                    .OnComplete(() => FadeIn());
            }
        });

        sequence.Play();
    }

    private void FadeIn()
    {
        Sequence fadeInSequence = DOTween.Sequence();

        if (spriteRenderer != null && originalSpriteAlphas.ContainsKey(spriteRenderer))
        {
            float targetAlpha = originalSpriteAlphas[spriteRenderer];
            fadeInSequence.Join(spriteRenderer.DOFade(targetAlpha, fadeDuration).SetEase(fadeEase));
        }

        foreach (var renderer in renderers)
        {
            if (renderer is SpriteRenderer sr && sr != spriteRenderer && originalSpriteAlphas.ContainsKey(sr))
            {
                float targetAlpha = originalSpriteAlphas[sr];
                fadeInSequence.Join(sr.DOFade(targetAlpha, fadeDuration).SetEase(fadeEase));
            }
        }

        foreach (var canvasGroup in canvasGroups)
        {
            if (originalCanvasAlphas.ContainsKey(canvasGroup))
            {
                float targetAlpha = originalCanvasAlphas[canvasGroup];
                fadeInSequence.Join(canvasGroup.DOFade(targetAlpha, fadeDuration).SetEase(fadeEase));
            }
        }

        fadeInSequence.Play();
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}

