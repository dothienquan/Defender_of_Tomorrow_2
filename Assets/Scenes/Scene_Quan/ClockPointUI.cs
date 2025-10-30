using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum ClockPointState { Idle, Highlighted, Locked }

public class ClockPointUI : MonoBehaviour
{
    [Header("Visuals")]
    public Image image; // Use UI Image instead of SpriteRenderer
    public Color idleColor = new Color(1f, 1f, 1f, 0.35f);
    public Color highlightColor = new Color(1f, 0.95f, 0.4f, 1f);
    public Color lockedColor = new Color(0.4f, 1f, 0.6f, 1f);
    public float pulseScale = 1.15f;
    public float pulseTime = 0.12f;

    [Header("Runtime")]
    public ClockPointState state = ClockPointState.Idle;

    private Vector3 _baseScale;
    private Coroutine _pulseCo;

    private void Reset()
    {
        image = GetComponentInChildren<Image>();
    }

    private void Awake()
    {
        if (image == null)
            image = GetComponentInChildren<Image>();

        _baseScale = transform.localScale;
        SetIdleImmediate();
    }

    /// <summary>
    /// Get the world-space angle of this point relative to the given center (e.g., the clock center).
    /// </summary>
    public float GetAngleDegrees(Vector3 centerWorld)
    {
        Vector2 dir = (Vector2)(transform.position - centerWorld);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    public void SetIdleImmediate()
    {
        state = ClockPointState.Idle;
        if (image != null) image.color = idleColor;
        transform.localScale = _baseScale;
    }

    public void SetHighlighted()
    {
        if (state == ClockPointState.Locked) return;
        state = ClockPointState.Highlighted;
        if (image != null) image.color = highlightColor;
        StartPulse();
    }

    public void SetLocked()
    {
        state = ClockPointState.Locked;
        if (image != null) image.color = lockedColor;
        StopPulse();
        transform.localScale = _baseScale;
        StartCoroutine(ConfirmBump());
    }

    public void SetDimFromHighlight()
    {
        if (state == ClockPointState.Locked) return;
        SetIdleImmediate();
        StopPulse();
    }

    private void StartPulse()
    {
        StopPulse();
        _pulseCo = StartCoroutine(Pulse());
    }

    private void StopPulse()
    {
        if (_pulseCo != null)
            StopCoroutine(_pulseCo);
        _pulseCo = null;
    }

    private IEnumerator Pulse()
    {
        while (true)
        {
            float t = 0f;
            while (t < pulseTime)
            {
                t += Time.deltaTime;
                float k = t / pulseTime;
                float s = Mathf.Lerp(1f, pulseScale, k);
                transform.localScale = _baseScale * s;
                yield return null;
            }
            t = 0f;
            while (t < pulseTime)
            {
                t += Time.deltaTime;
                float k = t / pulseTime;
                float s = Mathf.Lerp(pulseScale, 1f, k);
                transform.localScale = _baseScale * s;
                yield return null;
            }
        }
    }

    private IEnumerator ConfirmBump()
    {
        float t = 0f;
        float upTime = 0.06f;
        float downTime = 0.14f;
        float target = pulseScale;

        while (t < upTime)
        {
            t += Time.deltaTime;
            float k = t / upTime;
            float s = Mathf.Lerp(1f, target, k);
            transform.localScale = _baseScale * s;
            yield return null;
        }

        t = 0f;
        while (t < downTime)
        {
            t += Time.deltaTime;
            float k = t / downTime;
            float s = Mathf.Lerp(target, 1f, k);
            transform.localScale = _baseScale * s;
            yield return null;
        }
    }
}
