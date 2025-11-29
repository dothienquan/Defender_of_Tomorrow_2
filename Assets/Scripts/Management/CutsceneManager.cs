using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

/// <summary>
/// Quản lý việc load và chuyển đổi giữa scene chính và cutscene scenes
/// </summary>
public class CutsceneManager : Singleton<CutsceneManager>
{
    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float waitBeforeLoad = 0.3f;
    [SerializeField] private bool disablePlayerDuringCutscene = true;
    [SerializeField] private bool allowSkipCutscene = true;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;

    [Header("Return Position Settings")]
    [SerializeField] private bool useCustomReturnPosition = true; // Sử dụng CutsceneReturnPoint nếu có
    [SerializeField] private string returnPointID = ""; // ID của return point (optional, để match với cutscene cụ thể)

    private string previousSceneName;
    private bool isCutsceneActive = false;
    private PlayerController playerController;
    
    private const string PREVIOUS_SCENE_KEY = "CutsceneManager_PreviousScene";
    private const string RETURN_POINT_ID_KEY = "CutsceneManager_ReturnPointID";
    private const string USE_RETURN_POINT_KEY = "CutsceneManager_UseReturnPoint";

    protected override void Awake()
    {
        base.Awake();
        // Singleton base class đã xử lý DontDestroyOnLoad
        
        // Kiểm tra xem có phải New Game không (flag được set bởi MainMenuController)
        bool isNewGame = PlayerPrefs.GetInt("MainMenuController_NewGame", 0) == 1;
        if (isNewGame)
        {
            // Nếu là New Game, clear tất cả dữ liệu liên quan đến cutscene
            previousSceneName = null;
            PlayerPrefs.DeleteKey(PREVIOUS_SCENE_KEY);
            PlayerPrefs.DeleteKey(RETURN_POINT_ID_KEY);
            PlayerPrefs.DeleteKey(USE_RETURN_POINT_KEY);
            Debug.Log("[CutsceneManager] New Game detected - cleared all cutscene data.");
        }
        else
        {
            // Khôi phục previousSceneName từ PlayerPrefs nếu có
            if (string.IsNullOrEmpty(previousSceneName))
            {
                previousSceneName = PlayerPrefs.GetString(PREVIOUS_SCENE_KEY, "");
                if (!string.IsNullOrEmpty(previousSceneName))
                {
                    Debug.Log($"[CutsceneManager] Khôi phục scene trước đó từ PlayerPrefs: {previousSceneName}");
                }
            }
        }
    }

    private void Update()
    {
        // Cho phép skip cutscene
        if (isCutsceneActive && allowSkipCutscene && Input.GetKeyDown(skipKey))
        {
            SkipCutscene();
        }
    }

    /// <summary>
    /// Load cutscene scene từ scene hiện tại
    /// </summary>
    /// <param name="cutsceneSceneName">Tên scene cutscene cần load</param>
    /// <param name="returnToPreviousScene">Có quay lại scene trước đó sau khi cutscene kết thúc không</param>
    /// <param name="useReturnPoint">Có sử dụng CutsceneReturnPoint khi quay lại không (mặc định: false)</param>
    public void LoadCutscene(string cutsceneSceneName, bool returnToPreviousScene = true, bool useReturnPoint = false)
    {
        if (isCutsceneActive)
        {
            Debug.LogWarning("[CutsceneManager] Đang có cutscene đang chạy, không thể load cutscene mới.");
            return;
        }

        if (returnToPreviousScene)
        {
            previousSceneName = SceneManager.GetActiveScene().name;
            // Lưu vào PlayerPrefs để persist qua scene load
            PlayerPrefs.SetString(PREVIOUS_SCENE_KEY, previousSceneName);
            // Lưu flag có dùng return point hay không
            PlayerPrefs.SetInt(USE_RETURN_POINT_KEY, useReturnPoint ? 1 : 0);
            // Lưu return point ID nếu có và được bật
            if (useReturnPoint && !string.IsNullOrEmpty(returnPointID))
            {
                PlayerPrefs.SetString(RETURN_POINT_ID_KEY, returnPointID);
            }
            else
            {
                PlayerPrefs.DeleteKey(RETURN_POINT_ID_KEY);
            }
            PlayerPrefs.Save();
            Debug.Log($"[CutsceneManager] Lưu scene hiện tại để quay lại: {previousSceneName}, UseReturnPoint: {useReturnPoint}, ReturnPointID: {returnPointID}");
        }
        else
        {
            previousSceneName = null;
            PlayerPrefs.DeleteKey(PREVIOUS_SCENE_KEY);
        }

        StartCoroutine(LoadCutsceneRoutine(cutsceneSceneName));
    }

    /// <summary>
    /// Load cutscene với fade effect
    /// </summary>
    private IEnumerator LoadCutsceneRoutine(string cutsceneSceneName)
    {
        isCutsceneActive = true;

        // Disable player nếu cần
        if (disablePlayerDuringCutscene)
        {
            DisablePlayer();
        }

        // Fade to black
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToBlack(fadeInDuration);
            yield return new WaitForSeconds(fadeInDuration);
        }

        // Wait a bit
        yield return new WaitForSeconds(waitBeforeLoad);

        // Đảm bảo PlayerControls được disable trước khi load scene
        EnsurePlayerControlsDisabled();

        // Load cutscene scene
        SceneManager.LoadScene(cutsceneSceneName);

        // Đợi scene load xong
        yield return null;
        yield return null;

        // Fade to clear
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToClear(fadeOutDuration);
        }
    }

    /// <summary>
    /// Kết thúc cutscene và quay lại scene trước đó (nếu có)
    /// </summary>
    public void EndCutscene()
    {
        Debug.Log($"[CutsceneManager] EndCutscene() called. isCutsceneActive: {isCutsceneActive}, previousSceneName: {previousSceneName}");
        
        // Nếu không có cutscene active, vẫn cho phép load scene nếu có previousSceneName
        if (!isCutsceneActive)
        {
            // Kiểm tra PlayerPrefs
            string savedScene = PlayerPrefs.GetString(PREVIOUS_SCENE_KEY, "");
            if (!string.IsNullOrEmpty(savedScene))
            {
                Debug.LogWarning($"[CutsceneManager] Không có cutscene active nhưng tìm thấy scene đã lưu: {savedScene}. Sẽ load scene này.");
                previousSceneName = savedScene;
            }
            else if (string.IsNullOrEmpty(previousSceneName))
            {
                Debug.LogWarning("[CutsceneManager] Không có cutscene đang chạy và không có scene trước đó được lưu.");
                // Vẫn cho phép load scene mặc định
            }
        }

        StartCoroutine(EndCutsceneRoutine());
    }

    /// <summary>
    /// Kết thúc cutscene với fade effect
    /// </summary>
    private IEnumerator EndCutsceneRoutine()
    {
        // Fade to black
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToBlack(fadeInDuration);
            yield return new WaitForSeconds(fadeInDuration);
        }

        // Wait a bit
        yield return new WaitForSeconds(waitBeforeLoad);

        // Đảm bảo PlayerControls được disable trước khi load scene
        EnsurePlayerControlsDisabled();

        // Load scene trước đó hoặc scene mặc định
        // Kiểm tra cả trong memory và PlayerPrefs
        if (string.IsNullOrEmpty(previousSceneName))
        {
            previousSceneName = PlayerPrefs.GetString(PREVIOUS_SCENE_KEY, "");
        }
        
        if (!string.IsNullOrEmpty(previousSceneName))
        {
            Debug.Log($"[CutsceneManager] Quay lại scene ban đầu: {previousSceneName}");
            // Xóa PlayerPrefs sau khi sử dụng
            PlayerPrefs.DeleteKey(PREVIOUS_SCENE_KEY);
            PlayerPrefs.Save();
            SceneManager.LoadScene(previousSceneName);
        }
        else
        {
            // Load scene chính mặc định nếu không có scene trước đó
            Debug.LogWarning("[CutsceneManager] Không có scene trước đó được lưu. Load scene mặc định.");
            SceneManager.LoadScene("Defender Of Tomorrow");
        }

        // Đợi scene load xong
        yield return null;
        yield return null;

        // Kiểm tra xem có nên dùng return point không (từ PlayerPrefs)
        bool shouldUseReturnPoint = PlayerPrefs.GetInt(USE_RETURN_POINT_KEY, 0) == 1;
        
        // Set player position từ CutsceneReturnPoint nếu được bật
        if (useCustomReturnPosition && shouldUseReturnPoint)
        {
            SetPlayerReturnPosition();
        }
        else
        {
            Debug.Log("[CutsceneManager] Không sử dụng return point. Player sẽ ở vị trí mặc định.");
        }

        // Re-enable player
        if (disablePlayerDuringCutscene)
        {
            EnablePlayer();
        }

        // Fade to clear
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToClear(fadeOutDuration);
        }

        isCutsceneActive = false;
        previousSceneName = null;
        // Xóa PlayerPrefs sau khi hoàn thành
        PlayerPrefs.DeleteKey(PREVIOUS_SCENE_KEY);
        PlayerPrefs.DeleteKey(RETURN_POINT_ID_KEY);
        PlayerPrefs.DeleteKey(USE_RETURN_POINT_KEY);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Skip cutscene hiện tại
    /// </summary>
    public void SkipCutscene()
    {
        if (!isCutsceneActive)
        {
            return;
        }

        Debug.Log("[CutsceneManager] Cutscene đã được skip.");
        EndCutscene();
    }

    /// <summary>
    /// Disable player controller
    /// </summary>
    private void DisablePlayer()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
    }

    /// <summary>
    /// Enable player controller
    /// </summary>
    private void EnablePlayer()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        else
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }
        }
    }

    /// <summary>
    /// Kiểm tra xem có cutscene đang chạy không
    /// </summary>
    public bool IsCutsceneActive()
    {
        return isCutsceneActive;
    }

    /// <summary>
    /// Set scene trước đó (dùng khi load cutscene từ code khác)
    /// </summary>
    public void SetPreviousScene(string sceneName)
    {
        previousSceneName = sceneName;
        PlayerPrefs.SetString(PREVIOUS_SCENE_KEY, sceneName);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set return point ID để match với CutsceneReturnPoint cụ thể
    /// </summary>
    public void SetReturnPointID(string id)
    {
        returnPointID = id;
        if (!string.IsNullOrEmpty(id))
        {
            PlayerPrefs.SetString(RETURN_POINT_ID_KEY, id);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Set vị trí player khi quay lại từ cutscene
    /// </summary>
    private void SetPlayerReturnPosition()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[CutsceneManager] Không tìm thấy PlayerController để set vị trí.");
            return;
        }

        // Lấy return point ID từ PlayerPrefs nếu có
        string savedReturnPointID = PlayerPrefs.GetString(RETURN_POINT_ID_KEY, "");
        string targetID = !string.IsNullOrEmpty(savedReturnPointID) ? savedReturnPointID : returnPointID;

        // Tìm CutsceneReturnPoint
        CutsceneReturnPoint returnPoint = null;

        if (!string.IsNullOrEmpty(targetID))
        {
            // Tìm return point theo ID
            CutsceneReturnPoint[] allReturnPoints = FindObjectsByType<CutsceneReturnPoint>(FindObjectsSortMode.None);
            foreach (var point in allReturnPoints)
            {
                if (point.GetReturnPointID() == targetID)
                {
                    returnPoint = point;
                    break;
                }
            }
        }
        else
        {
            // Nếu không có ID, dùng return point đầu tiên tìm thấy
            returnPoint = FindFirstObjectByType<CutsceneReturnPoint>();
        }

        if (returnPoint != null)
        {
            Vector2 spawnPosition = returnPoint.GetSpawnPosition();
            player.transform.position = spawnPosition;
            Debug.Log($"[CutsceneManager] Set player position từ CutsceneReturnPoint: {spawnPosition}");
            
            // Update camera confiner với boundary phù hợp
            UpdateCameraConfiner(returnPoint);
            
            // Set camera follow nếu có
            var cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetPlayerCameraFollow();
            }
        }
        else
        {
            Debug.Log("[CutsceneManager] Không tìm thấy CutsceneReturnPoint. Player sẽ ở vị trí mặc định.");
            // Vẫn cố gắng update confiner với boundary mặc định
            UpdateCameraConfiner(null);
        }
    }

    /// <summary>
    /// Update camera confiner với boundary phù hợp
    /// </summary>
    private void UpdateCameraConfiner(CutsceneReturnPoint returnPoint)
    {
        CinemachineConfiner confiner = FindFirstObjectByType<CinemachineConfiner>();
        if (confiner == null)
        {
            Debug.LogWarning("[CutsceneManager] Không tìm thấy CinemachineConfiner.");
            return;
        }

        PolygonCollider2D boundary = null;

        if (returnPoint != null)
        {
            // Lấy boundary từ return point
            boundary = returnPoint.GetMapBoundary();
        }

        // Nếu không có boundary từ return point, tìm boundary gần player nhất
        if (boundary == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                Vector2 playerPos = player.transform.position;
                PolygonCollider2D[] allBoundaries = FindObjectsByType<PolygonCollider2D>(FindObjectsSortMode.None);
                PolygonCollider2D closest = null;
                float closestDistance = float.MaxValue;

                foreach (var b in allBoundaries)
                {
                    // Kiểm tra xem boundary có chứa player không
                    if (b.bounds.Contains(playerPos))
                    {
                        boundary = b;
                        break;
                    }

                    // Hoặc tìm boundary gần nhất
                    float distance = Vector2.Distance(b.bounds.center, playerPos);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = b;
                    }
                }

                if (boundary == null && closest != null)
                {
                    boundary = closest;
                }
            }
        }

        if (boundary != null)
        {
            confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
            confiner.m_BoundingShape2D = boundary;
            confiner.InvalidatePathCache();
            Debug.Log($"[CutsceneManager] Updated camera confiner với boundary: {boundary.name}");
        }
        else
        {
            Debug.LogWarning("[CutsceneManager] Không tìm thấy map boundary phù hợp cho camera confiner.");
        }
    }

    /// <summary>
    /// Đảm bảo tất cả PlayerControls được disable trước khi unload scene
    /// Bằng cách disable các component để trigger OnDisable()
    /// </summary>
    private void EnsurePlayerControlsDisabled()
    {
        // Disable PlayerController component để trigger OnDisable
        var playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null && playerController.enabled)
        {
            playerController.enabled = false;
        }

        // Disable ActiveWeapon component
        var activeWeapon = FindFirstObjectByType<ActiveWeapon>();
        if (activeWeapon != null && activeWeapon.enabled)
        {
            activeWeapon.enabled = false;
        }

        // Disable ActiveInventory component
        var activeInventory = FindFirstObjectByType<ActiveInventory>();
        if (activeInventory != null && activeInventory.enabled)
        {
            activeInventory.enabled = false;
        }
    }
}

