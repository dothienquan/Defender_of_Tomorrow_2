using UnityEngine;

/// <summary>
/// Component để trigger cutscene khi player đi vào trigger zone
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CutsceneTrigger : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [SerializeField] private string cutsceneSceneName;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool returnToPreviousScene = true;

    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnEnter) return;
        TriggerCutscene(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (triggerOnEnter) return;
        TriggerCutscene(other);
    }

    private void TriggerCutscene(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (triggerOnce && hasTriggered) return;

        if (string.IsNullOrEmpty(cutsceneSceneName))
        {
            Debug.LogError($"[CutsceneTrigger] Chưa gán cutsceneSceneName trên {gameObject.name}!");
            return;
        }

        hasTriggered = true;

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.LoadCutscene(cutsceneSceneName, returnToPreviousScene);
        }
        else
        {
            Debug.LogError("[CutsceneTrigger] Không tìm thấy CutsceneManager trong scene!");
        }
    }

    /// <summary>
    /// Reset trigger để có thể trigger lại
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    /// <summary>
    /// Load cutscene từ code
    /// </summary>
    public void LoadCutscene()
    {
        if (string.IsNullOrEmpty(cutsceneSceneName))
        {
            Debug.LogError($"[CutsceneTrigger] Chưa gán cutsceneSceneName trên {gameObject.name}!");
            return;
        }

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.LoadCutscene(cutsceneSceneName, returnToPreviousScene);
        }
    }
}

