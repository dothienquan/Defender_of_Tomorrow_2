using UnityEngine;
using UnityEngine.UI;

public class BossHealthPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider healthSlider;

    [Header("Anchored Positions (for sliding)")]
    [SerializeField] private Vector2 onScreenAnchoredPos = new Vector2(0f, -40f);
    [SerializeField] private Vector2 offScreenAnchoredPos = new Vector2(0f, 120f);

    private EnemyHealth _target;
    private bool _visible = true;

    public Vector2 OnScreenAnchoredPos => onScreenAnchoredPos;
    public Vector2 OffScreenAnchoredPos => offScreenAnchoredPos;

    private void Awake()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = offScreenAnchoredPos;
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        if (!_visible) return;
        if (_target == null) return;

        if (healthSlider != null)
            healthSlider.value = Mathf.Clamp01(_target.GetHealthPercentage());
    }

    /// Called by Manager before Bind() to apply per-boss UI profile.
    public virtual void ApplyProfile(BossUIProfile profile)
    {
        if (profile == null) return;
        onScreenAnchoredPos = profile.onScreenAnchoredPos;
        offScreenAnchoredPos = profile.offScreenAnchoredPos;

        // If you have name text / icons / colors, override this method in a derived panel and apply them here.
    }

    public void Bind(EnemyHealth target)
    {
        Unbind();
        _target = target;

        if (_target != null)
            _target.OnDeath += HandleTargetDeath;

        if (healthSlider != null && _target != null)
            healthSlider.value = Mathf.Clamp01(_target.GetHealthPercentage());
    }

    public void Unbind()
    {
        if (_target != null)
            _target.OnDeath -= HandleTargetDeath;

        _target = null;
    }

    public bool IsBoundTo(EnemyHealth target) => _target == target;

    public void SetVisible(bool visible)
    {
        _visible = visible;
        gameObject.SetActive(visible);
    }

    private void HandleTargetDeath()
    {
        if (BossHealthUIManager.Instance != null)
            BossHealthUIManager.Instance.Hide();
        else
            Destroy(gameObject);
    }
}
