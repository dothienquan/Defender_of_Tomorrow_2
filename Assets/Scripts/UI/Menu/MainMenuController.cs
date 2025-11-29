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
    [SerializeField] private Button quitButton;

    [Header("Settings")]
    [SerializeField] private int gameSceneIndex = 1; // Scene index để load khi New Game
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
        StartCoroutine(LoadGameSceneRoutine());
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
    private IEnumerator LoadGameSceneRoutine()
    {
        // Disable buttons để tránh click nhiều lần
        if (newGameButton != null) newGameButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;

        // Fade to black
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToBlack(fadeDuration);
            yield return new WaitForSeconds(fadeDuration);
        }

        // Wait a bit
        yield return new WaitForSeconds(waitBeforeLoad);

        // Reset quest và save data nếu cần
        if (resetQuestOnNewGame)
        {
            ResetAllQuests();
        }

        if (resetSaveDataOnNewGame)
        {
            ResetSaveData();
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
        if (quitButton != null) quitButton.interactable = false;

        // Fade to black
        if (UIFade.Instance != null)
        {
            UIFade.Instance.FadeToBlack(fadeDuration);
            yield return new WaitForSeconds(fadeDuration);
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
    }

    private void OnDestroy()
    {
        // Remove listeners để tránh memory leak
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(OnNewGameClicked);
        
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
    }
}

