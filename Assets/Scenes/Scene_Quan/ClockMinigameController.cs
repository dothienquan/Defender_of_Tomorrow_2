using UnityEngine;
using System;
using System.Collections.Generic;

public class ClockMinigameController : MonoBehaviour
{
    [Header("References")]
    public Transform hand;                  // Rotating hand pivot (zero angle points to +X)
    public Transform center;                // Center reference (usually same as this.transform)
    public List<ClockPointUI> points = new List<ClockPointUI>();

    [Header("Timing & Input")]
    public float rotationSpeedDegPerSec = 120f;
    public KeyCode hitKey = KeyCode.Space;
    [Tooltip("Angle window (degrees) for auto-highlight when the hand nears a point.")]
    public float highlightWindowDeg = 10f;
    [Tooltip("Stricter angle window (degrees) for a successful lock when pressing hitKey.")]
    public float hitWindowDeg = 6f;
    [Tooltip("How far (degrees) past the highlight window before dimming again if not locked.")]
    public float graceDeg = 12f;

    [Header("Audio (Optional)")]
    public AudioSource sfx;
    public AudioClip sfxHighlight;
    public AudioClip sfxLock;
    public AudioClip sfxFail;

    public event Action OnCompleted;

    float _handAngle; // 0..360
    Dictionary<ClockPointUI, float> _pointAngles = new Dictionary<ClockPointUI, float>();

    void OnEnable()
    {
        if (center == null) center = transform;
        CacheAngles();
        ResetGame();
    }

    void Update()
    {
        // Rotate hand
        _handAngle += rotationSpeedDegPerSec * Time.deltaTime;
        _handAngle %= 360f;
        if (hand != null) hand.localRotation = Quaternion.Euler(0f, 0f, _handAngle - 90f); // -90 so 0deg points to +X visually

        // Auto highlight when in range; dim when passed
        foreach (var p in points)
        {
            if (p == null) continue;
            float a = _pointAngles[p];
            float diff = SmallestAngleDiffDeg(_handAngle, a);

            if (p.state == ClockPointState.Locked) continue;

            if (Mathf.Abs(diff) <= highlightWindowDeg)
            {
                if (p.state != ClockPointState.Highlighted)
                {
                    p.SetHighlighted();
                    PlaySfx(sfxHighlight);
                }
            }
            else if (Mathf.Abs(diff) > highlightWindowDeg + graceDeg)
            {
                if (p.state == ClockPointState.Highlighted)
                {
                    p.SetDimFromHighlight();
                }
            }
        }

        // Hit input
        if (Input.GetKeyDown(hitKey))
        {
            TryLockAnyHighlightedPoint();
        }
    }

    public void ResetGame()
    {
        _handAngle = 0f;
        if (hand != null) hand.localRotation = Quaternion.identity;

        foreach (var p in points)
        {
            if (p != null) p.SetIdleImmediate();
        }
    }

    void CacheAngles()
    {
        _pointAngles.Clear();
        foreach (var p in points)
        {
            if (p == null) continue;
            float ang = p.GetAngleDegrees(center != null ? center.position : Vector3.zero);
            _pointAngles[p] = ang;
        }
    }

    void TryLockAnyHighlightedPoint()
    {
        ClockPointUI best = null;
        float bestAbsDiff = 999f;

        foreach (var p in points)
        {
            if (p == null) continue;
            if (p.state != ClockPointState.Highlighted) continue;
            float a = _pointAngles[p];
            float diff = Mathf.Abs(SmallestAngleDiffDeg(_handAngle, a));
            if (diff < bestAbsDiff)
            {
                bestAbsDiff = diff;
                best = p;
            }
        }

        if (best != null && bestAbsDiff <= hitWindowDeg)
        {
            best.SetLocked();
            PlaySfx(sfxLock);
            CheckForCompletion();
        }
        else
        {
            PlaySfx(sfxFail);
        }
    }

    void CheckForCompletion()
    {
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (p == null) continue;
            if (p.state != ClockPointState.Locked) return;
        }
        OnCompleted?.Invoke();
    }

    static float SmallestAngleDiffDeg(float a, float b)
    {
        float diff = Mathf.Repeat((a - b) + 540f, 360f) - 180f;
        return diff;
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfx != null && clip != null)
        {
            sfx.PlayOneShot(clip);
        }
    }
}
