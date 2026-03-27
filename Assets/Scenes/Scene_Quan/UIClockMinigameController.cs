using UnityEngine;
using UnityEngine.Events;

public class UIClockMinigameController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root GameObject of the minigame panel (enable/disable).")]
    public GameObject panelRoot;

    [Tooltip("Clock minigame logic. Will be enabled when panel opens.")]
    public ClockMinigameController minigame;

    [Tooltip("Optional: Disable a PlayerController (or any MonoBehaviour) while the panel is open.")]
    public MonoBehaviour playerControllerToDisable;

    [Header("Events")]
    public UnityEvent onPanelOpened;
    public UnityEvent onPanelClosed;
    public UnityEvent onMiniGameSuccess;

    private bool _isOpen;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (minigame != null) minigame.enabled = false;
    }

    public void OpenPanel()
    {
        if (_isOpen) return;
        _isOpen = true;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (minigame != null)
        {
            minigame.enabled = true;
            minigame.ResetGame();
            minigame.OnCompleted -= HandleMinigameComplete;
            minigame.OnCompleted += HandleMinigameComplete;
        }
        if (playerControllerToDisable != null) playerControllerToDisable.enabled = false;

        onPanelOpened?.Invoke();
    }

    public void ClosePanel()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (panelRoot != null) panelRoot.SetActive(false);
        if (minigame != null) minigame.enabled = false;
        if (playerControllerToDisable != null) playerControllerToDisable.enabled = true;

        onPanelClosed?.Invoke();
    }

    private void HandleMinigameComplete()
    {
        onMiniGameSuccess?.Invoke();
        ClosePanel();
    }
}
