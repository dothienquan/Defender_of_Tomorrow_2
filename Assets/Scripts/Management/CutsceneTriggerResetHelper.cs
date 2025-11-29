using UnityEngine;
using Cinemachine;

/// <summary>
/// Helper component để reset tất cả CutsceneTrigger states khi scene load
/// Đặt component này vào scene game để tự động reset khi New Game
/// </summary>
public class CutsceneTriggerResetHelper : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool resetOnStart = false; // Reset khi scene bắt đầu
    [SerializeField] private bool resetOnNewGame = true; // Reset khi New Game (check PlayerPrefs flag)
    [SerializeField] private bool fixCameraConfinerOnNewGame = true; // Fix camera confiner khi New Game

    private const string NEW_GAME_FLAG_KEY = "MainMenuController_NewGame";

    private void Awake()
    {
        // Chạy sớm hơn Start() để đảm bảo reset trước khi các component khác khởi tạo
        if (resetOnNewGame)
        {
            // Kiểm tra xem có phải New Game không
            bool isNewGame = PlayerPrefs.GetInt(NEW_GAME_FLAG_KEY, 0) == 1;
            if (isNewGame)
            {
                // Reset ngay trong Awake để đảm bảo triggers được reset trước khi Start() của chúng chạy
                ResetAllTriggers();
                
                // Fix camera confiner nếu được bật
                if (fixCameraConfinerOnNewGame)
                {
                    // Delay một frame để đảm bảo player đã được spawn
                    StartCoroutine(FixCameraConfinerDelayed());
                }
                
                // Xóa flag sau khi reset
                PlayerPrefs.DeleteKey(NEW_GAME_FLAG_KEY);
                PlayerPrefs.Save();
                Debug.Log("[CutsceneTriggerResetHelper] Đã reset tất cả triggers trong Awake (New Game detected).");
            }
        }
    }
    
    private System.Collections.IEnumerator FixCameraConfinerDelayed()
    {
        // Đợi 2 frames để đảm bảo player và các component khác đã được khởi tạo
        yield return null;
        yield return null;
        
        FixCameraConfiner();
    }
    
    /// <summary>
    /// Fix camera confiner dựa trên vị trí player thực tế (không dùng return point)
    /// </summary>
    private void FixCameraConfiner()
    {
        CinemachineConfiner confiner = FindFirstObjectByType<CinemachineConfiner>();
        if (confiner == null)
        {
            Debug.LogWarning("[CutsceneTriggerResetHelper] Không tìm thấy CinemachineConfiner.");
            return;
        }
        
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[CutsceneTriggerResetHelper] Không tìm thấy PlayerController.");
            return;
        }
        
        Vector2 playerPos = player.transform.position;
        PolygonCollider2D[] allBoundaries = FindObjectsByType<PolygonCollider2D>(FindObjectsSortMode.None);
        PolygonCollider2D correctBoundary = null;
        PolygonCollider2D closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (var boundary in allBoundaries)
        {
            // Kiểm tra xem boundary có chứa player không
            if (boundary.bounds.Contains(playerPos))
            {
                correctBoundary = boundary;
                break;
            }
            
            // Hoặc tìm boundary gần nhất
            float distance = Vector2.Distance(boundary.bounds.center, playerPos);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = boundary;
            }
        }
        
        if (correctBoundary == null && closest != null)
        {
            correctBoundary = closest;
        }
        
        if (correctBoundary != null)
        {
            confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
            confiner.m_BoundingShape2D = correctBoundary;
            confiner.InvalidatePathCache();
            Debug.Log($"[CutsceneTriggerResetHelper] Đã fix camera confiner với boundary: {correctBoundary.name} (dựa trên vị trí player: {playerPos})");
        }
        else
        {
            Debug.LogWarning("[CutsceneTriggerResetHelper] Không tìm thấy boundary phù hợp cho camera confiner.");
        }
    }

    private void Start()
    {
        if (resetOnStart)
        {
            ResetAllTriggers();
        }
    }

    /// <summary>
    /// Reset tất cả CutsceneTrigger trong scene
    /// </summary>
    public void ResetAllTriggers()
    {
        CutsceneTrigger.ResetAllCutsceneTriggerStates();
        
        // Reset tất cả CutsceneTrigger objects trong scene
        CutsceneTrigger[] triggers = FindObjectsByType<CutsceneTrigger>(FindObjectsSortMode.None);
        foreach (var trigger in triggers)
        {
            trigger.ResetTrigger();
        }
        
        Debug.Log($"[CutsceneTriggerResetHelper] Đã reset {triggers.Length} CutsceneTrigger(s) trong scene.");
    }
}

