using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button pauseButton; // Optional: UI button to pause

    [Header("Settings")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool pauseOnStart = false;

    private bool _isPaused = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ResumeGame);
        }

        // Setup pause button if exists
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(PauseGame);
        }

        // Hide panel on start
        if (pausePanel != null)
        {
            pausePanel.SetActive(pauseOnStart);
        }

        // Set initial game state
        if (pauseOnStart)
        {
            Time.timeScale = 0f;
            _isPaused = true;
        }
        else
        {
            Time.timeScale = 1f;
            _isPaused = false;
        }
    }

    private void Update()
    {
        // Check for pause key press
        if (Input.GetKeyDown(pauseKey))
        {
            if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (_isPaused) return;

        _isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        if (!_isPaused) return;

        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public bool IsPaused => _isPaused;

    private void OnDestroy()
    {
        // Reset time scale when destroyed (safety measure)
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }
}

