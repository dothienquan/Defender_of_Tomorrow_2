using System.Collections;
using UnityEngine;

public class ContinuousBlink : MonoBehaviour
{
    [Header("Settings")]
    public Color blinkColor = Color.red;
    public float interval = 0.2f; // Time between color changes
    public bool startOnAwake = false; // Check this to blink immediately

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine blinkCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        if (startOnAwake)
            StartBlinking();
    }

    public void StartBlinking()
    {
        if (blinkCoroutine == null)
            blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    public void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        // Always return to normal color when stopped
        spriteRenderer.color = originalColor;
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            // Switch to Red
            spriteRenderer.color = blinkColor;
            yield return new WaitForSeconds(interval);

            // Switch to Normal
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(interval);
        }
    }
}