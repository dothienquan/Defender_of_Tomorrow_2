using System.Collections;
using UnityEngine;

public class BossHealthUIManager : MonoBehaviour
{
    public static BossHealthUIManager Instance { get; private set; }

    [Header("Defaults")]
    [SerializeField] private RectTransform uiRoot;          // Parent under Canvas
    [SerializeField] private BossHealthPanel defaultPanelPrefab;

    [Header("Slide Animation")]
    [SerializeField] private bool slideInOut = true;
    [SerializeField] private float slideDuration = 0.25f;

    private BossHealthPanel _currentPanel;
    private Coroutine _animRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// Use defaultPanelPrefab
    public void ShowFor(EnemyHealth enemyHealth)
    {
        ShowFor(enemyHealth, (BossHealthPanel)null, null);
    }

    /// Use a specific panel prefab (for different boss types)
    public void ShowFor(EnemyHealth enemyHealth, BossHealthPanel panelPrefab)
    {
        ShowFor(enemyHealth, panelPrefab, null);
    }

    /// Use a UI profile (recommended)
    public void ShowFor(EnemyHealth enemyHealth, BossUIProfile profile)
    {
        BossHealthPanel prefab = (profile != null && profile.panelPrefab != null) ? profile.panelPrefab : null;
        ShowFor(enemyHealth, prefab, profile);
    }

    private void ShowFor(EnemyHealth enemyHealth, BossHealthPanel overridePrefab, BossUIProfile profile)
    {
        if (enemyHealth == null) return;
        if (uiRoot == null)
        {
            Debug.LogWarning("[BossHealthUIManager] Missing uiRoot.");
            return;
        }

        BossHealthPanel prefabToUse = overridePrefab != null ? overridePrefab : defaultPanelPrefab;
        if (prefabToUse == null)
        {
            Debug.LogWarning("[BossHealthUIManager] Missing defaultPanelPrefab (and no override).");
            return;
        }

        // If showing same target, just ensure visible
        if (_currentPanel != null && _currentPanel.IsBoundTo(enemyHealth))
        {
            _currentPanel.SetVisible(true);
            return;
        }

        // Replace existing boss UI (typical boss flow: 1 boss at a time)
        if (_currentPanel != null)
        {
            Destroy(_currentPanel.gameObject);
            _currentPanel = null;
        }

        _currentPanel = Instantiate(prefabToUse, uiRoot);
        if (profile != null)
        {
            _currentPanel.ApplyProfile(profile);
        }
        _currentPanel.Bind(enemyHealth);

        if (slideInOut) StartSlide(_currentPanel, true);
        else _currentPanel.SetVisible(true);
    }

    public void Hide()
    {
        if (_currentPanel == null) return;

        if (slideInOut) StartSlide(_currentPanel, false);
        else
        {
            Destroy(_currentPanel.gameObject);
            _currentPanel = null;
        }
    }

    private void StartSlide(BossHealthPanel panel, bool show)
    {
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(SlideRoutine(panel, show));
    }

    private IEnumerator SlideRoutine(BossHealthPanel panel, bool show)
    {
        if (panel == null) yield break;

        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null)
        {
            panel.SetVisible(show);
            if (!show)
            {
                Destroy(panel.gameObject);
                _currentPanel = null;
            }
            yield break;
        }

        panel.SetVisible(true);

        Vector2 onPos = panel.OnScreenAnchoredPos;
        Vector2 offPos = panel.OffScreenAnchoredPos;

        Vector2 start = show ? offPos : onPos;
        Vector2 end = show ? onPos : offPos;

        float t = 0f;
        rt.anchoredPosition = start;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, slideDuration);
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // easeOutCubic
            rt.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
            yield return null;
        }

        rt.anchoredPosition = end;

        if (!show)
        {
            Destroy(panel.gameObject);
            if (_currentPanel == panel) _currentPanel = null;
        }

        _animRoutine = null;
    }
}
