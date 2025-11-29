using UnityEngine;

/// <summary>
/// Component để kết thúc cutscene (thường đặt ở cuối cutscene scene)
/// Có thể trigger tự động hoặc khi player đi vào trigger zone
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CutsceneEndTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool endOnStart = false; // Tự động kết thúc khi scene bắt đầu
    [SerializeField] private float delayBeforeEnd = 0f; // Delay trước khi kết thúc
    [SerializeField] private bool useTrigger = false; // Dùng trigger zone thay vì tự động
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string fallbackSceneName = "Defender Of Tomorrow"; // Scene mặc định nếu không có CutsceneManager

    private void Reset()
    {
        // Tự động setup collider khi add component
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        if (endOnStart && !useTrigger)
        {
            if (delayBeforeEnd > 0)
            {
                Invoke(nameof(EndCutscene), delayBeforeEnd);
            }
            else
            {
                EndCutscene();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[CutsceneEndTrigger] OnTriggerEnter2D called with: {other.name}, tag: {other.tag}, useTrigger: {useTrigger}");
        
        if (!useTrigger)
        {
            Debug.LogWarning("[CutsceneEndTrigger] useTrigger is false, trigger will not work. Please enable 'Use Trigger' in Inspector.");
            return;
        }
        
        if (!other.CompareTag(playerTag))
        {
            Debug.Log($"[CutsceneEndTrigger] Object {other.name} does not have tag '{playerTag}'. Current tag: {other.tag}");
            return;
        }

        Debug.Log($"[CutsceneEndTrigger] Player entered trigger zone. Ending cutscene in {delayBeforeEnd} seconds...");

        if (delayBeforeEnd > 0)
        {
            Invoke(nameof(EndCutscene), delayBeforeEnd);
        }
        else
        {
            EndCutscene();
        }
    }

    /// <summary>
    /// Kết thúc cutscene
    /// </summary>
    public void EndCutscene()
    {
        Debug.Log("[CutsceneEndTrigger] EndCutscene() called");
        
        // Tìm hoặc tạo CutsceneManager
        CutsceneManager manager = CutsceneManager.Instance;
        
        if (manager == null)
        {
            // Tự động tạo CutsceneManager nếu chưa có
            GameObject managerObj = new GameObject("CutsceneManager");
            manager = managerObj.AddComponent<CutsceneManager>();
            Debug.LogWarning("[CutsceneEndTrigger] Tự động tạo CutsceneManager vì không tìm thấy instance.");
        }

        if (manager != null)
        {
            Debug.Log("[CutsceneEndTrigger] Calling CutsceneManager.EndCutscene()");
            // Luôn gọi EndCutscene, nó sẽ tự xử lý việc load scene
            // Nếu không có previousSceneName, nó sẽ load scene mặc định
            manager.EndCutscene();
        }
        else
        {
            // Fallback cuối cùng: load scene trực tiếp
            Debug.LogError($"[CutsceneEndTrigger] Không thể tạo CutsceneManager. Load scene fallback: {fallbackSceneName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(fallbackSceneName);
        }
    }
}

