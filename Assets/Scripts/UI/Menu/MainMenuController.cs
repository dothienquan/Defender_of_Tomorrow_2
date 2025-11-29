using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controller cho Main Menu - xử lý New Game và Quit Game buttons
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [Header("Settings")]
    [SerializeField] private int gameSceneIndex = 1; // Scene index để load khi New Game hoặc Continue
    [SerializeField] private float fadeDuration = 0.5f; // Thời gian fade
    [SerializeField] private float waitBeforeLoad = 0.3f; // Thời gian chờ trước khi load scene
    [SerializeField] private bool resetQuestOnNewGame = true; // Reset quest state khi New Game
    [SerializeField] private bool resetSaveDataOnNewGame = true; // Reset save data khi New Game

    private void Awake()
    {
        // Tự động tìm buttons nếu chưa gán
        if (newGameButton == null)
        {
            GameObject newGameObj = GameObject.Find("NewGameButton");
            if (newGameObj != null)
                newGameButton = newGameObj.GetComponent<Button>();
        }

        if (continueButton == null)
        {
            GameObject continueObj = GameObject.Find("ContinueButton");
            if (continueObj != null)
                continueButton = continueObj.GetComponent<Button>();
        }

        if (quitButton == null)
        {
            GameObject quitObj = GameObject.Find("ExitButton");
            if (quitObj != null)
                quitButton = quitObj.GetComponent<Button>();
        }
    }

    private void Start()
    {
        // Setup button listeners
        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }
        else
        {
            Debug.LogWarning("[MainMenuController] NewGameButton không được tìm thấy!");
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            // Kiểm tra xem có save file không để enable/disable button
            UpdateContinueButtonState();
        }
        else
        {
            Debug.LogWarning("[MainMenuController] ContinueButton không được tìm thấy!");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
        else
        {
            Debug.LogWarning("[MainMenuController] QuitButton không được tìm thấy!");
        }
    }

    /// <summary>
    /// Xử lý khi click New Game button
    /// </summary>
    public void OnNewGameClicked()
    {
        Debug.Log("[MainMenuController] New Game clicked. Loading scene index: " + gameSceneIndex);
        StartCoroutine(LoadGameSceneRoutine(true)); // true = isNewGame
    }

    /// <summary>
    /// Xử lý khi click Continue button
    /// </summary>
    public void OnContinueClicked()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("[MainMenuController] Không có save file để continue!");
            return;
        }

        Debug.Log("[MainMenuController] Continue clicked. Loading scene index: " + gameSceneIndex);
        StartCoroutine(LoadGameSceneRoutine(false)); // false = isContinue (không phải New Game)
    }

    /// <summary>
    /// Xử lý khi click Quit button
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("[MainMenuController] Quit Game clicked.");
        
        // Có thể thêm confirmation dialog ở đây nếu muốn
        StartCoroutine(QuitGameRoutine());
    }

    /// <summary>
    /// Load game scene với fade effect
    /// </summary>
    /// <param name="isNewGame">True nếu là New Game, False nếu là Continue</param>
    private IEnumerator LoadGameSceneRoutine(bool isNewGame)
    {
        // Disable buttons để tránh click nhiều lần
        if (newGameButton != null) newGameButton.interactable = false;
        if (continueButton != null) continueButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;

        // Fade to black
        bool fadeCompleted = false;
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToBlack(fadeDuration);
            yield return new WaitForSeconds(fadeDuration);
            fadeCompleted = true;
        }
        else
        {
            // Fallback: Tạo fade effect tạm thời nếu UIFade không tồn tại
            Debug.LogWarning("[MainMenuController] UIFade.Instance không tồn tại. Tạo fade effect tạm thời...");
            yield return StartCoroutine(CreateTemporaryFade(fadeDuration, true));
            fadeCompleted = true;
        }

        // Wait a bit
        yield return new WaitForSeconds(waitBeforeLoad);

        if (isNewGame)
        {
            // Reset quest và save data nếu cần
            if (resetQuestOnNewGame)
            {
                ResetAllQuests();
            }

            if (resetSaveDataOnNewGame)
            {
                ResetSaveData();
            }

            // Set flag để scene game biết đây là New Game và cần reset triggers
            PlayerPrefs.SetInt("MainMenuController_NewGame", 1);
            PlayerPrefs.Save();
        }
        else
        {
            // Continue: Không reset data, chỉ load save
            // Xóa flag New Game nếu có
            PlayerPrefs.DeleteKey("MainMenuController_NewGame");
            PlayerPrefs.Save();
        }

        // Load game scene
        SceneManager.LoadScene(gameSceneIndex);
    }

    /// <summary>
    /// Quit game với fade effect
    /// </summary>
    private IEnumerator QuitGameRoutine()
    {
        // Disable buttons
        if (newGameButton != null) newGameButton.interactable = false;
        if (continueButton != null) continueButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;

        // Fade to black
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToBlack(fadeDuration);
            yield return new WaitForSeconds(fadeDuration);
        }
        else
        {
            // Fallback: Tạo fade effect tạm thời nếu UIFade không tồn tại
            Debug.LogWarning("[MainMenuController] UIFade.Instance không tồn tại. Tạo fade effect tạm thời...");
            yield return StartCoroutine(CreateTemporaryFade(fadeDuration, true));
        }

        // Wait a bit
        yield return new WaitForSeconds(waitBeforeLoad);

        // Quit game
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Set game scene index (có thể gọi từ code)
    /// </summary>
    public void SetGameSceneIndex(int sceneIndex)
    {
        gameSceneIndex = sceneIndex;
    }

    /// <summary>
    /// Reset tất cả quest states
    /// </summary>
    private void ResetAllQuests()
    {
        // Tìm tất cả quest keys trong PlayerPrefs và xóa
        // PlayerPrefs keys có format: "QuestState_{questID}"
        // Lưu ý: Unity không có cách trực tiếp để list tất cả keys, nên ta sẽ xóa các keys phổ biến
        
        // Xóa quest state mặc định
        PlayerPrefs.DeleteKey("QuestState_MainQuest");
        PlayerPrefs.DeleteKey("QuestState_Quest");
        
        // Reset quest trong scene hiện tại nếu có
        PassiveQuest[] quests = FindObjectsByType<PassiveQuest>(FindObjectsSortMode.None);
        foreach (var quest in quests)
        {
            quest.ResetQuest();
        }
        
        Debug.Log("[MainMenuController] Đã reset tất cả quest states.");
    }

    /// <summary>
    /// Reset save data
    /// </summary>
    private void ResetSaveData()
    {
        SaveController saveController = FindFirstObjectByType<SaveController>();
        if (saveController != null)
        {
            saveController.DeleteSave();
            Debug.Log("[MainMenuController] Đã xóa save data.");
        }
        else
        {
            // Nếu không có SaveController, xóa file save trực tiếp
            string saveLocation = System.IO.Path.Combine(Application.persistentDataPath, "saveData.json");
            if (System.IO.File.Exists(saveLocation))
            {
                System.IO.File.Delete(saveLocation);
                Debug.Log("[MainMenuController] Đã xóa save file.");
            }
        }

        // Reset tất cả CutsceneTrigger states
        ResetAllCutsceneTriggers();
        
        // Xóa tất cả PlayerPrefs keys liên quan đến CutsceneManager để tránh ảnh hưởng khi New Game
        PlayerPrefs.DeleteKey("CutsceneManager_PreviousScene");
        PlayerPrefs.DeleteKey("CutsceneManager_ReturnPointID");
        PlayerPrefs.DeleteKey("CutsceneManager_UseReturnPoint");
        PlayerPrefs.Save();
        Debug.Log("[MainMenuController] Đã xóa tất cả CutsceneManager PlayerPrefs keys.");
    }

    /// <summary>
    /// Reset tất cả CutsceneTrigger states (xóa trạng thái inactive đã lưu)
    /// </summary>
    private void ResetAllCutsceneTriggers()
    {
        // Gọi static method để reset tất cả CutsceneTrigger states
        CutsceneTrigger.ResetAllCutsceneTriggerStates();
        
        // Reset tất cả CutsceneTrigger trong scene hiện tại (nếu có)
        CutsceneTrigger[] triggers = FindObjectsByType<CutsceneTrigger>(FindObjectsSortMode.None);
        foreach (var trigger in triggers)
        {
            trigger.ResetTrigger();
        }
        
        Debug.Log($"[MainMenuController] Đã reset tất cả CutsceneTrigger states và {triggers.Length} CutsceneTrigger object(s) trong scene.");
    }

    /// <summary>
    /// Kiểm tra xem có save file không
    /// </summary>
    private bool HasSaveFile()
    {
        string saveLocation = System.IO.Path.Combine(Application.persistentDataPath, "saveData.json");
        return System.IO.File.Exists(saveLocation);
    }

    /// <summary>
    /// Update trạng thái Continue button (enable/disable dựa trên save file)
    /// </summary>
    private void UpdateContinueButtonState()
    {
        if (continueButton != null)
        {
            continueButton.interactable = HasSaveFile();
        }
    }

    /// <summary>
    /// Tạo fade effect tạm thời nếu UIFade không tồn tại
    /// </summary>
    private IEnumerator CreateTemporaryFade(float duration, bool fadeToBlack)
    {
        // Tìm hoặc tạo fade screen
        GameObject fadeObject = GameObject.Find("FadeScreen");
        Image fadeImage = null;

        if (fadeObject == null)
        {
            // Tạo fade screen mới
            fadeObject = new GameObject("FadeScreen");
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                // Tạo canvas mới nếu chưa có
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            fadeObject.transform.SetParent(canvas.transform, false);
            fadeImage = fadeObject.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, fadeToBlack ? 0 : 1);
            
            RectTransform rectTransform = fadeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            fadeImage = fadeObject.GetComponent<Image>();
            if (fadeImage == null)
            {
                fadeImage = fadeObject.AddComponent<Image>();
            }
        }

        // Fade animation
        float startAlpha = fadeImage.color.a;
        float targetAlpha = fadeToBlack ? 1f : 0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // Đảm bảo đạt target alpha chính xác
        fadeImage.color = new Color(0, 0, 0, targetAlpha);
    }

    private void OnDestroy()
    {
        // Remove listeners để tránh memory leak
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(OnNewGameClicked);
        
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
        
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
    }
}

