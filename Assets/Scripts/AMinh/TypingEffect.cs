using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class TypingEffect : MonoBehaviour
{
    public TMP_Text textUI;

    [Header("Dialogue Lines (Edit in Inspector)")]
    [TextArea(2, 5)]
    public List<string> lines = new List<string>();

    [Header("Timing")]
    public float typingDurationPerLine = 2f;
    public float delayBetweenLines = 0.5f;

    [Header("Control")]
    public bool autoStart = true;   // Start on Awake/Start
    public bool waitForInput = false; // If true, wait for Next() instead of auto-advance

    private int currentIndex = 0;
    private Tween typingTween;

    void Start()
    {
        if (autoStart)
        {
            StartCutscene();
        }
    }

    public void StartCutscene()
    {
        currentIndex = 0;
        PlayCurrentLine();
    }

    public void Next()
    {
        // For manual input (space, button, etc.)
        if (typingTween != null && typingTween.IsActive() && typingTween.IsPlaying())
        {
            // If still typing → finish instantly
            typingTween.Complete();
        }
        else
        {
            // Go to next line
            currentIndex++;
            PlayCurrentLine();
        }
    }

    private void PlayCurrentLine()
    {
        if (typingTween != null && typingTween.IsActive())
            typingTween.Kill();

        if (currentIndex >= lines.Count)
        {
            OnCutsceneEnd();
            return;
        }

        string message = lines[currentIndex];
        textUI.text = "";

        int targetCount = message.Length;

        typingTween = DOTween.To(
            () => 0,
            i => textUI.text = message.Substring(0, i),
            targetCount,
            typingDurationPerLine
        )
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            if (!waitForInput)
            {
                // Auto next after delay
                DOVirtual.DelayedCall(delayBetweenLines, () =>
                {
                    currentIndex++;
                    PlayCurrentLine();
                });
            }
        });
    }

    private void OnCutsceneEnd()
    {
        // TODO: trigger event, load next scene, enable player control, etc.
        Debug.Log("Cutscene finished.");
    }
}
