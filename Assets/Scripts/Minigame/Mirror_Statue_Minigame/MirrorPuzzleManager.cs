using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MirrorPuzzleManager : MonoBehaviour
{
    [Header("Statues in order (0 -> 1 -> 2 ... )")]
    public List<Statue> statues = new List<Statue>();

    [Header("First active statue index")]
    public int startIndex = 0;

    [Header("Events")]
    public UnityEvent onPuzzleStarted;
    public UnityEvent onPuzzleSuccess;
    public UnityEvent onPuzzleReset;

    [Header("Cutscene Settings")]
    [SerializeField] private bool loadCutsceneOnComplete = false;
    [SerializeField] private string cutsceneSceneName = "";
    [SerializeField] private float delayBeforeCutscene = 1f; // Delay trước khi load cutscene

    public int CurrentIndex { get; private set; } = -1;
    public bool IsRunning { get; private set; } = false;

    void Awake()
    {
        for (int i = 0; i < statues.Count; i++)
        {
            if (statues[i] != null)
            {
                statues[i].Init(this, i);
                statues[i].Deactivate();
            }
        }
    }

    public void StartPuzzle()
    {
        if (statues.Count == 0) return;

        ResetPuzzleInternal(false);
        IsRunning = true;
        CurrentIndex = Mathf.Clamp(startIndex, 0, statues.Count - 1);

        for (int i = 0; i < statues.Count; i++)
        {
            statues[i].SetCompleted(false);
        }

        statues[CurrentIndex].Activate();
        onPuzzleStarted?.Invoke();
    }

    public void AdvanceToNextStatue()
    {
        if (!IsRunning) return;

        statues[CurrentIndex].SetCompleted(true);
        statues[CurrentIndex].Deactivate();

        CurrentIndex++;

        // If last statue completed its connection and loops to first -> puzzle success
        if (CurrentIndex >= statues.Count)
        {
            IsRunning = false;
            onPuzzleSuccess?.Invoke();
            
            // Load cutscene nếu được bật
            if (loadCutsceneOnComplete && !string.IsNullOrEmpty(cutsceneSceneName))
            {
                StartCoroutine(LoadCutsceneAfterDelay());
            }
            
            return;
        }

        // Otherwise, activate the next statue
        statues[CurrentIndex].Activate();
    }

    public void ResetPuzzle()
    {
        ResetPuzzleInternal(true);
    }

    private void ResetPuzzleInternal(bool fireEvent)
    {
        IsRunning = false;
        CurrentIndex = -1;

        foreach (var s in statues)
        {
            if (s != null)
            {
                s.ResetStatue();
                s.Deactivate();
                s.SetCompleted(false);
            }
        }

        if (fireEvent) onPuzzleReset?.Invoke();
    }

    /// <summary>
    /// Checks if the hit statue is the correct next target.
    /// Now supports loop: last statue must hit the first to complete the puzzle.
    /// </summary>
    public bool IsCorrectNextTarget(Statue hit)
    {
        if (!IsRunning || hit == null) return false;

        int nextIndex = CurrentIndex + 1;

        // Loop: if current is last, next must be the first (0)
        if (nextIndex >= statues.Count)
            nextIndex = 0;

        return hit.Index == nextIndex;
    }

    public bool IsCurrentStatue(Statue s)
    {
        return IsRunning && s != null && s.Index == CurrentIndex;
    }

    /// <summary>
    /// Load cutscene sau một khoảng delay
    /// </summary>
    private IEnumerator LoadCutsceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeCutscene);

        if (CutsceneManager.Instance != null)
        {
            // Load cutscene với return point (vì đây là từ laser minigame)
            CutsceneManager.Instance.LoadCutscene(cutsceneSceneName, true, true);
        }
        else
        {
            Debug.LogWarning("[MirrorPuzzleManager] Không tìm thấy CutsceneManager. Load scene trực tiếp.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(cutsceneSceneName);
        }
    }
}
