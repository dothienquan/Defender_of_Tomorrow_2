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
    [SerializeField] private bool inactiveAfterTrigger = true; // Set GameObject inactive sau khi trigger
    [SerializeField] private bool useReturnPoint = false; // Sử dụng CutsceneReturnPoint khi quay lại (mặc định: false)
    [SerializeField] private bool persistInactiveState = true; // Lưu trạng thái inactive qua scene load

    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";

    [Header("Persistence Settings")]
    [SerializeField] private string triggerID = ""; // ID duy nhất để lưu trạng thái (nếu để trống sẽ dùng tên GameObject)

    private bool hasTriggered = false;
    private string TriggerStateKey => $"CutsceneTrigger_Inactive_{GetTriggerID()}";

    private string GetTriggerID()
    {
        if (!string.IsNullOrEmpty(triggerID))
            return triggerID;
        return gameObject.name;
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Start()
    {
        // Khôi phục trạng thái inactive nếu đã được lưu
        if (persistInactiveState)
        {
            bool wasInactive = PlayerPrefs.GetInt(TriggerStateKey, 0) == 1;
            if (wasInactive)
            {
                gameObject.SetActive(false);
                hasTriggered = true; // Đánh dấu đã trigger để không trigger lại
                Debug.Log($"[CutsceneTrigger] Khôi phục trạng thái inactive cho {gameObject.name}");
            }
        }
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
            CutsceneManager.Instance.LoadCutscene(cutsceneSceneName, returnToPreviousScene, useReturnPoint);
        }
        else
        {
            Debug.LogError("[CutsceneTrigger] Không tìm thấy CutsceneManager trong scene!");
        }

        // Set GameObject inactive sau khi trigger nếu được bật
        if (inactiveAfterTrigger)
        {
            gameObject.SetActive(false);
            
            // Lưu trạng thái inactive vào PlayerPrefs để persist qua scene load
            if (persistInactiveState)
            {
                PlayerPrefs.SetInt(TriggerStateKey, 1);
                PlayerPrefs.Save();
                Debug.Log($"[CutsceneTrigger] Đã lưu trạng thái inactive cho {gameObject.name} (ID: {GetTriggerID()})");
            }
            
            Debug.Log($"[CutsceneTrigger] GameObject {gameObject.name} đã được set inactive sau khi trigger.");
        }
    }

    /// <summary>
    /// Reset trigger để có thể trigger lại
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        gameObject.SetActive(true);
        
        // Xóa trạng thái inactive đã lưu
        if (persistInactiveState)
        {
            PlayerPrefs.DeleteKey(TriggerStateKey);
            PlayerPrefs.Save();
        }
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
            CutsceneManager.Instance.LoadCutscene(cutsceneSceneName, returnToPreviousScene, useReturnPoint);
        }
    }
}

